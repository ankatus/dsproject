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

            var gameState = new GameState();
            var gameCoordinator = new GameCoordinator(gameState);

            var ui = new UIController(display, gameState, gameCoordinator);

            display.WriteString("Press any key to start game!", 0, 0, ConsoleColor.Green);
            display.Update();
            Console.ReadKey(true);
            display.Clear();

            ui.JoinGame();
        }
    }
}
