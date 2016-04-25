using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Backup
{
    using System.IO;
    using System.Text;

    /// <summary>
    /// http://stackoverflow.com/a/3670628
    /// </summary>
    public static class NonBlockingConsole
    {
        private static readonly BlockingCollection<string> Queue = new BlockingCollection<string>();

        private static readonly StringBuilder Report = new StringBuilder();

        static NonBlockingConsole()
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

        public static void WriteLine(string value)
        {
            Report.AppendLine(value);
            Queue.Add(value);
        }

        public static void WriteLine(string format, params object[] arg)
        {
            WriteLine(string.Format(format, arg));
        }

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
