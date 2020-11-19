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
        private const int DISPLAY_WIDTH = 150;
        private const int DISPLAY_HEIGHT = 50;

        private readonly char[][] _characters;
        private readonly ConsoleColor[][] _colors;

        public Display()
        {
            // Init characters
            _characters = new char[DISPLAY_HEIGHT][];
            for (var rowIndex = 0; rowIndex < DISPLAY_HEIGHT; rowIndex++)
            {
                _characters[rowIndex] = new char[DISPLAY_WIDTH];
            }

            // Init colors
            _colors = new ConsoleColor[DISPLAY_HEIGHT][];
            for (var rowIndex = 0; rowIndex < DISPLAY_HEIGHT; rowIndex++)
            {
                _colors[rowIndex] = new ConsoleColor[DISPLAY_WIDTH];

                // Init to white
                for (var i = 0; i < _colors[rowIndex].Length; i++)
                {
                    _colors[rowIndex][i] = ConsoleColor.White;
                }
            }

            // Check OS
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // If windows, we can check that console buffer and window size are big enough

                if (Console.WindowWidth < DISPLAY_WIDTH || Console.WindowHeight < DISPLAY_HEIGHT)
                {
                    var newWidth = Console.WindowWidth < DISPLAY_WIDTH ? DISPLAY_WIDTH : Console.WindowWidth;
                    var newHeight = Console.WindowHeight < DISPLAY_HEIGHT ? DISPLAY_HEIGHT : Console.WindowHeight;

                    Console.SetWindowSize(newWidth, newHeight);
                }

                if (Console.BufferWidth < DISPLAY_WIDTH || Console.BufferHeight < DISPLAY_HEIGHT)
                {
                    var newWidth = Console.BufferWidth < DISPLAY_WIDTH ? DISPLAY_WIDTH : Console.BufferWidth;
                    var newHeight = Console.BufferHeight < DISPLAY_HEIGHT ? DISPLAY_HEIGHT : Console.BufferHeight;

                    Console.SetBufferSize(newWidth, newHeight);
                }

                WriteString("Display Initialized!", 0, 0);
                WriteString(("Window: W: " + Console.WindowWidth + " h: " + Console.WindowHeight), 1, 0);
                WriteString(("Buffer: W: " + Console.BufferWidth + " h: " + Console.BufferHeight), 2, 0);
                
                Update();
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
            if (row is < 0 or >= DISPLAY_HEIGHT) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DISPLAY_WIDTH) throw new ArgumentOutOfRangeException(nameof(col));
            
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
            InsertArray(new[] {content}, row, col,color);
        }

        public void InsertArray(char[] content, int row, int col)
        {
            InsertArray(content, row, col, ConsoleColor.White);
        }
        

        public void WriteString(string content, int row, int col, ConsoleColor color)
        {
            if (row is < 0 or >= DISPLAY_HEIGHT) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DISPLAY_WIDTH) throw new ArgumentOutOfRangeException(nameof(col));

            // Check if string fits on this row starting from "col"
            if (content.Length <= DISPLAY_WIDTH - col)
            {
                Array.Copy(content.ToCharArray(), 0, _characters[row], col, content.Length);
            }
            else
            {
                // If not, take a substring that fits and try to write the rest on the next row
                var thisRowContent = content.Substring(0, DISPLAY_WIDTH - col);
                Array.Copy(thisRowContent.ToCharArray(), 0, _characters[row], col, thisRowContent.Length);

                var nextRowContent = content.Substring(DISPLAY_WIDTH - col);
                WriteString(nextRowContent, row + 1, 0);
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

            for (var rowIndex = 0; rowIndex < DISPLAY_HEIGHT; rowIndex++)
            {
                var currentChunks = new List<(ConsoleColor color, List<char> content)>();
                var rowChars = _characters[rowIndex];
                var rowColors = _colors[rowIndex];
                var currentColor = _colors[rowIndex][0];
                var currentContent = new List<char>();

                for (var colIndex = 0; colIndex < DISPLAY_WIDTH; colIndex++)
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

            Debug.Assert(rows.Count == DISPLAY_HEIGHT);

            for (var rowIndex = 0; rowIndex < DISPLAY_HEIGHT; rowIndex++)
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
