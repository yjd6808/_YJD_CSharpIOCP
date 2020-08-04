// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-03 오후 7:43:34   
// @PURPOSE     : IOCP 클라이언트 - 서버에서 쓰는 NetworkClient를 확장해서씀.
// ===============================

using CSharpSimpleIOCP.Network.Logger;
using CSharpSimpleIOCP.Network.Server;
using CSharpSimpleIOCP.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace CSharpSimpleIOCP.Network.Client
{
    public class NetworkIOCPClient : NetworkClient
    {
        private int _ConnectionTimeout;

        #region 델리게이트와 이벤트들
        public delegate void OnConnectedHandler(long tick);
        public delegate void OnDisconnectedHandler(long tick);

        private event OnConnectedHandler _OnConnected;
        private event OnDisconnectedHandler _OnDisconnected;
        #endregion

        public NetworkIOCPClient() : base()
        {
            _ConnectionTimeout = 1500;
        }

        #region Getter/Setter
        public string UserSerial
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _UserSerial;
                }
            }
        }

        public string IsConnected
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _UserSerial;
                }
            }
        }

        public long ConnectedTime
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _ConnectedTime;
                }
            }
        }

        public int ConnectionTimeout
        {
            get
            {
                using (_GeneralLocker.Read())
                {
                    return _ConnectionTimeout;
                }
            }
            set
            {
                using (_GeneralLocker.Write())
                {
                    _ConnectionTimeout = value;
                }
            }
        }

        public event OnConnectedHandler OnConnected
        {
            add
            {
                using (_GeneralLocker.Write())
                {
                    _OnConnected += value;
                }
            }

            remove
            {
                using (_GeneralLocker.Write())
                {
                    _OnConnected -= value;
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



        public void SetLogger(INetworkLogger logger)
        {
            NetworkLogger.SetLogger(logger);
        }

        public override void Disconnect()
        {
            base.Disconnect();

            using (_GeneralLocker.Write())
            {
                _TcpClient = null;
            }

            _OnConnected?.Invoke(DateTime.Now.Ticks);
            ((INetworkIOCPClientEventListener)_EventListener)?.OnDisconnected(DateTime.Now.Ticks);
        }

        public void Connect(IPEndPoint targetEndpoint)
        {
            Connect(targetEndpoint.Address.ToString(), targetEndpoint.Port);
        }

        public void Connect(IPAddress address, int port)
        {
            Connect(address.ToString(), port);
        }

        public void Connect(string hostname, int port)
        {
            using (_GeneralLocker.Write())
            {
                if (_TcpClient != null && _TcpClient.Connected)
                {
                    NetworkLogger.WriteLine(NetworkLogLevel.Error, "이미 서버와 연결되어있습니다.");
                    return;
                }
            }

            try
            {
                StartThreadWithParam(new IPEndPoint(IPAddress.Parse(hostname), port));
            }
            catch  (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }
        }

        protected override void Execute(object param)
        {
            IPEndPoint targetEndpoint = param as IPEndPoint;

            try
            {
                using (_GeneralLocker.Write())
                {
                    _TcpClient = new TcpClient();
                    _Endpoint = targetEndpoint;
                    _TcpClient.BeginConnect(targetEndpoint.Address, targetEndpoint.Port, new AsyncCallback(OnTcpServerConnected), this)
                        .AsyncWaitHandle.WaitOne(_ConnectionTimeout);
                }
            }
            catch (Exception e )
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "서버 접속에 실패했습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);

                Disconnect();
            }
        }

        private void OnTcpServerConnected(IAsyncResult asyncResult)
        {
            try
            {
                using (_GeneralLocker.Write())
                {
                    if (_TcpClient == null)
                        throw new Exception("클라이언트의 연결이 이미 끊어졌습니다.");

                    _TcpClient.EndConnect(asyncResult);
                    _IsConnectionAlive = true;
                }

                _OnConnected?.Invoke(DateTime.Now.Ticks);
                ((INetworkIOCPClientEventListener)_EventListener)?.OnConnected(DateTime.Now.Ticks);

                NetworkLogger.WriteLine(NetworkLogLevel.Info, _Endpoint + " 서버에 접속하였습니다.");
            }
            catch (Exception e)
            {
                NetworkLogger.WriteLine(NetworkLogLevel.Error, "서버 접속에 실패했습니다.");
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.Message);
                NetworkLogger.WriteLine(NetworkLogLevel.Error, e.StackTrace);
            }
        }
    }
}
