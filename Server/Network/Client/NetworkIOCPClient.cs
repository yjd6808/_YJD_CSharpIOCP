// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-03 오후 7:43:34   
// @PURPOSE     : IOCP 클라이언트
// ===============================

using Network;
using Network.Logger;
using Network.Server;
using Shared.Util;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Server.Network.Client
{
    public static class NetworkIOCPClientDelegates
    {
        public delegate void OnConnectedHandler(long tick);
        public delegate void OnDisconnectedHandler(long tick);
        public delegate void OnReceiveCompleteHandler(byte[] bytes);
        public delegate void OnSendCompleteHandler(byte[] bytes);
    }

    public class NetworkIOCPClient : NetworkClient
    {
        private int _ConnectionTimeout;

        public event NetworkIOCPClientDelegates.OnConnectedHandler OnConnected;
        public event NetworkIOCPClientDelegates.OnDisconnectedHandler OnDisconnected;
        public event NetworkIOCPClientDelegates.OnSendCompleteHandler OnSendPacket;
        public event NetworkIOCPClientDelegates.OnReceiveCompleteHandler OnReceivePacket;

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
                _TcpClient.EndConnect(asyncResult);
                NetworkLogger.WriteLine(NetworkLogLevel.Info, _Endpoint + " 서버에 접속하였습니다.");
                OnConnected?.Invoke(DateTime.Now.Ticks);
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
