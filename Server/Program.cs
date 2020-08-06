using CSharpSimpleIOCP.Network;
using CSharpSimpleIOCP.Network.Client;
using CSharpSimpleIOCP.Network.Logger;
using CSharpSimpleIOCP.Network.Server;
using CSharpSimpleIOCP.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        public class ServerEventListener : INetworkIOCPServerEventListener
        {
            public void OnClientConnected(NetworkClient client)
            {
                Console.Write(client.Endpoint + "가 접속했습니다.");
            }

            public void OnClientDisconnected(NetworkClient client)
            {
                Console.Write(client.Endpoint + "가 접속을 종료했습니다.");
            }

            public void OnReceiveComplete(NetworkDataWriter networkDataWriter, NetworkClient targetClient)
            {
                //에코전송
                Console.WriteLine("클라로부터 수신 : " + networkDataWriter.PeekString());
                targetClient.Send(networkDataWriter);
            }

            public void OnSendComplete(NetworkDataWriter networkDataWriter, NetworkClient targetClient)
            {
                Console.WriteLine("클라로 전송완료 : " + networkDataWriter.PeekString());
            }

            public void OnServerStarted(long tick)
            {
                Console.WriteLine(new DateTime(tick).ToString("yyyy-MM-dd HH-mm-ss") + " 서버가 시작되었습니다.");
            }

            public void OnServerStopped(long tick)
            {
                Console.WriteLine(new DateTime(tick).ToString("yyyy-MM-dd HH-mm-ss") + " 서버가 종료되었습니다.");
            }
        }

        static void Main(string[] args)
        {
            ServerEventListener serverEventListener = new ServerEventListener();
            NetworkIOCPServer iocpServer = new NetworkIOCPServer();
            iocpServer.Start();
            iocpServer.SetEventListener(serverEventListener);

            while (true)
            {
                //끝나지마라.
            }
        }
    }
}
