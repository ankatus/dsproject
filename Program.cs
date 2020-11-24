﻿using System;
using System.Threading;

namespace dsproject
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Console.CursorVisible = false;

            var display = new Display();

            display.WriteString("Press any key to continue!", 49, 0);
            
            display.Update();
            
            Console.ReadKey(true);
            
            display.Clear();

            // Test drawing of some cards
            var oneCard = new[]
            {
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', '/', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '/', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
            };
            
            var twoCard = new[]
            {
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
            };
            
            var threeCard = new[]
            {
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', '|', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', '-', '-', '-', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', '|' },
                new[] { '|', '-', '-', '-', '-', '-', '-', '-', '-', '-', '-', '|' },
            };

            display.InsertArray(oneCard, 10, 5, ConsoleColor.Red);
            display.InsertArray(twoCard, 10, 18, ConsoleColor.Green);
            display.InsertArray(threeCard, 10, 31, ConsoleColor.Blue);

            display.Update();

            Console.ReadKey(true);



            //NetworkCommunication nc = new NetworkCommunication();

            //Thread waitForPlayersThread = new Thread(nc.WaitForPlayersToJoin);
            //waitForPlayersThread.Start();
        }
    }
}
