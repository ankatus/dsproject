using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal class CardsView
    {
        private const int VISIBLE_CARDS = 5;
        private const int HAND_OFFSET_LEFT = 30;

        private readonly Display _display;

        public List<UnoCard> Hand { get; set; }
        public UnoCard TopCard { get; set; }
        public int VisibleIndex { get; private set; }


        public CardsView(Display display)
        {
            _display = display;
            Hand = new List<UnoCard>();
        }

        public void IncreaseVisibleIndex()
        {
            var max = Hand.Count / 5 - (Hand.Count % 5 == 0 ? 1 : 0);
            if (VisibleIndex == max) return;

            VisibleIndex++;
        }

        public void DecreaseVisibleIndex()
        {
            if (VisibleIndex == 0) return;

            VisibleIndex--;
        }

        public void Draw()
        {
            // Draw top card
            if (TopCard is not null) _display.InsertArray(TopCard.GetGraphic(), 0, 60, Utils.CardToConsoleColor(TopCard.Color));

            // Draw visible hand
            var cardsAfterIndex = Hand.Count - (VISIBLE_CARDS * VisibleIndex); 
            for (var i = 0; i < (cardsAfterIndex < VISIBLE_CARDS ? cardsAfterIndex : VISIBLE_CARDS); i++)
            {
                var card = Hand[i + VisibleIndex * VISIBLE_CARDS];

                // Draw card
                _display.InsertArray(card.GetGraphic(), 30, HAND_OFFSET_LEFT + i * CardGraphics.CARDGRAPHIC_WIDTH + i, Utils.CardToConsoleColor(card.Color));

                // Draw selection number
                _display.WriteString("" + (i + 1), 30, HAND_OFFSET_LEFT + i * CardGraphics.CARDGRAPHIC_WIDTH + i + 5);
            }
        }
    }
}
