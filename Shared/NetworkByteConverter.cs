// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-07-25 오후 4:19:30
// @PURPOSE     : 직렬화, 역질렬화 해준느 클래스
// @EMAIL       : wjdeh10110@gmail.com
// ===============================


using NetworkShared;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetworkShared
{
    public static class NetworkByteConverter
    {
        public static byte[] ToByteArray(this INetworkPacket packet)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream Stream = new MemoryStream();

            formatter.Serialize(Stream, packet);
            return Stream.ToArray();
        }

        public static INetworkPacket ToNetworkPacket(this byte[] bytes)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            MemoryStream Stream = new MemoryStream();

            Stream.Write(bytes, 0, bytes.Length);
            Stream.Seek(0, SeekOrigin.Begin);

            INetworkPacket clientInfo = (INetworkPacket)formatter.Deserialize(Stream);

            return clientInfo;
        }

    }

}