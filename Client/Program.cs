using CSharpSimpleIOCP.Network;
using CSharpSimpleIOCP.Network.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {

        private static void Client_OnSendComeplete(NetworkDataWriter networkDataWriter)
        {
            Console.WriteLine("서버로 전송 : " + networkDataWriter.ReadString());
        }

        private static void Client_OnReceiveComeplete(NetworkDataWriter networkDataWriter)
        {
            Console.WriteLine("서버로부터 수신 : " + networkDataWriter.ReadString());
        }

        private static void Client_OnDisconnected(long tick)
        {
            Console.WriteLine(new DateTime(tick).ToString("yyyy-MM-dd HH-mm-ss") + " 서버와 연결이 끊어졌습니다.");
        }

        private static void Client_OnConnected(long tick)
        {
            Console.WriteLine(new DateTime(tick).ToString("yyyy-MM-dd HH-mm-ss") + " 서버와 연결되었습니다.");

            while (true)
            {
                string read = Console.ReadLine();
                client.Send(NetworkDataWriter.FromString(read));
            }
        }

        static NetworkIOCPClient client = new NetworkIOCPClient();
        static void Main(string[] args)
        {
            client.Connect("127.0.0.1", 12345);

            //이벤트 연결 방식
            client.OnConnected += Client_OnConnected;
            client.OnDisconnected += Client_OnDisconnected;
            client.OnReceiveComeplete += Client_OnReceiveComeplete;
            client.OnSendComeplete += Client_OnSendComeplete; ;

            while (true)
            {
                //끝나지마라
            }
        }

       
    }
}
