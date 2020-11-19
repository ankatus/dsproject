﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace dsproject
{
    internal static class Utils
    {
        internal static ConsoleColor GetRandomColor()
        {
            var possibleColors = new[] { ConsoleColor.White, ConsoleColor.Red, ConsoleColor.Green, ConsoleColor.Blue };
            var randomGen = new Random();

            return possibleColors[randomGen.Next(0, possibleColors.Length)];
        }
    }
}