// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-02 오전 1:20:05   
// @PURPOSE     : 비동기 클라이언트
// ===============================


using Network.Server;
using Network.Logger;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Network.Server
{
    public delegate void OnSendPacketHandler(int sendBytes);
    public delegate void OnReceivePacketHandler(int sendBytes);

    public enum NetworkConnectionType
    {
        Local,
        Remote
    }

    public class NetworkClient : OveridableThread
    {
        private string _UserSerial;                           //클라 ID - 절대 중복되면 안됨
        private long _ConnectedTime;                
        private TcpClient _TcpClient;               
        private volatile bool _IsConnectionAlive;             //서버와 연결상태 여부
        private ReaderWriterLockSlim _GeneralLocker;
        private NetworkConnectionType _NetworkConnectionType; //연결타입 - 로컬접속인지 외부접속인지
        private IPEndPoint _Endpoint;                         //접속자의 아이피
        private NetworkIOCPServer _IOCPServer;

        public NetworkClient(long connectedTime, TcpClient  tcpClient, NetworkIOCPServer iocpServer)
        {
            _TcpClient = tcpClient;
            _UserSerial = "";
            _ConnectedTime = connectedTime;
            _GeneralLocker = new ReaderWriterLockSlim();
            _IsConnectionAlive = true;
            _Endpoint = _TcpClient.Client.RemoteEndPoint as IPEndPoint;
            _NetworkConnectionType = _Endpoint != null ? NetworkConnectionType.Remote : NetworkConnectionType.Local;
            _Endpoint = _TcpClient.Client.LocalEndPoint as IPEndPoint;
            _IOCPServer = iocpServer;
        }

        #region Getter
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

        public void Disconnect()
        {
            NetworkLogger.WriteLine(NetworkLogLevel.Info, _Endpoint + " 클라이언트의 접속이 끊어졌습니다");

            try
            {
                using (_GeneralLocker.Write())
                {
                    if (!_IsConnectionAlive)
                        return;

                    _TcpClient.Client.Shutdown(SocketShutdown.Both);
                    _TcpClient.Close();
                    _IsConnectionAlive = false;
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 Shutdown에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }

            
            //서버 연결되었지만 확인안된 리스트에서 제거
            //서버 연결중인 리스트에서 제거
        }

        public void Send(byte[] data)
        {
            Send(data, 0, data.Count());
        }

        public void Send(byte[] data, int offset, int dataSize)
        {
            try
            {
                using (_GeneralLocker.Write())
                {
                    NetworkTraffic sendTraffic = NetworkTraffic.CreateSendTraffic(data, offset, dataSize, this);
                    _TcpClient.Client.BeginSend(
                        sendTraffic.HeaderPacket.TransferingData, 
                        sendTraffic.HeaderPacket.Offset, 
                        sendTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), sendTraffic);
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

            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringHeader)
                Sending(sendBytesSize, sendTraffic.HeaderPacket);
            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringHeaderComplete)
            {
                SendingHeaderComplete(sendBytesSize, sendTraffic.ContentPacket);
                return;
            }

            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringContent)
                Sending(sendBytesSize, sendTraffic.ContentPacket);
            if (sendTraffic.Status == NetworkTrafficStep.OnTransferringContentComplete)
            {
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
                    using (_GeneralLocker.Write())
                    {
                        _TcpClient.Client.BeginSend(
                            sendTrafficPacket.TransferingData,
                            sendTrafficPacket.Offset,
                            sendTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), sendTrafficPacket.Traffic);
                    }

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

            try
            {
                using (_GeneralLocker.Write())
                {
                    _TcpClient.Client.BeginSend(
                        nextSendTrafficPacket.TransferingData,
                        nextSendTrafficPacket.Offset,
                        nextSendTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnSend), nextSendTrafficPacket.Traffic);
                }
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

        //실제 내용 전송이 모두 완료됫을 경우
        private void SendingContentComplete(int sendBytesSize, NetworkTrafficPacket sendTrafficPacket)
        {
        }

        protected override void Execute()
        {
            try
            {
                using (_GeneralLocker.Write())
                {
                    NetworkTraffic receiveTraffic = NetworkTraffic.CreateReceiveTraffic(NetworkPacketHeader.HeaderSize, this);
                    _TcpClient.Client.BeginReceive(
                        receiveTraffic.HeaderPacket.TransferingData,
                        receiveTraffic.HeaderPacket.Offset,
                        receiveTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTraffic);
                }
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

            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringHeader)
                Receiving(receiveBytesSize, receiveTraffic.HeaderPacket);
            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringHeaderComplete)
            {
                ReceivingHeaderComplete(receiveBytesSize, receiveTraffic);
                return;
            }

            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringContent)
                Receiving(receiveBytesSize, receiveTraffic.ContentPacket);
            if (receiveTraffic.Status == NetworkTrafficStep.OnTransferringContentComplete)
            {
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

                    using (_GeneralLocker.Write())
                    {
                        _TcpClient.Client.BeginReceive(
                            receiveTrafficPacket.TransferingData,
                            receiveTrafficPacket.Offset,
                            receiveTrafficPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTrafficPacket.Traffic);
                    }
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
            try
            {
                NetworkPacketHeader packetHeader = NetworkPacketHeader.MakeHeaderFromBytes(traffic.HeaderPacket.TransferingData);
                traffic.ContentPacket = new NetworkTrafficPacket(packetHeader.ShouldReceiveBytesSize, traffic);

                using (_GeneralLocker.Write())
                {
                    _TcpClient.Client.BeginReceive(
                        traffic.ContentPacket.TransferingData,
                        traffic.ContentPacket.Offset,
                        traffic.ContentPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), traffic);
                }
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
                Console.WriteLine(traffic.ContentPacket.TransferingData[0] + " Echo Send");
                Send(traffic.ContentPacket.TransferingData, 0, 1);
                using (_GeneralLocker.Write())
                {
                    NetworkTraffic receiveTraffic = NetworkTraffic.CreateReceiveTraffic(NetworkPacketHeader.HeaderSize, this);
                    _TcpClient.Client.BeginReceive(
                        receiveTraffic.HeaderPacket.TransferingData,
                        receiveTraffic.HeaderPacket.Offset,
                        receiveTraffic.HeaderPacket.Size, SocketFlags.None, new AsyncCallback(OnReceive), receiveTraffic);
                }
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
