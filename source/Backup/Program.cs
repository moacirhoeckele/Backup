using System;

namespace Backup
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            // TODO: Validate this
            var sources = args[0].Split(',');
            var destination = args[1];

            BackupBusiness.StartPipeline(sources, destination);

            LogBusiness.Log("Press any key to exit", LogType.Console, LogLevel.Info);
            Console.ReadKey();
        }
    }
}
