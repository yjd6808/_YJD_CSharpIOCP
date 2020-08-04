// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-04 오후 9:13:20   
// @PURPOSE     : 클라이언트 이벤트 리스너
// ===============================

using CSharpSimpleIOCP.Network.Server;

namespace CSharpSimpleIOCP.Network.Client
{
    public interface INetworkIOCPClientEventListener : INetworkClientEventListener
    {
        void OnConnected(long tick);
        void OnDisconnected(long tick);

    }
}
