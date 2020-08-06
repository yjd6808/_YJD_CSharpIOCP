### 서버 예시

```C#
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
```



### 클라 예시

```c#
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
```

