// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-02 오전 1:20:05   
// @PURPOSE     : 비동기 클라이언트
// ===============================


using CSharpSimpleIOCP.Network.Server;
using CSharpSimpleIOCP.Network.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpSimpleIOCP.Network.Client;
using CSharpSimpleIOCP.Util;
using System.Collections.Concurrent;

namespace CSharpSimpleIOCP.Network.Server
{
    public enum NetworkConnectionType
    {
        NotConnected,
        Local,
        Remote
    }

    public class NetworkClient : NetworkOveridableThread
    {
        protected string _UserSerial;                                      //클라 ID - 절대 중복되면 안됨
        protected long _ConnectedTime;
        protected TcpClient _TcpClient;
        protected volatile bool _IsConnectionAlive;                        //서버와 연결상태 여부
        protected ReaderWriterLockSlim _GeneralLocker;
        protected bool _NoDelay;
        protected IPEndPoint _Endpoint;                                    //접속자의 아이피
        protected INetworkClientEventListener _EventListener;              //이벤트리스너
        protected NetworkConnectionType _NetworkConnectionType;            //연결타입 - 로컬접속인지 외부접속인지
        
        private NetworkIOCPServer _IOCPServer;
        private EventWaitHandle _SendWaitHandle;
        private ConcurrentQueue<NetworkTraffic> _SendWaitQueue;
        

        #region 델리게이트와 이벤트들
        public delegate void OnReceiveCompleteHandler(NetworkDataWriter networkDataWriter);
        public delegate void OnSendCompleteHandler(NetworkDataWriter networkDataWriter);
        public delegate void OnDisconnectedHandler(long tick);

        private event OnReceiveCompleteHandler _OnReceiveComeplete;
        private event OnSendCompleteHandler _OnSendComeplete;
        private event OnDisconnectedHandler _OnDisconnected;
        #endregion

        public NetworkClient()
        {
            _UserSerial = "";
            _IsConnectionAlive = false;
            _GeneralLocker = new ReaderWriterLockSlim();
            _ConnectedTime = 0;
            _NetworkConnectionType = NetworkConnectionType.NotConnected;
            _NoDelay = true;
            _SendWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
            _SendWaitQueue = new ConcurrentQueue<NetworkTraffic>();
        }

        public NetworkClient(long connectedTime, TcpClient tcpClient, NetworkIOCPServer iocpServer)
        {
            _TcpClient = tcpClient;
            _UserSerial = "";
            _ConnectedTime = connectedTime;
            _GeneralLocker = new ReaderWriterLockSlim();
            _NoDelay = true;
            _TcpClient.NoDelay = _NoDelay;
            _IsConnectionAlive = true;
            _Endpoint = _TcpClient.Client.RemoteEndPoint as IPEndPoint;
            _NetworkConnectionType = _Endpoint != null ? NetworkConnectionType.Remote : NetworkConnectionType.Local;
            _Endpoint = _TcpClient.Client.LocalEndPoint as IPEndPoint;
            _IOCPServer = iocpServer;
            _SendWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset);
            _SendWaitQueue = new ConcurrentQueue<NetworkTraffic>();
            _IOCPServer?.OnConnected(this);
        }

        #region Getter/Setter

