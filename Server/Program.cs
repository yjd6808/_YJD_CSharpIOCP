using Network;
using Network.Server;
using Server.Network.Client;
using Shared.Util;
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

        static void Main(string[] args)
        {
            NetworkIOCPServer iocpServer = new NetworkIOCPServer();
            iocpServer.Start();

            Thread.Sleep(1500);
            NetworkIOCPClient iocpClient = new NetworkIOCPClient()
            {
                ConnectionTimeout = 1500
            };
            iocpClient.Connect("127.0.0.1", 12345);
            iocpClient.OnConnected += (tick) =>
            {
                Console.WriteLine(tick);
            };

            while (true)
            {

            }
        }
    }
}
