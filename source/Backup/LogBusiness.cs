using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

namespace Backup
{
    /// <summary>
    /// Solution found at http://stackoverflow.com/a/3670628
    /// </summary>
    public static class LogBusiness
    {
        private static readonly BlockingCollection<string> Queue = new BlockingCollection<string>();

        private static readonly StringBuilder Report = new StringBuilder();

        static LogBusiness()
        {
            var thread = new Thread(
                () =>
                    {
                        while (true)
                        {
                            Console.WriteLine(Queue.Take());
                        }
                    }) { IsBackground = true };

            thread.Start();
        }

        /// <summary>
        /// Logs a message according whith the type and level
        /// </summary>
        /// <param name="message">Message to log</param>
        /// <param name="type">Type of log</param>
        /// <param name="level">Message level</param>
        public static void Log(string message, LogType type, LogLevel level)
        {
            var log = string.Format("{0} - ", DateTime.Now.ToShortTimeString());

            switch (level)
            {
                case LogLevel.Debug:
                    log += string.Format("[DEBUG] {0}", message);
                    break;
                case LogLevel.Info:
                    log += string.Format("[INFO] {0}", message);
                    break;
                case LogLevel.Warning:
                    log += string.Format("[WARNING] {0}", message);
                    break;
                case LogLevel.Error:
                    log += string.Format("[ERROR] {0}", message);
                    break;
                default:
                    log += message;
                    break;
            }

            switch (type)
            {
                case LogType.Console:
                    Queue.Add(log);
                    break;
                case LogType.File:
                    Queue.Add(log);
                    Report.AppendLine(log);
                    break;
            }
        }

        /// <summary>
        /// Writes the log into a text file
        /// </summary>
        /// <param name="folder">Folder to save the file</param>
        public static void DumpToFile(string folder)
        {
            var reportFileName = string.Format("backup-{0}.txt", DateTime.Now.ToString("yyyyMMddHHmm"));

            using (var fs = new FileStream(Path.Combine(folder, reportFileName), FileMode.Append, FileAccess.Write))
            {
                using (var outputFile = new StreamWriter(fs))
                {
                    outputFile.WriteLine(Report.ToString());
                }
            }
        }
    }
}
