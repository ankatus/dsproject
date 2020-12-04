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

            GameState gameState = new GameState();
            GameCoordinator gameCoordinator = new GameCoordinator(gameState);

            UIController ui = new UIController(display, gameState, gameCoordinator);

            display.WriteString("Press any key to start game!", 49, 0);
            display.Update();
            Console.ReadKey(true);
            display.Clear();

            ui.JoinGame();

        }
    }
}