        public IPEndPoint Endpoint
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _Endpoint;
                }
            }
        }

        public NetworkConnectionType ConnectionType
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _NetworkConnectionType;
                }
            }
        }


        public string Serial
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _UserSerial;
                }
            }
        }


        public Socket ClientSocket
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _TcpClient.Client;
                }
            }
        }

        public TcpClient ClientTcp
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _TcpClient;
                }
            }
        }

        public bool IsConnectionAlive
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _IsConnectionAlive;
                }
            }
        }

        public bool NoDelay
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _NoDelay;
                }
            }
            set
            {
                using (_GeneralLocker.Write())
                {
                    _NoDelay = value;
                    if (_TcpClient != null && _TcpClient.Connected)
                        _TcpClient.NoDelay = true;
                }
            }
        }


        public event OnReceiveCompleteHandler OnReceiveComeplete
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnReceiveComeplete += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnReceiveComeplete -= value;
                }
            }
        }

        public event OnSendCompleteHandler OnSendComeplete
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnSendComeplete += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnSendComeplete -= value;
                }
            }
        }

        public event OnDisconnectedHandler OnDisconnected
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnDisconnected += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnDisconnected -= value;
                }
            }
        }
        #endregion

        public void Start()
        {
            using (_GeneralLocker.Read())
            {
                if (!_IsConnectionAlive)
                    return;
            }

            StartThread();
        }

        public virtual void Disconnect()
        {
            try
            {
                using (_GeneralLocker.Write())
                {
                    if (!_IsConnectionAlive)
                        return;

                    _TcpClient.Client.Shutdown(SocketShutdown.Both);
                    _TcpClient.Close();
                    _IsConnectionAlive = false;
                    _NetworkConnectionType = NetworkConnectionType.NotConnected;
                    _Endpoint = null;
                }

                _OnDisconnected?.Invoke(DateTime.Now.Ticks);
                ((INetworkIOCPClientEventListener)_EventListener)?.OnDisconnected(DateTime.Now.Ticks);
                _IOCPServer?.OnDisconnected(this);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 Shutdown에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }
        }

        public void SetEventListener(INetworkClientEventListener eventListener)
        {
            using (_GeneralLocker.Write())
            {
                _EventListener = eventListener;
            }
        }

        public void Send(NetworkDataWriter networkDataWriter)
        {
            Send(networkDataWriter.AvailableData);
        }

        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
        }

        public void Send(byte[] data, int offset, int dataSize)
        {
            try
            {
                if (IsConnectionAlive == false)
                    throw new Exception("현재 서버와 연결되어 있지 않습니다");

                NetworkTraffic sendTraffic = NetworkTraffic.CreateSendTraffic(data, offset, dataSize, this);

                //만약 크리티컬섹션에 진입한 상태라면 큐에 넣어줬다가 전송끝나면 처리하도록 하자
                if (_SendWaitHandle.WaitOne(0))
                {
                    _TcpClient.Client.BeginSend(
                        sendTraffic.HeaderPacket.TransferingData,
                        sendTraffic.HeaderPacket.Offset,
                        sendTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), sendTraffic);
                }
                else
                {
                    _SendWaitQueue.Enqueue(sendTraffic);
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [1] BeginSend에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }
        }

        private void OnSend(IAsyncResult asyncResult)
        {
            NetworkTraffic sendTraffic = asyncResult.AsyncState as NetworkTraffic;
            NetworkClient sender = sendTraffic.Tag as NetworkClient;
            int sendBytesSize = 0;

            try
            {
                if (_TcpClient != null)
                    sendBytesSize = _TcpClient.Client.EndSend(asyncResult);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 EndSend에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
                return;
            }

            //상태 클라이언트와 연결이 끊어졌을 때...
            if (sendBytesSize <= 0)
            {
                Disconnect();
                return;
            }

            int threadid = Thread.CurrentThread.ManagedThreadId;
            NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + sendBytesSize + "바이트 송신", threadid);

            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringHeader)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "SendingHeader", threadid);
                Sending(sendBytesSize, sendTraffic.HeaderPacket);
            }
            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringHeaderComplete)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "SendingHeader Complete", threadid);
                SendingHeaderComplete(sendBytesSize, sendTraffic.ContentPacket);
                return;
            }

            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringContent)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "SendingContent", threadid);
                Sending(sendBytesSize, sendTraffic.ContentPacket);
            }
            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringContentComplete)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "SendingContent Complete", threadid);
                SendingContentComplete(sendBytesSize, sendTraffic.ContentPacket);
                return;
            }
        }

        private void Sending(int sendBytesSize, NetworkTrafficPacket sendTrafficPacket)
        {
            if (sendBytesSize < sendTrafficPacket.Size)
            {
                //전송을 완전히 못했다면 남은 전송을 마저해준다.
                sendTrafficPacket.Size = sendTrafficPacket.Size - sendBytesSize;
                sendTrafficPacket.Offset = sendTrafficPacket.Offset + sendBytesSize;

                try
                {
                    _TcpClient.Client.BeginSend(
                        sendTrafficPacket.TransferingData,
                        sendTrafficPacket.Offset,
                        sendTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), sendTrafficPacket.Traffic);
                }
                catch (Exception e)
                {
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [2] BeginSend에 실패하였습니다.");
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                    Disconnect();
                }
            }
            else
            {
                //전송이 완료되었다면 다음 스텝으로 진행
                sendTrafficPacket.Traffic.SetNextTransferringStep();
            }
        }

        //헤더 전송이 모두 완료됬을 경우
        private void SendingHeaderComplete(int sendBytesSize, NetworkTrafficPacket nextSendTrafficPacket)
        {
            nextSendTrafficPacket.Traffic.SetNextTransferringStep();
            NetworkLogger.WriteLine(NetworkLogLevel.Debug, "-- SendingHeader Complete : " + nextSendTrafficPacket.Traffic.HeaderPacketInfo);

            try
            {
                _TcpClient.Client.BeginSend(
                    nextSendTrafficPacket.TransferingData,
                    nextSendTrafficPacket.Offset,
                    nextSendTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), nextSendTrafficPacket.Traffic);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [3] BeginSend에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
                return;
            }
        }

        //실제 내용 수신이 모두 완료됫을 경우
        private void SendingContentComplete(int sendBytesSize, NetworkTrafficPacket sendTrafficPacket)
        {
            //어차피 이 데이터는 더이상 다른곳에서 안쓰이므로 복사안하고 레퍼런스만 받아서 넣어줘도 멀티쓰레딩으로 영향 받을일 없을듯
            NetworkDataWriter sendDataWriter =  NetworkDataWriter.FromBytes(sendTrafficPacket.TransferingData, false); 

            _EventListener?.OnSendComplete(sendDataWriter);
            _IOCPServer?.OnSend(sendDataWriter, this);
            _OnSendComeplete?.Invoke(sendDataWriter);

            //송신 대기중인 패킷이 있을 경우 처리진행
            if (_SendWaitQueue.IsEmpty)
            {
                _SendWaitHandle.Set();
                return;
            }

            if (_SendWaitQueue.TryDequeue(out NetworkTraffic sendWaitTraffic))
            {
                try
                {
                    _TcpClient.Client.BeginSend(
                        sendWaitTraffic.HeaderPacket.TransferingData,
                        sendWaitTraffic.HeaderPacket.Offset,
                        sendWaitTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), sendWaitTraffic);
                }
                catch (Exception e)
                {
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [2] BeginSend에 실패하였습니다.");
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                    Disconnect();
                }
            }
            else
            {
                _SendWaitHandle.Set();
            }
        }

        protected override void Execute(object param = null)
        {
            try
            {
                NetworkTraffic receiveTraffic = NetworkTraffic.CreateReceiveTraffic(NetworkPacketHeader.HeaderSize, this);
                _TcpClient.Client.BeginReceive(
                    receiveTraffic.HeaderPacket.TransferingData,
                    receiveTraffic.HeaderPacket.Offset,
                    receiveTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTraffic);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [1] BeginReceive에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
            }
        }

        private void OnReceive(IAsyncResult asyncResult)
        {
            NetworkTraffic receiveTraffic = asyncResult.AsyncState as NetworkTraffic;
            NetworkClient receiver = receiveTraffic.Tag as NetworkClient;
            int receiveBytesSize = 0;

            try
            {
                receiveBytesSize = _TcpClient.Client.EndReceive(asyncResult);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 EndReceive에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
                return;
            }

            if (receiveBytesSize <= 0)
            {
                Disconnect();
                return;
            }

            int threadid = Thread.CurrentThread.ManagedThreadId;
            NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + receiveBytesSize + "바이트 수신", threadid);

            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringHeader)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "ReceivingHeader", threadid);
                Receiving(receiveBytesSize, receiveTraffic.HeaderPacket);
                
            }
            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringHeaderComplete)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "ReceivingHeaderComplete", threadid);
                ReceivingHeaderComplete(receiveBytesSize, receiveTraffic);
                
                return;
            }

            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringContent)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "ReceivingContent", threadid);
                Receiving(receiveBytesSize, receiveTraffic.ContentPacket);
                
            }
            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringContentComplete)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Debug, "[{0}]" + "ReceivingContentComplete", threadid);
                ReceivingContentComplete(receiveBytesSize, receiveTraffic);
                
                return;
            }
        }

        private void Receiving(int receiveBytesSize, NetworkTrafficPacket receiveTrafficPacket)
        {
            if (receiveBytesSize < receiveTrafficPacket.Size)
            {
                //수신을 완전히 못했다면 남은 수신을 마저해준다.
                try
                {
                    receiveTrafficPacket.Size = receiveTrafficPacket.Size - receiveBytesSize;
                    receiveTrafficPacket.Offset = receiveTrafficPacket.Offset + receiveBytesSize;

                    _TcpClient.Client.BeginReceive(
                        receiveTrafficPacket.TransferingData,
                        receiveTrafficPacket.Offset,
                        receiveTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTrafficPacket.Traffic);
                }
                catch (Exception e)
                {
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [2] BeginReceive에 실패하였습니다.");
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                    Disconnect();
                    return;
                }
            }
            else
            {
                //수신이 완료되었다면 다음 스텝으로 진행
                receiveTrafficPacket.Traffic.SetNextTransferringStep();
            }
        }

        //헤더 수신이 모두 완료됬을 경우
        private void ReceivingHeaderComplete(int receiveBytesSize, NetworkTraffic traffic)
        {
            traffic.SetNextTransferringStep();
            NetworkLogger.WriteLine(NetworkLogLevel.Debug, "-- ReceivingHeader Complete : " + traffic.HeaderPacketInfo);
            try
            {
                NetworkPacketHeader packetHeader = NetworkPacketHeader.MakeHeaderFromBytes(traffic.HeaderPacket.TransferingData);
                traffic.ContentPacket = new NetworkTrafficPacket(packetHeader.ShouldReceiveBytesSize, traffic);

                _TcpClient.Client.BeginReceive(
                    traffic.ContentPacket.TransferingData,
                    traffic.ContentPacket.Offset,
                    traffic.ContentPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), traffic);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [3] BeginReceive에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
                return;
            }
        }

        //실제 내용 전송이 모두 완료됫을 경우
        private void ReceivingContentComplete(int receiveBytesSize, NetworkTraffic traffic)
        {
            try
            {
                NetworkDataWriter receiveDataWriter = NetworkDataWriter.FromBytes(traffic.ContentPacket.TransferingData, false);
                NetworkTraffic receiveTraffic = NetworkTraffic.CreateReceiveTraffic(NetworkPacketHeader.HeaderSize, this);
                _TcpClient.Client.BeginReceive(
                    receiveTraffic.HeaderPacket.TransferingData,
                    receiveTraffic.HeaderPacket.Offset,
                    receiveTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTraffic);

                _IOCPServer?.OnReceive(receiveDataWriter, this);
                _EventListener?.OnReceiveComplete(receiveDataWriter);
                _OnReceiveComeplete?.Invoke(receiveDataWriter);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [4] BeginReceive에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
            }
        }
    }
}
