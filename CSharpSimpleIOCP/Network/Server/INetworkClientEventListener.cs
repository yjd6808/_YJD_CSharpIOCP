// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-04 오후 9:21:07   
// @PURPOSE     : 
// ===============================


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpSimpleIOCP.Network.Server
{
    public interface INetworkClientEventListener
    {
        void OnSendComplete(byte[] sendBytes);
        void OnReceiveComplete(byte[] receiveBytes);
    }
}
