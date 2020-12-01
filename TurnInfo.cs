using System.Collections.Generic;

namespace dsproject
{
    internal class TurnInfo : TurnHandoff
    {
        public int TurnNumber { get; set; }
        public UnoCard PlayedCard { get; set; }
        public List<UnoCard> DrawnCards { get; set; }

        public TurnInfo()
        {
            DrawnCards = new List<UnoCard>();
        }

        public TurnInfo(TurnInfo source)
        {
            TurnNumber = source.TurnNumber;
            PlayedCard = new UnoCard(source.PlayedCard);
            DrawnCards = new List<UnoCard>();
            foreach (var card in source.DrawnCards)
            {
                DrawnCards.Add(new UnoCard(card));
            }
        }
    }
}