using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal abstract class Message
    {
        public UInt32 MsgType;
        public UInt32 Sender;
        
        public abstract byte[] GetBytes();
        public abstract void ReadBytes(byte[] bytes);

        public static Message CreateMessageFromBytes(byte[] bytes)
        {
            UInt32 msgType = BitConverter.ToUInt32(bytes, 0);

            switch (msgType)
            {
                case 1:
                    CreateGameMessage msg = new CreateGameMessage();
                    msg.ReadBytes(bytes);
                    return msg;

                default:
                    return null;
            }
        }
    }

    // Message used when waiting for players who want to play
    internal class CreateGameMessage : Message
    {
        public CreateGameMessage(UInt32 sender)
        {
            MsgType = 1;
            Sender = sender;
        }

        public CreateGameMessage()
        {
            MsgType = 1;
            Sender = 0;
        }

        public override byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(MsgType));
            bytes.AddRange(BitConverter.GetBytes(Sender));

            return bytes.ToArray();
        }

        public override void ReadBytes(byte[] bytes)
        {
            int index = 0;

            MsgType = BitConverter.ToUInt32(bytes, index);
            index += 4;
            Sender = BitConverter.ToUInt32(bytes, index);
        }
    }
}
