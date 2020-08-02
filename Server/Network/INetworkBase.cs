// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-02 오전 11:47:35   
// ===============================

using Network.Logger;

namespace Network
{
    interface INetworkBase
    {
        void SetLogger(INetworkLogger logger);
    }
}
