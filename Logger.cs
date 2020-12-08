using System;
using System.Globalization;
using System.IO;

namespace dsproject
{
    public class Logger
    {
        private readonly string _logName;

        public Logger(string logName)
        {
            _logName = logName;
        }

        public void Log(string message)
        {
            Log(message, _logName);
        }

        public static void Log(string message, string fileName)
        {
            var logFile = fileName + ".txt";
            var path = Directory.GetCurrentDirectory() + @"\" + logFile;
            using var stream = File.AppendText(path);
            stream.WriteLine(DateTime.Now.ToString("yyyy-M-d H:mm:ss", CultureInfo.InvariantCulture) + ": " + message);
        }
    }
}