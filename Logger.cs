using System;
using System.Globalization;
using System.IO;

namespace dsproject
{
    public class Logger
    {
        private const string LOGFILE_PATH = "log";

        public static void Log(string message)
        {
            if (!File.Exists(LOGFILE_PATH)) File.Create(LOGFILE_PATH).Close();

            using var stream = new StreamWriter(LOGFILE_PATH);
            stream.WriteLine(DateTime.Now.ToString("yyyy-M-d H:mm:ss", CultureInfo.InvariantCulture) + ": " + message);
        }
    }
}