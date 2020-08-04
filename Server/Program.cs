using CSharpSimpleIOCP.Network.Client;
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
        static int data = 0;

        static void Main(string[] args)
        {
            //var locker = new ReaderWriterLockSlim();


            //new Thread(() =>
            //{
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        using (locker.Write())
            //        {
            //            data--;
            //        }
            //    }
            //}).Start();

            //new Thread(() =>
            //{
            //    for (int i = 0; i < 100000; i++)
            //    {
            //        using (locker.Write())
            //        {
            //            data++;

            //            Thread.Sleep(150000);
            //        }


            //    }
            //}).Start();

            //new Thread(() =>
            //{
            //    while (true)
            //    {
            //        using (locker.Read())
            //        {
            //            Console.WriteLine(data);
            //            Thread.Sleep(100);
            //        }
            //    }
            //}).Start();


            NetworkIOCPServer iocpServer = new NetworkIOCPServer();
            iocpServer.Start();
            iocpServer.OnReceiveComplete += (data, client) =>
            {
                Console.WriteLine(client.Endpoint + " / " + data[0] + "수신");
            };

            Thread.Sleep(1500);
            NetworkIOCPClient iocpClient = new NetworkIOCPClient()
            {
                ConnectionTimeout = 1500
            };
            iocpClient.Connect("127.0.0.1", 12345);
            iocpClient.OnConnected += (tick) =>
            {
                Console.WriteLine(tick);

                iocpClient.Send(new byte[1] { 1 }, 0, 1);
                iocpClient.Send(new byte[1] { 2 }, 0, 1);
                //iocpClient.Send(new byte[1] { 3 }, 0, 1);
                //iocpClient.Send(new byte[1] { 41 }, 0, 1);
            };

            

            while (true)
            {

            }
        }
    }
}
