using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal enum MessageType
    {
        JoinGame = 1,
        TurnInfo = 2,
        Response = 3
    }

    internal class Message
    {
        public MessageType MsgType { get; set; }
        public int Sender { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    internal class JoinGameMessage : Message
    {
        public string Name { get; set; }
        public int LobbySize { get; set; }

        public JoinGameMessage()
        {
            MsgType = MessageType.JoinGame;
        }    
    }

    internal class TurnInfoMessage : Message
    {
        public TurnInfo TurnInfo { get; set; }

        public TurnInfoMessage()
        {
            MsgType = MessageType.TurnInfo;
        }
    }

    internal class ResponseMessage : Message
    {
        public int Receiver { get; set; }
        public bool Approve { get; set; }
        public string ErrorString { get; set; }

        public ResponseMessage()
        {
            MsgType = MessageType.Response;
        }
    }
}