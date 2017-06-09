using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace lib.Net.Sockets
{
    public enum MessageType
    {
        Unknow, Data, String, Object
    }
    public interface ITransferableObject
    {
        byte[] GetData();
        void ReadData(byte[] data, int index, int len);
        void ReadData(byte[] data);
    }
    public class SocketDataMessage
    {
        public uint Length { get; internal set; }
        public byte[] Data { get; internal set; }
    }
    public class SocketStringMessage
    {
        public string String { get; internal set; }

    }
    public class SocketObjectMessage
    {
        public ITransferableObject Object { get; internal set; }
    }

    public class SocketMessage
    {
        private MessageType msgtype = MessageType.Unknow;
        private SocketDataMessage dataMessage = null;
        private SocketStringMessage strMessage = null;
        private SocketObjectMessage objMessage = null;

        public MessageType MsgType
        {
            get
            {
                return msgtype;
            }
            private set
            {
                msgtype = value;
            }
        }
        public SocketDataMessage DataMessage
        {
            get
            {
                return dataMessage;
            }
        }
        public SocketStringMessage StringMessage
        {
            get
            {
                return strMessage;
            }
        }
        public SocketObjectMessage ObjectMessage
        {
            get
            {
                return objMessage;
            }
        }

        internal SocketMessage()
        {
            MsgType = MessageType.Unknow;
        }

        public static List<SocketMessage> Resolve(byte[] buffer, int index, int endIndex, out bool moreDataNeeded, out int lastOffset)
        {
            List<SocketMessage> list = new List<SocketMessage>();
            int offset = index;
            while (offset < endIndex)
            {
                SocketMessage msg = new SocketMessage();
                msg.MsgType = (MessageType)buffer[offset];
                offset++;
                
                if (endIndex - offset + 1 < 4)
                {
                    offset -= 1;
                    break;
                }
                UInt32 size = BitConverter.ToUInt32(buffer, offset);
                offset += 4;
                if (endIndex - offset + 1 < size)
                {
                    offset -= 5;
                    break;
                }
                switch (msg.MsgType)
                {
                    case MessageType.Data:
                        msg.dataMessage = new SocketDataMessage();
                        msg.dataMessage.Length = size;
                        msg.dataMessage.Data = new byte[size];
                        Array.Copy(buffer, offset, msg.dataMessage.Data, 0, size);
                        break;
                    case MessageType.String:
                        msg.strMessage = new SocketStringMessage();
                        msg.strMessage.String = Encoding.UTF8.GetString(buffer, offset, (int)size);
                        break;
                    case MessageType.Object:
                        msg.objMessage = new SocketObjectMessage();
                        byte[] tmpbuf = new byte[size];
                        Array.Copy(buffer, offset, tmpbuf, 0, size);
                        msg.objMessage.Object = lib.Extensions.SerializationUnit.DeserializeObject(tmpbuf);
                        break;
                    default:
                        throw new Exception("未知类型");
                        break;
                }
                offset += (int)size;
                list.Add(msg);
            }
            if (offset > endIndex + 1)
                throw new Exception("可能出现无效消息");
            if (offset == endIndex + 1)
            {
                moreDataNeeded = false;
            }
            else
            {
                moreDataNeeded = true;
            }
            lastOffset = offset;
            return list;
        }

        public static SocketMessage GetDataMessage(byte[] data)
        {
            return new SocketMessage()
            {
                MsgType = MessageType.Data,
                dataMessage = new SocketDataMessage()
                {
                    Data = data,
                    Length = (uint)data.Length
                }
            };
        }
        public static SocketMessage GetStringMessage(string str)
        {
            return new SocketMessage()
            {
                MsgType = MessageType.String,
                strMessage = new SocketStringMessage()
                {
                    String = str
                }
            };
        }
        public static SocketMessage GetObjectMessage(ITransferableObject obj)
        {
            return new SocketMessage()
            {
                MsgType = MessageType.Object,
                objMessage = new SocketObjectMessage()
                {
                    Object = obj
                }
            };
        }

        internal byte[] GetBuffer()
        {
            byte[] type = new byte[1] { (byte)MsgType };
            byte[] sizebyte;
            switch (MsgType)
            {
                case MessageType.Data:
                    sizebyte = BitConverter.GetBytes((UInt32)DataMessage.Length);
                    return type.Concat(sizebyte).Concat(DataMessage.Data).ToArray();
                    break;
                case MessageType.Object:
                    byte[] data = lib.Extensions.SerializationUnit.SerializeObject(ObjectMessage.Object);
                    return type.Concat(BitConverter.GetBytes((UInt32)data.Length)).Concat(data).ToArray();
                    break;
                case MessageType.String:
                    byte[] strbytes = Encoding.UTF8.GetBytes(StringMessage.String);
                    sizebyte = BitConverter.GetBytes((UInt32)strbytes.Length);
                    return type.Concat(sizebyte).Concat(strbytes).ToArray();
                    break;
            }
            return null;
        }
    }

    public class SocketMessageEventArgs
    {
        private List<SocketMessage> messages = null;
        private byte[] rawData = null;
        private int rawDataLength = 0;

        public List<SocketMessage> Messages
        {
            get
            {
                return messages;
            }

            internal set
            {
                messages = value;
            }
        }

        public byte[] RawData
        {
            get
            {
                return rawData;
            }
            private set
            {
                rawData = value;
            }
        }

        public int RawDataLength
        {
            get
            {
                return rawDataLength;
            }
            internal set
            {
                rawDataLength = value;
            }
        }

        public SocketMessageEventArgs(byte[] rawData, int dataLen)
        {
            this.rawData = new byte[rawData.Length];
            Array.Copy(rawData, this.rawData, rawData.Length);
            rawDataLength = dataLen;
        }
    }
}
