using System;
using System.Collections.Generic;
using System.IO;

namespace Backup
{
    public class BackupProcess
    {
        public string Destination { get; set; }

        public string[] Sources { get; set; }

        public IEnumerable<string> Options { get; set; }

        public DateTime StartedTime { get; set; }

        public List<FileInfo> SourceFiles { get; set; }

        public List<FileInfo> CopiedFiles { get; set; }

        public List<FileInfo> DeletedFiles { get; set; }

        public string BackupBaseFolder { get; set; }

        public string BackupBasePath { get; set; }

        public List<FileInfo> DestinationFiles { get; set; }
    }
}