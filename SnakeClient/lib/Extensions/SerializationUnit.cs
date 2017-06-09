using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace lib.Extensions
{
    public class SerializationUnit
    {
        public static byte[] SerializeObject(lib.Net.Sockets.ITransferableObject obj)
        {
            if (obj == null)
                return null;
            byte[] namebuff = Encoding.UTF8.GetBytes(obj.GetType().FullName);
            byte[] buff = BitConverter.GetBytes((int)namebuff.Length)
                          .Concat(namebuff)
                          .Concat(obj.GetData()).ToArray();
            return buff;
        }
        public static lib.Net.Sockets.ITransferableObject DeserializeObject(byte[] bytes)
        {
            lib.Net.Sockets.ITransferableObject obj = null;
            if (bytes == null)
                return null;
            int len = BitConverter.ToInt32(bytes, 0);
            string fullName = Encoding.UTF8.GetString(bytes, 4, len);
            Type type = Type.GetType(fullName);
            obj = Activator.CreateInstance(type) as lib.Net.Sockets.ITransferableObject;
            if (obj != null)
                obj.ReadData(bytes, 4 + len, bytes.Length - 4 - len);
            return obj;
        }
    }
}
