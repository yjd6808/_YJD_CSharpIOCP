// ===============================
// @AUTHOR      : 윤정도
// @CREATE DATE : 2020-08-06 오후 1:04:49   
// @PURPOSE     : 데이터 쓰고 지워주는거
// ===============================


using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CSharpSimpleIOCP.Network
{
    public class NetworkDataWriter
    {
        protected byte[] _Data;                     //버퍼
        protected int _WritePosition;               //쓰기 포인터 위치
        protected int _ReadPosition;                //읽기 포인터 위치
        private const int _InitialSize = 16 * 4;    //초기 배열크기
        private readonly bool _AutoResize;          //자동으로 배열 확장할지


        #region Getter
        //모든 데이터를 가져옴
        public byte[] Data
        {
            get 
            { 
                return _Data; 
            }
        }

        public byte[] AvailableData //읽어진 데이터빼고 쓴데이터만 가져옴
        {
            get
            {
                return _Data.Skip(_ReadPosition).Take(_WritePosition - _ReadPosition).ToArray();
            }
        }

        public int Capacity
        {
            get 
            { 
                return _Data.Length; 
            }
        }

        public int WritePosition
        {
            get
            {
                return _WritePosition;
            }
        }

        public int ReadPosition
        {
            get
            {
                return _WritePosition;
            }
        }

        public int ReadAvailableBytesSize
        {
            get { return _Data.Length - _ReadPosition; }
        }

        #endregion

        #region Constructors
        public NetworkDataWriter() : this(true, _InitialSize)
        {
        }

        public NetworkDataWriter(bool autoResize) : this(autoResize, _InitialSize)
        {
        }

        public NetworkDataWriter(bool autoResize, int _InitialSize)
        {
            _Data = new byte[_InitialSize];
            _AutoResize = autoResize;
        }
        #endregion

        #region Static Constructors
        public static NetworkDataWriter FromBytes(byte[] bytes, bool copy)
        {
            if (copy)
            {
                var NetworkDataWriter = new NetworkDataWriter(true, bytes.Length);
                NetworkDataWriter.Write(bytes);
                return NetworkDataWriter;
            }
            return new NetworkDataWriter(true, 0) { _Data = bytes, _WritePosition = bytes.Length };
        }

        public static NetworkDataWriter FromBytes(byte[] bytes, int offset, int length)
        {
            var NetworkDataWriter = new NetworkDataWriter(true, bytes.Length);
            NetworkDataWriter.Write(bytes, offset, length);
            return NetworkDataWriter;
        }
        public static NetworkDataWriter FromString(string value)
        {
            var NetworkDataWriter = new NetworkDataWriter();
            NetworkDataWriter.Write(value);
            return NetworkDataWriter;
        }
        #endregion


        public void ResizeIfNeed(int newSize)
        {
            int len = _Data.Length;
            if (len < newSize)
            {
                while (len < newSize)
                    len *= 2;
                Array.Resize(ref _Data, len);
            }
        }

        public void ResetSize(int size)
        {
            ResizeIfNeed(size);
            _WritePosition = 0;
            _ReadPosition = 0;
        }

        public void ResetWritePosition()
        {
            _WritePosition = 0;
        }

        public void ResetReadPosition()
        {
            _WritePosition = 0;
        }

        public byte[] CopyData()
        {
            byte[] resultData = new byte[_WritePosition];
            Buffer.BlockCopy(_Data, 0, resultData, 0, _WritePosition);
            return resultData;
        }



        public void Write(float value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 4);

            Write(BitConverter.GetBytes(value), _WritePosition, 4);
            _WritePosition += 4;
        }

        public void Write(double value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 8);

            Write(BitConverter.GetBytes(value), _WritePosition, 8);
            _WritePosition += 8;
        }

        public void Write(long value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 8);

            Write(BitConverter.GetBytes(value), _WritePosition, 8);
            _WritePosition += 8;
        }

        public void Write(ulong value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 8);

            Write(BitConverter.GetBytes(value), _WritePosition, 8);
            _WritePosition += 8;
        }

        public void Write(int value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 4);

            Write(BitConverter.GetBytes(value), _WritePosition, 4);
        }

        public void Write(uint value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 4);

            Write(BitConverter.GetBytes(value), _WritePosition, 4);
        }

        public void Write(char value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 2);

            Write(BitConverter.GetBytes(value), _WritePosition, 2);
        }

        public void Write(ushort value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 2);

            Write(BitConverter.GetBytes(value), _WritePosition, 2);
        }

        public void Write(short value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 2);

            Write(BitConverter.GetBytes(value), _WritePosition, 2);
        }

        public void Write(sbyte value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 1);
            _Data[_WritePosition] = (byte)value;
            _WritePosition++;
        }

        public void Write(byte value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 1);
            _Data[_WritePosition] = value;
            _WritePosition++;
        }

        public void Write(byte[] data, int offset, int length)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + length);
            Buffer.BlockCopy(data, offset, _Data, _WritePosition, length);
            _WritePosition += length;
        }

        public void Write(byte[] data)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + data.Length);
            Buffer.BlockCopy(data, 0, _Data, _WritePosition, data.Length);
            _WritePosition += data.Length;
        }


        public void Write(bool value)
        {
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + 1);
            _Data[_WritePosition] = (byte)(value ? 1 : 0);
            _WritePosition++;
        }

        private void WriteArray(Array arr, int sz)
        {
            ushort length = arr == null ? (ushort)0 : (ushort)arr.Length;
            sz *= length;
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + sz + 2);

            Write(BitConverter.GetBytes(length), _WritePosition, 2); //길이를 먼저씀
            if (arr != null)
                Buffer.BlockCopy(arr, 0, _Data, _WritePosition + 2, sz);
            _WritePosition += sz + 2;
        }

        public void WriteArray(float[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(double[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(long[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(ulong[] value)
        {
            WriteArray(value, 8);
        }

        public void WriteArray(int[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(uint[] value)
        {
            WriteArray(value, 4);
        }

        public void WriteArray(ushort[] value)
        {
            WriteArray(value, 2);
        }

        public void WriteArray(short[] value)
        {
            WriteArray(value, 2);
        }

        public void WriteArray(bool[] value)
        {
            WriteArray(value, 1);
        }

        public void WriteArray(string[] value)
        {
            ushort len = value == null ? (ushort)0 : (ushort)value.Length;
            Write(len);
            for (int i = 0; i < len; i++)
                Write(value[i]);
        }

        public void WriteArray(string[] value, int maxLength)
        {
            ushort len = value == null ? (ushort)0 : (ushort)value.Length;
            Write(len);
            for (int i = 0; i < len; i++)
                Write(value[i], maxLength);
        }

        public void Write(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write(0);
                return;
            }

            //바이트 수를 먼저쓰고
            int bytesCount = Encoding.UTF8.GetByteCount(value);
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + bytesCount + 4);
            Write(bytesCount);

            //문자열을 넣어줘야함 그래야 읽을때 얼마나 가져올지 알 수 있으니
            Encoding.UTF8.GetBytes(value, 0, value.Length, _Data, _WritePosition);
            _WritePosition += bytesCount;
        }

        public void Write(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                Write(0);
                return;
            }

            int length = value.Length > maxLength ? maxLength : value.Length;
            //문자열을 적어준 길이만큼 넣어줌
            int bytesCount = Encoding.UTF8.GetByteCount(value);
            if (_AutoResize)
                ResizeIfNeed(_WritePosition + bytesCount + 4);

            Write(bytesCount);
            Encoding.UTF8.GetBytes(value, 0, length, _Data, _WritePosition);

            _WritePosition += bytesCount;
        }

        #region ReadMethods

        public byte ReadByte()
        {
            ThorwExceptionIfNoReadableData(sizeof(bool));
            byte res = _Data[_ReadPosition];
            _ReadPosition += 1;
            return res;
        }

        public bool[] ReadBoolArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(bool) * size);
            var arr = new bool[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size);
            _ReadPosition += size;
            return arr;
        }

        public ushort[] ReadUShortArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(ushort) * size);
            var arr = new ushort[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 2);
            _ReadPosition += size * 2;
            return arr;
        }

        public short[] ReadShortArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(short) * size);
            var arr = new short[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 2);
            _ReadPosition += size * 2;
            return arr;
        }

        public long[] ReadLongArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(long) * size);
            var arr = new long[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 8);
            _ReadPosition += size * 8;
            return arr;
        }

        public ulong[] ReadULongArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(ulong) * size);
            var arr = new ulong[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 8);
            _ReadPosition += size * 8;
            return arr;
        }

        public int[] ReadIntArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(int) * size);
            var arr = new int[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 4);
            _ReadPosition += size * 4;
            return arr;
        }

        public uint[] ReadUIntArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(uint) * size);
            var arr = new uint[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 4);
            _ReadPosition += size * 4;
            return arr;
        }

        public float[] ReadFloatArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(float) * size);
            var arr = new float[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 4);
            _ReadPosition += size * 4;
            return arr;
        }

        public double[] ReadDoubleArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(double) * size);
            var arr = new double[size];
            Buffer.BlockCopy(_Data, _ReadPosition, arr, 0, size * 8);
            _ReadPosition += size * 8;
            return arr;
        }

        public string[] ReadStringArray()
        {
            ushort size = ReadUShort();
            ThorwExceptionIfNoReadableData(sizeof(double) * size);
            var arr = new string[size];
            for (int i = 0; i < size; i++)
            {
                arr[i] = ReadString();
            }
            return arr;
        }

        public string[] ReadStringArray(int maxStringLength)
        {
            ushort size = ReadUShort();
            var arr = new string[size];
            for (int i = 0; i < size; i++)
            {
                arr[i] = ReadString(maxStringLength);
            }
            return arr;
        }

        public bool ReadBool()
        {
            ThorwExceptionIfNoReadableData(sizeof(bool));
            bool res = _Data[_ReadPosition] > 0;
            _ReadPosition += 1;
            return res;
        }

        public char ReadChar()
        {
            ThorwExceptionIfNoReadableData(sizeof(char));
            char result = BitConverter.ToChar(_Data, _ReadPosition);
            _ReadPosition += 2;
            return result;
        }

        public ushort ReadUShort()
        {
            ThorwExceptionIfNoReadableData(sizeof(ushort));
            ushort result = BitConverter.ToUInt16(_Data, _ReadPosition);
            _ReadPosition += 2;
            return result;
        }

        public short ReadShort()
        {
            ThorwExceptionIfNoReadableData(sizeof(short));
            short result = BitConverter.ToInt16(_Data, _ReadPosition);
            _ReadPosition += 2;
            return result;
        }

        public long ReadLong()
        {
            ThorwExceptionIfNoReadableData(sizeof(long));
            long result = BitConverter.ToInt64(_Data, _ReadPosition);
            _ReadPosition += 8;
            return result;
        }

        public ulong ReadULong()
        {
            ThorwExceptionIfNoReadableData(sizeof(ulong));
            ulong result = BitConverter.ToUInt64(_Data, _ReadPosition);
            _ReadPosition += 8;
            return result;
        }

        public int ReadInt()
        {
            ThorwExceptionIfNoReadableData(sizeof(int));
            int result = BitConverter.ToInt32(_Data, _ReadPosition);
            _ReadPosition += 4;
            return result;
        }

        public uint ReadUInt()
        {
            ThorwExceptionIfNoReadableData(sizeof(uint));
            uint result = BitConverter.ToUInt32(_Data, _ReadPosition);
            _ReadPosition += 4;
            return result;
        }

        public float ReadFloat()
        {
            ThorwExceptionIfNoReadableData(sizeof(float));
            float result = BitConverter.ToSingle(_Data, _ReadPosition);
            _ReadPosition += 4;
            return result;
        }

        public double ReadDouble()
        {
            ThorwExceptionIfNoReadableData(sizeof(double));
            double result = BitConverter.ToDouble(_Data, _ReadPosition);
            _ReadPosition += 8;
            return result;
        }

        public string ReadString(int maxLength)
        {
            int bytesCount = ReadInt();
            if (bytesCount <= 0 || bytesCount > maxLength * 2)
            {
                return string.Empty;
            }

            int charCount = Encoding.UTF8.GetCharCount(_Data, _ReadPosition, bytesCount);
            if (charCount > maxLength)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_Data, _ReadPosition, bytesCount);
            _ReadPosition += bytesCount;
            return result;
        }

        public string ReadString()
        {
            int bytesCount = ReadInt();
            if (bytesCount <= 0)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_Data, _ReadPosition, bytesCount);
            _ReadPosition += bytesCount;
            return result;
        }

        public ArraySegment<byte> ReadRemainingBytesSegment()
        {
            ArraySegment<byte> segment = new ArraySegment<byte>(_Data, _ReadPosition, ReadAvailableBytesSize);
            _ReadPosition = _Data.Length;
            return segment;
        }

        public byte[] ReadRemainingBytes()
        {
            byte[] outgoingData = new byte[ReadAvailableBytesSize];
            Buffer.BlockCopy(_Data, _ReadPosition, outgoingData, 0, ReadAvailableBytesSize);
            _ReadPosition = _Data.Length;
            return outgoingData;
        }

        public void ReadBytes(byte[] destination, int start, int count)
        {
            Buffer.BlockCopy(_Data, _ReadPosition, destination, start, count);
            _ReadPosition += count;
        }

        public void ReadBytes(byte[] destination, int count)
        {
            Buffer.BlockCopy(_Data, _ReadPosition, destination, 0, count);
            _ReadPosition += count;
        }

        public byte[] ReadBytesWithLength()
        {
            int length = ReadInt();
            byte[] outgoingData = new byte[length];
            Buffer.BlockCopy(_Data, _ReadPosition, outgoingData, 0, length);
            _ReadPosition += length;
            return outgoingData;
        }
        #endregion

        #region PeekMethods

        public byte PeekByte()
        {
            return _Data[_ReadPosition];
        }

        public bool PeekBool()
        {
            return _Data[_ReadPosition] > 0;
        }

        public char PeekChar()
        {
            return BitConverter.ToChar(_Data, _ReadPosition);
        }

        public ushort PeekUShort()
        {
            return BitConverter.ToUInt16(_Data, _ReadPosition);
        }

        public short PeekShort()
        {
            return BitConverter.ToInt16(_Data, _ReadPosition);
        }

        public long PeekLong()
        {
            return BitConverter.ToInt64(_Data, _ReadPosition);
        }

        public ulong PeekULong()
        {
            return BitConverter.ToUInt64(_Data, _ReadPosition);
        }

        public int PeekInt()
        {
            return BitConverter.ToInt32(_Data, _ReadPosition);
        }

        public uint PeekUInt()
        {
            return BitConverter.ToUInt32(_Data, _ReadPosition);
        }

        public float PeekFloat()
        {
            return BitConverter.ToSingle(_Data, _ReadPosition);
        }

        public double PeekDouble()
        {
            return BitConverter.ToDouble(_Data, _ReadPosition);
        }

        public string PeekString(int maxLength)
        {
            int bytesCount = BitConverter.ToInt32(_Data, _ReadPosition);
            if (bytesCount <= 0 || bytesCount > maxLength * 2)
            {
                return string.Empty;
            }

            int charCount = Encoding.UTF8.GetCharCount(_Data, _ReadPosition + 4, bytesCount);
            if (charCount > maxLength)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_Data, _ReadPosition + 4, bytesCount);
            return result;
        }

        public string PeekString()
        {
            int bytesCount = BitConverter.ToInt32(_Data, _ReadPosition);
            if (bytesCount <= 0)
            {
                return string.Empty;
            }

            string result = Encoding.UTF8.GetString(_Data, _ReadPosition + 4, bytesCount);
            return result;
        }

        public byte[] PeekRemainingBytes()
        {
            byte[] outgoingData = new byte[ReadAvailableBytesSize];
            Buffer.BlockCopy(_Data, _ReadPosition, outgoingData, 0, ReadAvailableBytesSize);
            return outgoingData;
        }
        #endregion

        #region TryReadMethods
        public bool TryReadByte(out byte result)
        {
            if (ReadAvailableBytesSize >= 1)
            {
                result = ReadByte();
                return true;
            }
            result = 0;
            return false;
        }


        public bool TryReadBool(out bool result)
        {
            if (ReadAvailableBytesSize >= 1)
            {
                result = ReadBool();
                return true;
            }
            result = false;
            return false;
        }

        public bool TryReadChar(out char result)
        {
            if (ReadAvailableBytesSize >= 2)
            {
                result = ReadChar();
                return true;
            }
            result = '\0';
            return false;
        }

        public bool TryReadShort(out short result)
        {
            if (ReadAvailableBytesSize >= 2)
            {
                result = ReadShort();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadUShort(out ushort result)
        {
            if (ReadAvailableBytesSize >= 2)
            {
                result = ReadUShort();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadInt(out int result)
        {
            if (ReadAvailableBytesSize >= 4)
            {
                result = ReadInt();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadUInt(out uint result)
        {
            if (ReadAvailableBytesSize >= 4)
            {
                result = ReadUInt();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadLong(out long result)
        {
            if (ReadAvailableBytesSize >= 8)
            {
                result = ReadLong();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadULong(out ulong result)
        {
            if (ReadAvailableBytesSize >= 8)
            {
                result = ReadULong();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadFloat(out float result)
        {
            if (ReadAvailableBytesSize >= 4)
            {
                result = ReadFloat();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadDouble(out double result)
        {
            if (ReadAvailableBytesSize >= 8)
            {
                result = ReadDouble();
                return true;
            }
            result = 0;
            return false;
        }

        public bool TryReadString(out string result)
        {
            if (ReadAvailableBytesSize >= 4)
            {
                var bytesCount = PeekInt();
                if (ReadAvailableBytesSize >= bytesCount + 4)
                {
                    result = ReadString();
                    return true;
                }
            }
            result = null;
            return false;
        }

        public bool TryReadStringArray(out string[] result)
        {
            ushort size;
            if (!TryReadUShort(out size))
            {
                result = null;
                return false;
            }

            result = new string[size];
            for (int i = 0; i < size; i++)
            {
                if (!TryReadString(out result[i]))
                {
                    result = null;
                    return false;
                }
            }

            return true;
        }

        public bool TryReadBytesWithLength(out byte[] result)
        {
            if (ReadAvailableBytesSize >= 4)
            {
                var length = PeekInt();
                if (length >= 0 && ReadAvailableBytesSize >= length + 4)
                {
                    result = ReadBytesWithLength();
                    return true;
                }
            }
            result = null;
            return false;
        }
        #endregion

        public void Clear()
        {
            _WritePosition = 0;
            _ReadPosition = 0;
            _Data = null;
        }

        //더 이상 읽을게 없는데 읽을려고하면 오류를 뱉어내자!
        private void ThorwExceptionIfNoReadableData(int size)
        {
            if (_ReadPosition + size >= _WritePosition)
                throw new Exception("읽을 수 있는 데이터가 없다!!!");
        }
    }
}
