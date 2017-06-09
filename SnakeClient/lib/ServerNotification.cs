using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace lib
{
    [Serializable]
    public class ServerNotification : lib.Net.Sockets.ITransferableObject
    {
        public int Index;
        public string ElseMessage;
        public string PlayerMessage;

        public byte[] GetData()
        {
            var bytes = BitConverter.GetBytes(Index);
            var elsemsgbytes = Encoding.UTF8.GetBytes(ElseMessage);
            var playermsgbytes = Encoding.UTF8.GetBytes(PlayerMessage);
            return bytes.Concat(BitConverter.GetBytes(elsemsgbytes.Count())).Concat(elsemsgbytes).Concat(BitConverter.GetBytes(playermsgbytes.Count())).Concat(playermsgbytes).ToArray();
        }

        public void ReadData(byte[] data, int index, int len)
        {
            int offset = index;
            Index = BitConverter.ToInt32(data, offset);
            offset += 4;
            int elsemsglen = BitConverter.ToInt32(data, offset);
            offset += 4;
            ElseMessage = Encoding.UTF8.GetString(data, offset, elsemsglen);
            offset += elsemsglen;
            int playermsglen = BitConverter.ToInt32(data, offset);
            offset += 4;
            PlayerMessage = Encoding.UTF8.GetString(data, offset, playermsglen);
            offset += elsemsglen;
        }

        public void ReadData(byte[] data)
        {
            ReadData(data, 0, data.Length);
        }
    }
}
