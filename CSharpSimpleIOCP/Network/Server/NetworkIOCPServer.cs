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
using CSharpSimpleIOCP.Network.Logger;
using CSharpSimpleIOCP.Util;

namespace CSharpSimpleIOCP.Network.Server
{


    public class NetworkIOCPServer : NetworkOveridableThread, INetworkBase
    {
        // 제너럴 변수들
        private TcpListener _Listener;
        private volatile bool _IsRunning;
        private readonly ushort _ListeningPort;
        private ReaderWriterLockSlim _GeneralLocker;
        private INetworkIOCPServerEventListener _EventListener;

        //접속 승인 대기중인 클라이언트
        private readonly List<NetworkClient> _WaitingClientList;
        private readonly ReaderWriterLockSlim _WaitingClientLocker;

        #region 델리게이트와 이벤트들
        public delegate void OnServerStartedHandler(long tick);
        public delegate void OnServerStoppedHandler(long tick);
        public delegate void OnClientConnectedHandler(NetworkClient client);
        public delegate void OnClientDisconnectedHandler(NetworkClient client);
        public delegate void OnReceiveCompleteHandler(byte[] bytes, NetworkClient targetClient);
        public delegate void OnSendCompleteHandler(byte[] bytes, NetworkClient targetClient);

        private event OnServerStartedHandler _OnServerStarted;
        private event OnServerStoppedHandler _OnServerStopped;
        private event OnClientConnectedHandler _OnClientConnected;
        private event OnClientDisconnectedHandler _OnClientDisconnected;
        private event OnSendCompleteHandler _OnSendComplete;
        private event OnReceiveCompleteHandler _OnReceiveComplete;
        #endregion



        public NetworkIOCPServer(ushort listeningPort = 0)
        {
            //포트 설정없이 시작한경우 디폴트 포트값을 넣어준다.
            if (listeningPort == 0)
                _ListeningPort = NetworkConstant.DefaultListeningPort;
            else
                _ListeningPort = listeningPort;

            _IsRunning = false;
            _Listener = null;
            _GeneralLocker = new ReaderWriterLockSlim();
            _WaitingClientLocker = new ReaderWriterLockSlim();
            _WaitingClientList = new List<NetworkClient>();
            _EventListener = null;
        }

        public void SetLogger(INetworkLogger logger)
        {
            NetworkLogger.SetLogger(logger);
        }

        public void SetEventListener(INetworkIOCPServerEventListener eventListener)
        {
            using (_GeneralLocker.Write())
            {
                _EventListener = eventListener;
            }
        }

        #region Getter/Setter
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

        public event OnServerStartedHandler OnServerStarted
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnServerStarted += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnServerStarted -= value;
                }
            }
        }

        public event OnServerStoppedHandler OnServerStopped
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnServerStopped += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnServerStopped -= value;
                }
            }
        }

        public event OnClientConnectedHandler OnClientConnected
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnClientConnected += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnClientConnected -= value;
                }
            }
        }

        public event OnClientDisconnectedHandler OnClientDisconnected
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnClientDisconnected += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnClientDisconnected -= value;
                }
            }
        }

        public event OnSendCompleteHandler OnSendComplete
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnSendComplete += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnSendComplete -= value;
                }
            }
        }

        public event OnReceiveCompleteHandler OnReceiveComplete
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnReceiveComplete += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnReceiveComplete -= value;
                }
            }
        }

        internal INetworkIOCPServerEventListener EventListener
        {
            get
            {
                return _EventListener;
            }
        }

        #endregion

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
            using (_GeneralLocker.Write())
            {
                _Listener.Stop();
                _Listener = null;
                _IsRunning = false;
                _OnServerStopped?.Invoke(DateTime.Now.Ticks);
                _EventListener?.OnServerStopped(DateTime.Now.Ticks);
            }

            using (_WaitingClientLocker.Write())
            {
                _WaitingClientList.Clear();
            }
        }

        protected override void Execute(object param)
        {
            try
            {
                using (_GeneralLocker.Write())
                {
                    _IsRunning = true;
                    _Listener = TcpListener.Create(_ListeningPort);
                    _Listener.Start();
                    _Listener.Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _Listener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientAccepted), null);
                }

                _OnServerStarted?.Invoke(DateTime.Now.Ticks);
                _EventListener?.OnServerStarted(DateTime.Now.Ticks);
                NetworkLogger.WriteLine(NetworkLogLevel.Info, "서버가 {0}포트에서 수신 대기중입니다...", _ListeningPort);
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "서버 시작에 실패했습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Stop();
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
            try
            {
                using (_GeneralLocker.Write())
                {
                    if (_Listener != null)
                    {
                        acceptedTcp = _Listener.EndAcceptTcpClient(asyncResult);
                        acceptedClient = new NetworkClient(DateTime.Now.Ticks, acceptedTcp, this);
                    }
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 [1] Accept에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }

            //대기 큐에 넣어줌
            using (_WaitingClientLocker.Write())
            {
                _WaitingClientList.Add(acceptedClient);
            }

            //다시 수신 대기상태로 둠
            try
            {
                using (_GeneralLocker.Write())
                {
                    if (_Listener != null)
                    {
                        _Listener.BeginAcceptTcpClient(new AsyncCallback(OnTcpClientAccepted), null);
                    }
                }
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "클라이언트 BeginAccept에 실패하였습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Stop();
            }

            //접속한 클라를 수신가능한 상태로 둠
            acceptedClient.Start();
        }

        /// <summary>
        /// 소켓의 연결종료를 서버쪽에서 처리하기위해 정의
        /// 외부에서는 이 함수를 보면 안되기때문에 internal 접근제어 한정자를 줌
        /// </summary>
        /// <param name="bytes">수신받은 바이트</param>
        /// <param name="targetClient">이 데이터를 받을 클라이언트</param>

        internal void OnDisconnected(NetworkClient targetClient)
        {
            _EventListener?.OnClientDisconnected(targetClient);
            _OnClientDisconnected?.Invoke(targetClient);
        }

        internal void OnConnected(NetworkClient targetClient)
        {
            _EventListener?.OnClientConnected(targetClient);
            _OnClientConnected?.Invoke(targetClient);
        }

        /// <summary>
        /// 소켓의 수신 결과를 서버쪽에서 처리하기위해 정의
        /// 외부에서는 이 함수를 보면 안되기때문에 internal 접근제어 한정자를 줌
        /// </summary>
        /// <param name="bytes">수신받은 바이트</param>
        /// <param name="targetClient">이 데이터를 보낸 클라이언트</param>
        internal void OnReceive(byte[] bytes, NetworkClient targetClient)
        {
            _OnReceiveComplete?.Invoke(bytes, targetClient);
            _EventListener?.OnReceiveComplete(bytes, targetClient);
        }

        /// <summary>
        /// 소켓의 송신 결과를 서버쪽에서 처리하기위해 정의
        /// 외부에서는 이 함수를 보면 안되기때문에 internal 접근제어 한정자를 줌
        /// </summary>
        /// <param name="bytes">수신받은 바이트</param>
        /// <param name="targetClient">이 데이터를 받을 클라이언트</param>

        internal void OnSend(byte[] bytes, NetworkClient targetClient)
        {
            _OnSendComplete?.Invoke(bytes, targetClient);
            _EventListener?.OnSendComplete(bytes, targetClient);
        }
    }
}
