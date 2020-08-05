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
        class Threading
        {
            private int count = 0;
            public static EventWaitHandle _waitHandle;

            public Threading()
            {
                Console.Write("1:AutoResetEvent\t 2:ManualResetEvent - ");
                switch (Console.ReadKey().KeyChar)
                {
                    case '1':
                        _waitHandle = new AutoResetEvent(true);
                        break;
                    case '2':
                        _waitHandle = new ManualResetEvent(true);
                        break;
                }
                Console.WriteLine("");

                Thread T1 = new Thread(new ThreadStart(Work));
                Thread T2 = new Thread(new ThreadStart(Work));
                Thread T3 = new Thread(new ThreadStart(Work));
                Thread T4 = new Thread(new ThreadStart(Work));

                T1.Start();
                T2.Start();
                T3.Start();
                T4.Start();
            }

            private void Work()
            {
                Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] " + "WaitOne Call!");
                _waitHandle.Reset(); //true -> false로 만듬
                _waitHandle.WaitOne(); //true 상태여야 지나갈 수 있음
                for (int i = 0; i < 5; i++)
                {
                    Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] " + count++);
                    Thread.Sleep(1000);
                }
                _waitHandle.Set(); //false -> true로 만듬
            }
        }
        public static EventWaitHandle _waitHandle;

        static void Main(string[] args)
        {
            //Threading t = new Threading();
            //Console.ReadKey();
            NetworkLogger.SetPrintEnable((NetworkLogLevel.FullOption & ~NetworkLogLevel.Debug));

            new Thread(() =>
            {
                NetworkIOCPServer iocpServer = new NetworkIOCPServer();
                iocpServer.Start();
                iocpServer.OnClientConnected += (client) =>
                {
                    Console.WriteLine("[서버]" + client.Endpoint + " 클라가 접속했습니다");
                    client.Send(new byte[1]);
                };
                iocpServer.OnClientDisconnected += (client) =>
                {
                    Console.WriteLine("[서버] " + client.Endpoint + " 클라가 접속을 종료했습니다.");
                };

                iocpServer.OnReceiveComplete += (data, client) =>
                {
                    Console.WriteLine("[서버] " + client.Endpoint + " / " + data[0] + "수신");
                };

                iocpServer.OnSendComplete += (data, client) =>
                {
                    Console.WriteLine("[서버] " + client.Endpoint + " / " + data[0] + "송신");
                };

                iocpServer.OnServerStopped += (tick) =>
                {
                    Console.WriteLine("[서버] " + tick + " tick 타임에 서버가 종료되었습니다");
                };

                iocpServer.OnServerStarted += (tick) =>
                {
                    Console.WriteLine("[서버] " + tick + " tick 타임에 서버가 시작되었습니다");
                };

                while (true)
                {

                }
            }).Start();

            Thread.Sleep(1500);

            new Thread(() =>
            {
                NetworkIOCPClient iocpClient = new NetworkIOCPClient()
                {
                    ConnectionTimeout = 1500,
                    NoDelay = true
                };

                iocpClient.Connect("221.162.129.150", 12345);

                iocpClient.OnConnected += (tick) =>
                {
                    Console.WriteLine("[클라] " + tick + " 타임에 서버에 연결되었습니다.");
                    iocpClient.Send(new byte[1]);
                    //iocpClient.Send(new byte[1], 0, 1);
                    //iocpClient.Send(new byte[1], 0, 1);
                    //iocpClient.Disconnect();
                };
                iocpClient.OnDisconnected += (tick) =>
                {
                    Console.WriteLine("[클라] " + tick + " 타임에 서버와 연결이 끊어졌습니다.");
                };
                iocpClient.OnSendComeplete += (data) =>
                {
                    Console.WriteLine("[클라] " + "서버에 " + data.Length + " 바이트를 전송했습니다");
                };
                iocpClient.OnReceiveComeplete += (data) =>
                {
                    Console.WriteLine("[클라] " + "서버로부터 " + data.Length + " 바이트를 수신했습니다.");
                };

                while (true)
                {

                }
            }).Start();
            

            while (true)
            {

            }
        }
    }
}
