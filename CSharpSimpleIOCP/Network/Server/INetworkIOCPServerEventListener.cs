// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-04 오후 9:24:39   
// @PURPOSE     : IOCP 서버 이벤트리스너
// ===============================


namespace CSharpSimpleIOCP.Network.Server
{
    public interface INetworkIOCPServerEventListener
    {
        void OnServerStarted(long tick);
        void OnServerStopped(long tick);
        void OnClientConnected(NetworkClient client);
        void OnClientDisconnected(NetworkClient client);
        void OnSendComplete(NetworkDataWriter networkDataWriter, NetworkClient targetClient);
        void OnReceiveComplete(NetworkDataWriter networkDataWriter, NetworkClient targetClient);
    }
}
