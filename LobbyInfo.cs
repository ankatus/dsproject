    using System.Collections.Generic;

namespace dsproject
{
    internal class LobbyInfo
    {
        public LobbyInfo()
        {
            Players = new List<PlayerInfo>();
        }

        public List<PlayerInfo> Players { get; set; }
    }
}