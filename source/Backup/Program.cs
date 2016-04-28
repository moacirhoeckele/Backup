using System;
using System.Linq;

namespace Backup
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // TODO: Validate this
            var backupProcess = new BackupProcess
                                    {
                                        Options = args.TakeWhile(x => x.StartsWith("/")),
                                        Sources = args[args.Length - 2].Split(','),
                                        Destination = args[args.Length - 1]
                                    };

            BackupBusiness.StartPipeline(backupProcess);

            if (backupProcess.Options != null && (backupProcess.Options.Contains("/e") || backupProcess.Options.Contains("/E")))
            {
                return;
            }

            LogBusiness.Log("Press any key to exit", LogType.Console, LogLevel.Info);
            Console.ReadKey();
        }
    }
}
