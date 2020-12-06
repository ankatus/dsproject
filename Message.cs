using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal enum MessageType
    {
        TurnInfo = 1,
        Response = 2,
        AdvertiseLobby = 3,
        JoinLobby = 4
    }

    internal class Message
    {
        public MessageType MsgType { get; set; }
        public int Sender { get; set; }
        public string MsgID { get; set; }
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
        public string ReceivedMsgID { get; set; }
        public bool Approve { get; set; }
        public string ErrorString { get; set; }

        public ResponseMessage()
        {
            MsgType = MessageType.Response;
        }
    }

    internal class AdvertiseLobbyMessage : Message
    {
        public int LobbySize { get; set; }
        public LobbyInfo LobbyInfo { get; set; }

        public AdvertiseLobbyMessage()
        {
            MsgType = MessageType.AdvertiseLobby;
        }
    }

    internal class JoinLobbyMessage : Message
    {
        public string Name { get; set; }
        public int Receiver { get; set; }

        public JoinLobbyMessage()
        {
            MsgType = MessageType.JoinLobby;
        }
    }
}