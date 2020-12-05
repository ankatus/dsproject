using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace dsproject
{
    internal class Display
    {
        private readonly char[][] _characters;
        private readonly ConsoleColor[][] _colors;
        
        public const int DisplayWidth = 150;
        public const int DisplayHeight = 50;

        public Display()
        {
            // Init characters
            _characters = new char[DisplayHeight][];
            for (var rowIndex = 0; rowIndex < DisplayHeight; rowIndex++)
            {
                _characters[rowIndex] = new char[DisplayWidth];
            }

            // Init colors
            _colors = new ConsoleColor[DisplayHeight][];
            for (var rowIndex = 0; rowIndex < DisplayHeight; rowIndex++)
            {
                _colors[rowIndex] = new ConsoleColor[DisplayWidth];

                // Init to white
                for (var i = 0; i < _colors[rowIndex].Length; i++)
                {
                    _colors[rowIndex][i] = ConsoleColor.White;
                }
            }

            // Check OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.SetWindowSize(DisplayWidth + 1, DisplayHeight);
                Console.SetBufferSize(DisplayWidth + 1, DisplayHeight);
            }
            else
            {
                throw new NotImplementedException("Display not implemented for non-windows OSs");
            }
        }

        public void Clear()
        {
            foreach (var row in _characters)
            {
                Array.Clear(row, 0, row.Length);
            }
        }

        public void InsertArray(char[][] content, int row, int col, ConsoleColor color)
        {
            if (row is < 0 or >= DisplayHeight) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DisplayWidth) throw new ArgumentOutOfRangeException(nameof(col));

            // Copy characters
            for (var rowIndex = 0; rowIndex < content.Length; rowIndex++)
            {
                Array.Copy(content[rowIndex], 0, _characters[row + rowIndex], col, content[rowIndex].Length);
            }

            for (var y = 0; y < content.Length; y++)
            {
                for (var x = 0; x < content[y].Length; x++)
                {
                    _colors[row + y][col + x] = color;
                }
            }
        }

        public void InsertArray(char[][] content, int row, int col)
        {
            InsertArray(content, row, col, ConsoleColor.White);
        }

        public void InsertArray(char[] content, int row, int col, ConsoleColor color)
        {
            InsertArray(new[] { content }, row, col, color);
        }

        public void InsertArray(char[] content, int row, int col)
        {
            InsertArray(content, row, col, ConsoleColor.White);
        }


        public void WriteString(string content, int row, int col, ConsoleColor color)
        {
            if (row is < 0 or >= DisplayHeight) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DisplayWidth) throw new ArgumentOutOfRangeException(nameof(col));

            // Check if string fits on this row starting from "col"
            if (content.Length <= DisplayWidth - col)
            {
                InsertArray(content.ToCharArray(), row, col, color);
            }
            else
            {
                // If not, take a substring that fits and try to write the rest on the next row
                var thisRowContent = content.Substring(0, DisplayWidth - col);

                InsertArray(thisRowContent.ToCharArray(), row, col, color);

                var nextRowContent = content.Substring(DisplayWidth - col);
                WriteString(nextRowContent, row + 1, 0, color);
            }
        }

        public void WriteString(string content, int row, int col)
        {
            WriteString(content, row, col, ConsoleColor.White);
        }

        public void Update()
        {
            // To avoid drawing one character at a time (slow), split _characters into chunks of continuous color and draw those
            var rows = new List<List<(ConsoleColor color, List<char> content)>>();

            for (var rowIndex = 0; rowIndex < DisplayHeight; rowIndex++)
            {
                var currentChunks = new List<(ConsoleColor color, List<char> content)>();
                var rowChars = _characters[rowIndex];
                var rowColors = _colors[rowIndex];
                var currentColor = _colors[rowIndex][0];
                var currentContent = new List<char>();

                for (var colIndex = 0; colIndex < DisplayWidth; colIndex++)
                {
                    if (rowColors[colIndex] != currentColor)
                    {
                        // Start a new chunk
                        currentChunks.Add((currentColor, currentContent));
                        currentContent = new List<char>();
                        currentColor = rowColors[colIndex];
                    }

                    currentContent.Add(rowChars[colIndex]);
                }

                currentChunks.Add((currentColor, currentContent));
                rows.Add(currentChunks);
            }

            Debug.Assert(rows.Count == DisplayHeight);

            for (var rowIndex = 0; rowIndex < DisplayHeight; rowIndex++)
            {
                var chunks = rows[rowIndex];
                Console.SetCursorPosition(0, rowIndex);

                foreach (var (color, content) in chunks)
                {
                    Console.ForegroundColor = color;
                    Console.Write(content.ToArray());
                }
            }
        }
    }
}
