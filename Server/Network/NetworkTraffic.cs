// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-02 오전 9:31:02   
// @PURPOSE     : 비동기 송수신 트래픽
// ===============================


using NetworkShared;
using Shared.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    public enum NetworkTrafficStep : int
    {
        OnTransferringHeader = 0,           //헤더 패킷 통신 상태
        OnTransferringHeaderComplete,
        OnTransferringContent,  
        OnTransferringContentComplete  
    }

    /// <summary>
    /// 서버 통신전 항상 먼저받는 패킷
    /// 패킷의 크기를 확인하고 통신간 문제없었는지 확인
    /// </summary>
    public class NetworkPacketHeader
    {
        public static readonly ushort HeaderSize = 16;
        private static readonly ulong HeaderCheck = 0x00123456789ABCDEF;

        public int ShouldReceiveBytesSize { get; }
        public long ReceivedTime { get; }


        /// <summary>
        /// 프라이빗 생성자로 외부에선 접근 못하도록함
        /// </summary>
        private NetworkPacketHeader(int shoudReceiveBytesSize) 
        {
            ShouldReceiveBytesSize = shoudReceiveBytesSize;
            ReceivedTime = DateTime.Now.Ticks;
        }


        /// <summary>
        /// 받아야할 바이트를 패킷과 함께
        /// </summary>
        /// <param name="shouldReceiveBytesSize"></param>
        /// <returns></returns>
        public static byte[] MakeHeaderBytes(int shouldReceiveBytesSize)
        {
            if (shouldReceiveBytesSize < 0)
                return null;
            byte[] byteArr = new byte[HeaderSize];
            using (MemoryStream stream = new MemoryStream(byteArr))
            {
                stream.Write(BitConverter.GetBytes(HeaderCheck), 0, 8);             //헤더
                stream.Write(BitConverter.GetBytes(shouldReceiveBytesSize), 0, 4);  //보낼 데이터 양
                stream.Write(BitConverter.GetBytes(0), 0, 4);                       //16바이트 맞추기위한 패딩바이트
                return byteArr;
            }
        }

        public static NetworkPacketHeader MakeHeaderFromBytes(byte[] headerBytes)
        {
            ulong curHeaderCheck = BitConverter.ToUInt64(headerBytes, 0);
            int shouldReceive = BitConverter.ToInt32(headerBytes, 8);
            int padding = BitConverter.ToInt32(headerBytes, 12);

            if (HeaderCheck != curHeaderCheck || shouldReceive < 0 || padding != 0)
                return null;
            return new NetworkPacketHeader(shouldReceive);
        }
    }

    public class NetworkTrafficPacket
    {
        public byte[] TransferingData { get; set; }            //송수신 중인 데이터
        public int Offset { get; set; }                        //오프셋
        public int Size { get; set; }                          //트래픽 크기
        public NetworkTraffic Traffic { get; }                 //소속된 트래픽

        public NetworkTrafficPacket(NetworkTraffic traffic)
        {
            TransferingData = null;
            Offset = 0;
            Size = 0;
            Traffic = traffic;
        }

        //송신시 할당
        public NetworkTrafficPacket(byte[] data, int offset, int size, NetworkTraffic traffic)
        {
            TransferingData = data;
            Offset = offset;
            Size = size;
            Traffic = traffic;
        }

        //수신시 할당
        public NetworkTrafficPacket(int readSize, NetworkTraffic traffic)
        {
            TransferingData = new byte[readSize];
            Offset = 0;
            Size = readSize;
            Traffic = traffic;
        }
    }


    public class NetworkTraffic
    {
        private NetworkTrafficPacket _HeaderPacket;         //헤더 패킷
        private NetworkTrafficPacket _ContentPacket;        //컨텐츠 패킷
        private object _Tag;                                //트래픽의 태그 - 송신자 또는 수신자의 정보를 담으면됨
        private NetworkTrafficStep _Status;                 //트래픽의 상태 - 패킷 유효성 체크 중인지 / 체크가 완료되서 송, 수신 중인지
        private readonly ReaderWriterLockSlim _PacketLock;  //패킷 락

        //수신 트래픽생성
        private NetworkTraffic(int readSize, object tag) 
        {
            _HeaderPacket = new NetworkTrafficPacket(NetworkPacketHeader.HeaderSize, this);
            _ContentPacket = new NetworkTrafficPacket(this);

            _Status = NetworkTrafficStep.OnTransferringHeader;
            _Tag = tag;

            _PacketLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        //송신 트래픽생성
        private NetworkTraffic( byte[] data, int offset, int size, object tag)
        {
            _HeaderPacket = new NetworkTrafficPacket(this);
            _HeaderPacket.TransferingData = NetworkPacketHeader.MakeHeaderBytes(size);
            _HeaderPacket.Offset = 0;
            _HeaderPacket.Size = _HeaderPacket.TransferingData.Length;

            _ContentPacket = new NetworkTrafficPacket(this);
            _ContentPacket.TransferingData = data;
            _ContentPacket.Offset = offset;
            _ContentPacket.Size = size;

            _Status = NetworkTrafficStep.OnTransferringHeader;
            _Tag = tag;

            _PacketLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        }

        #region Getter

        public object Tag
        {
            get
            {
                using (_PacketLock.Read())
                {
                    return _Tag;
                }
            }
        }

        public NetworkTrafficStep Status
        {
            get
            {
                using (_PacketLock.Read())
                {
                    return _Status;
                }
            }
        }

        public NetworkTrafficPacket HeaderPacket
        {
            get
            {
                using (_PacketLock.Read())
                {
                    return _HeaderPacket;
                }
            }
        }

        public NetworkTrafficPacket ContentPacket
        {
            get
            {
                using (_PacketLock.Read())
                {
                    return _ContentPacket;
                }
            }

            set
            {
                using (_PacketLock.Write())
                {
                    _ContentPacket = value;
                }
            }

        }


        #endregion

        public static NetworkTraffic CreateReceiveTraffic(int readSize, object tag)
        {
            return new NetworkTraffic(readSize, tag);
        }

        public static NetworkTraffic CreateSendTraffic(byte[] data, int offset, int size, object tag)
        {
            return new NetworkTraffic(data, offset, size, tag);
        }

        public void SetNextTransferringStep()
        {
            using (_PacketLock.Write())
            {
                _Status++;
            }
        }
    }
}
