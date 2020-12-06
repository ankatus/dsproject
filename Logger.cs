using System;
using System.Globalization;
using System.IO;

namespace dsproject
{
    public class Logger
    {
        private const string LOGFILE_RELATIVE_PATH = "log.txt";

        public static void Log(string message)
        {
            var path = Directory.GetCurrentDirectory() + @"\log.txt";
            using var stream = File.AppendText(LOGFILE_RELATIVE_PATH);
            stream.WriteLine(DateTime.Now.ToString("yyyy-M-d H:mm:ss", CultureInfo.InvariantCulture) + ": " + message);
        }
    }
}