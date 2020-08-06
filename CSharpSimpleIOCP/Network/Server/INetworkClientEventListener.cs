// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-04 오후 9:21:07   
// @PURPOSE     : 클라 이벤트리스너 베이스 / 만약쓴다면 서버쪽에서 소켓별로 이벤트처리 할때? 실제쓰는 클라전용 리스너는 INetworkIOCPClientEventListener 이거임
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
        void OnSendComplete(NetworkDataWriter networkDataWriter);
        void OnReceiveComplete(NetworkDataWriter networkDataWriter);
    }
}
