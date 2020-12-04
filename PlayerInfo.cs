using System.Collections.Generic;

namespace dsproject
{
    internal class PlayerInfo
    {
        public int PlayerID { get; set; }
        public string PlayerName { get; set; }
        public bool Dealer { get; set; }
        public List<UnoCard> Hand { get; set; }

        public PlayerInfo()
        {
            Hand = new List<UnoCard>();
        }

        public override string ToString()
        {
            return "Name: " + PlayerName + ", ID: " + PlayerID;
        }
    }
}