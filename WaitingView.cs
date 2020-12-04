using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal class WaitingView
    {

        private readonly Display _display;

        public UnoCard TopCard { get; set; }
        public string Message { get; set; }
        public ConsoleColor MessageColor { get; set; }

        public WaitingView(Display display)
        {
            _display = display;
            MessageColor = ConsoleColor.White;
        }

        public void Draw()
        {
            // Draw top card
            if (TopCard is not null) _display.InsertArray(TopCard.GetGraphic(), 0, 60, Utils.CardToConsoleColor(TopCard.Color));

            // Draw message
            _display.WriteString(Message, 30, 0, MessageColor);
        }
    }
}
