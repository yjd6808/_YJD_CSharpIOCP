// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-01 오후 7:27:43   
// @PURPOSE     : 로거 인터페이스 사용자지정 로거를 만들수있도록 함
// ===============================

namespace CSharpSimpleIOCP.Network.Logger
{
    public interface INetworkLogger
    {
        void Write(NetworkLogLevel logLevel, string msg, object arg0);

        void Write(NetworkLogLevel logLevel, string msg, params object[] args);

    }
}
