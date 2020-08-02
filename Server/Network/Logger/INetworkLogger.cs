// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-01 오후 7:27:43   
// @PURPOSE     : 
// ===============================

namespace Network.Logger
{
    public interface INetworkLogger
    {
        void Write(NetworkLogLevel logLevel, string msg, params object[] args);

    }
}
