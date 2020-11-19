using System;
using System.Collections;
using System.Collections.Generic;
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

        public char[][] Rows { get; }

        public Display()
        {
            // Init Rows
            Rows = new char[DISPLAY_HEIGHT][];
            for (var rowIndex = 0; rowIndex < DISPLAY_HEIGHT; rowIndex++)
            {
                Rows[rowIndex] = new char[DISPLAY_WIDTH];
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

                WriteString("Display Initialized!", 0,0);
                WriteString(("Window: W: " + Console.WindowWidth + " h: " + Console.WindowHeight), 1,0);
                WriteString(("Buffer: W: " + Console.BufferWidth + " h: " + Console.BufferHeight), 2,0);

                Update();
            }
            else
            {
                throw new NotImplementedException("Display not implemented for non-windows OSs");
            }
        }

        public void Clear()
        {
            foreach (var row in Rows)
            {
                Array.Clear(row, 0, row.Length);
            }
        }

        public void InsertArray(char[][] content, int row, int col)
        {
            if (row is < 0 or >= DISPLAY_HEIGHT) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DISPLAY_WIDTH) throw new ArgumentOutOfRangeException(nameof(col));

            for (var contentRowIndex = 0; contentRowIndex < content.Length; contentRowIndex++)
            {
                Array.Copy(content[contentRowIndex], 0, Rows[row + contentRowIndex], col, content[contentRowIndex].Length);
            }
        }

        public void WriteString(string content, int row, int col)
        {
            if (row is < 0 or >= DISPLAY_HEIGHT) throw new ArgumentOutOfRangeException(nameof(row));
            if (col is < 0 or >= DISPLAY_WIDTH) throw new ArgumentOutOfRangeException(nameof(col));

            // Check if string fits on this row starting from "col"
            if (content.Length <= DISPLAY_WIDTH - col)
            {
                Array.Copy(content.ToCharArray(), 0, Rows[row], col, content.Length);
            }
            else
            {
                // If not, take a substring that fits and try to write the rest on the next row
                var thisRowContent = content.Substring(0, DISPLAY_WIDTH - col);
                Array.Copy(thisRowContent.ToCharArray(), 0, Rows[row], col, thisRowContent.Length);

                var nextRowContent = content.Substring(DISPLAY_WIDTH - col);
                WriteString(nextRowContent, row + 1, 0);
            }
        }

        public void Update()
        {
            for (var y = 0; y < DISPLAY_HEIGHT; y++)
            {
                Console.SetCursorPosition(0, y);
                Console.Write(Rows[y]);
            }
        }
    }
}
