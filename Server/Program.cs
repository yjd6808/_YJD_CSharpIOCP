using Network;
using Network.Server;
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
            NetworkIOCPServer server = new NetworkIOCPServer(12345);
            server.Start();

            ReaderWriterLockSlim a = new ReaderWriterLockSlim();
            int data = 0;
            Thread b = new Thread(() =>
            {
                TcpClient a = new TcpClient();
                a.Connect("127.0.0.1", 12345);
                a.GetStream().Write(NetworkPacketHeader.MakeHeaderBytes(1), 0, 16);
                a.GetStream().Write(new byte[1] { 1 }, 0, 1);

                byte[] af = new byte[16];
                a.GetStream().Read(af, 0, 16);
                a.GetStream().Read(af, 0, 1);

                Console.WriteLine("수숴ㅡ수수사ㅣㄴ : " + af[0]);
            });
            b.Start();

            while (true)
            {
            }
        }
    }
}
