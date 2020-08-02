// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-01 오후 5:58:25   
// @PURPOSE     : IOCP 서버
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Network.Logger;
using Shared.Util;

namespace Network.Server
{
    public delegate void OnServerStartedHandler(long tick);
    public delegate void OnServerStoppedHandler(long tick);

    public delegate void OnClientConnectedHandler(NetworkClient client);
    public delegate void OnClientDisconnectedHandler(NetworkClient client);

    public class NetworkIOCPServer : OveridableThread, INetworkBase
    {
        // 제너럴 변수들
        private INetworkLogger _Logger;
        private TcpListener _Listener;
        private volatile bool _IsRunning;
        private readonly ushort _ListeningPort;
        private ReaderWriterLockSlim _GeneralLocker;

        //접속 승인 대기중인 클라이언트
        private readonly List<NetworkClient> _WaitingClientList;
        private readonly ReaderWriterLockSlim _WaitingClientLocker;

        public event OnServerStartedHandler OnServerStarted;
        public event OnServerStoppedHandler OnServerStopped;
        public event OnClientConnectedHandler OnClientConnected;
        public event OnClientDisconnectedHandler OnClientDisconnected;
        public event OnSendPacketHandler OnSendPacket;
        public event OnReceivePacketHandler OnReceivePacket;

        public NetworkIOCPServer(ushort listeningPort = 0)
        {
            //포트 설정없이 시작한경우 디폴트 포트값을 넣어준다.
            if (listeningPort == 0)
                _ListeningPort = NetworkConstant.DefaultListeningPort;
            else
                _ListeningPort = listeningPort;

            _IsRunning = false;
            _Listener = null;
            _Logger = null;
            _GeneralLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _WaitingClientLocker = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
            _WaitingClientList = new List<NetworkClient>();
        }

        public void SetLogger(INetworkLogger logger)
        {
            NetworkLogger.SetLogger(logger);
        }

        public bool IsRunning
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _IsRunning;
                }
            }
        }

        /// <summary>
        /// 서버 시작
        /// </summary>
        public void Start()
        {
            lock (_GeneralLocker)
            {
                if (_IsRunning)
                {
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, "이미 서버가 시작된 상태입니다.");
                    return;
                }
            }
            StartThread();
        }
        
        /// <summary>
        /// 서버 종료
        /// </summary>
        public void Stop()
        {
            _GeneralLocker.EnterWriteLock();
            try
            {
                _Listener.Stop();
                _Listener = null;
            }
            finally
            {
                _GeneralLocker.ExitWriteLock();
            }
        }

        protected override void Execute()
        {
            _GeneralLocker.EnterWriteLock();
            try
            {
                _IsRunning = true;
                _Listener = TcpListener.Create(_ListeningPort);
                _Listener.Start();
                _Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                _Listener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientAccepted), null);
                OnServerStarted?.Invoke(DateTime.Now.Ticks);
                NetworkLogger.WriteLine(NetworkLogLevel.Info, "서버가 {0}포트에서 수신 대기중입니다...", _ListeningPort);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "서버 시작에 실패했습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Stop();
            }
            finally
            {
                _GeneralLocker.ExitWriteLock();
            }
        }

        /// <summary>
        /// TCP 클라이언트 Accept 성공시
        /// </summary>
        /// <param name="asyncResult">비동기 결과</param>
        private void OnTcpClientAccepted(IAsyncResult asyncResult)
        {
            TcpClient acceptedTcp = null;
            NetworkClient acceptedClient = null;

            //클라 Accept
            _GeneralLocker.EnterWriteLock();
            try
            {
                if (_Listener != null)
                {
                    acceptedTcp = _Listener.EndAcceptTcpClient(asyncResult);
                    acceptedClient = new NetworkClient(DateTime.Now.Ticks, acceptedTcp, this);
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [1] Accept에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }
            finally
            {
                _GeneralLocker.EnterWriteLock();
            }

            //대기 큐에 넣어줌
            using (_WaitingClientLocker.Write())
            {
                _WaitingClientList.Add(acceptedClient);
            }

            //다시 수신 대기상태로 둠
            _GeneralLocker.EnterWriteLock();
            try
            {
                if (_Listener != null)
                {
                    _Listener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientAccepted), null);
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 BeginAccept에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Stop();
            }
            finally
            {
                _GeneralLocker.EnterWriteLock();
            }

            //접속한 클라를 수신가능한 상태로 둠
            acceptedClient.Start();
        }
    }
}
