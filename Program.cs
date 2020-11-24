using System;
using System.Threading;
using System.Windows.Input;

namespace dsproject
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.CursorVisible = false;

            var display = new Display();

            display.WriteString("Press any key to start demo!", 49, 0);

            display.Update();

            Console.ReadKey(true);

            display.Clear();

            var view = new CardsView(display);
            var random = new Random();

            var cardNumber = random.Next(0, 10);
            var cardColor = (CardColor)random.Next(0, 4);

            view.TopCard = new UnoCard(CardType.Number, cardColor, cardNumber);

            for (var i = 0; i < 25; i++)
            {
                cardNumber = random.Next(0, 10);
                cardColor = (CardColor)random.Next(0, 4);
                view.Hand.Add(new UnoCard(CardType.Number, cardColor, cardNumber));
            }

            view.Draw();
            display.Update();

            while (true)
            {
                var keyPress = Console.ReadKey(true);

                if (keyPress.Key == ConsoleKey.Escape) break;
                switch (keyPress.Key)
                {
                    case ConsoleKey.RightArrow:
                        view.IncreaseVisibleIndex();
                        break;
                    case ConsoleKey.LeftArrow:
                        view.DecreaseVisibleIndex();
                        break;
                    default:
                        view.TopCard = keyPress.Key switch
                        {
                            ConsoleKey.D1 => view.Hand[0 + view.VisibleIndex * 5],
                            ConsoleKey.D2 => view.Hand[1 + view.VisibleIndex * 5],
                            ConsoleKey.D3 => view.Hand[2 + view.VisibleIndex * 5],
                            ConsoleKey.D4 => view.Hand[3 + view.VisibleIndex * 5],
                            ConsoleKey.D5 => view.Hand[4 + view.VisibleIndex * 5],
                            _ => view.TopCard
                        };
                        break;
                }

                view.Draw();
                display.Update();
                Thread.Sleep(10);
            }

            display.Update();

            Console.ReadKey(true);



            //NetworkCommunication nc = new NetworkCommunication();

            //Thread waitForPlayersThread = new Thread(nc.WaitForPlayersToJoin);
            //waitForPlayersThread.Start();
        }
    }
}
