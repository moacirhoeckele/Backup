using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Backup
{
    public static class Program
    {
        private static string[] sources;

        private static string destination;

        private static List<FileInfo> sourceFiles;

        private static List<FileInfo> destinationFiles;

        private static List<FileInfo> destinationFilesToDelete;

        private static string backupFolder;

        private static string backupBasePath;

        private static DateTime startedTime;

        private static int copiedFiles;

        public static void Main(string[] args)
        {
            startedTime = DateTime.Now;

            Console.Title = "Backup";
            NonBlockingConsole.WriteLine("====================================================================================");
            NonBlockingConsole.WriteLine("===================================== BACKUP =======================================");
            NonBlockingConsole.WriteLine("====================================================================================");

            sources = args[0].Split(',');
            destination = args[1];
            sourceFiles = new List<FileInfo>();
            destinationFiles = new List<FileInfo>();
            destinationFilesToDelete = new List<FileInfo>();
            copiedFiles = 0;

            backupFolder = "backup";
            backupBasePath = Path.Combine(destination, backupFolder);

            // Gathering source files
            foreach (var source in sources)
            {
                TraverseTree(source, BackupDirection.Source);
            }

            // To copy a folder's contents to a new location:
            // Create a new target folder, if necessary.
            if (!Directory.Exists(backupBasePath))
            {
                Directory.CreateDirectory(backupBasePath);
                NonBlockingConsole.WriteLine("Creating backup destination folder {0}...", backupBasePath);
            }
            else
            {
                // Gathering destination files
                TraverseTree(backupBasePath, BackupDirection.Destination);

                // Gathering deleted files
                // TODO: I think is possible improve it.
                destinationFilesToDelete.AddRange(
                    destinationFiles.Where(
                        d =>
                        !sourceFiles.Select(s => s.FullName.Replace(s.Directory.Root.Name, string.Empty))
                             .Contains(d.FullName.Replace(backupBasePath + "\\", string.Empty))));
            }
                        
            var destinationDi = new DriveInfo(Path.GetPathRoot(destination));
            var requiredFreeSpace = sourceFiles.Sum(x => x.Length) - destinationFilesToDelete.Sum(x => x.Length);

            if (destinationDi.TotalFreeSpace < requiredFreeSpace)
            {
                NonBlockingConsole.WriteLine("Is not possible to perform the backup.");
                NonBlockingConsole.WriteLine("Total available space: {0, 15} bytes.", destinationDi.TotalFreeSpace);
                NonBlockingConsole.WriteLine("Total required space: {0, 15} bytes.", requiredFreeSpace);
            }
            else
            {
                foreach (var dftd in destinationFilesToDelete)
                {
                    try
                    {
                        File.Delete(dftd.FullName);
                    }
                    catch (UnauthorizedAccessException uae)
                    {
                        //TODO: do something here
                    }
                    finally
                    {
                        NonBlockingConsole.WriteLine("Deleting nonexistent file {0}...", dftd.FullName);
                    }                    
                }

                foreach (var sf in sourceFiles)
                {
                    // Changes the source root by the destination root plus the base folder.
                    // TODO: Search for a better way to do this.
                    var targetPath = Path.Combine(backupBasePath, sf.Directory.FullName.Replace(sf.Directory.Root.Name, string.Empty));

                    // Use Path class to manipulate file and directory paths.
                    var destFile = Path.Combine(targetPath, sf.Name);

                    // Delete the destination file if it exists and his modification date 
                    // is older than the source file's modification date.
                    if (File.Exists(destFile))
                    {
                        var destFileFi = new FileInfo(destFile);

                        if (destFileFi.LastWriteTime < sf.LastWriteTime)
                        {
                            // To copy a file to another location and 
                            // overwrite the destination file if it already exists.
                            try
                            {
                                File.Copy(sf.FullName, destFile, true);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Set new file attributes and try to copy again.
                                File.SetAttributes(destFile, FileAttributes.Normal);
                                File.Copy(sf.FullName, destFile, true);
                            }
                            finally
                            {
                                NonBlockingConsole.WriteLine("Copying file {0} to {1}...", sf.FullName, destFile);
                                copiedFiles++;
                            }
                        }
                    }
                    else
                    {
                        // To copy a folder's contents to a new location:
                        // Create a new target folder, if necessary.
                        if (!Directory.Exists(targetPath))
                        {
                            Directory.CreateDirectory(targetPath);
                            NonBlockingConsole.WriteLine("Creating directory {0}...", targetPath);
                        }

                        // To copy a file to another location
                        File.Copy(sf.FullName, destFile);

                        NonBlockingConsole.WriteLine("Copying file {0} to {1}...", sf.FullName, destFile);
                        copiedFiles++;
                    }
                }
            }

            NonBlockingConsole.WriteLine("====================================================================================");
            NonBlockingConsole.WriteLine("- Total of source files: {0}", sourceFiles.Count);
            NonBlockingConsole.WriteLine("- Total of copied files: {0}", copiedFiles);
            NonBlockingConsole.WriteLine("- Total of deleted files on destination: {0}", destinationFilesToDelete.Count);
            NonBlockingConsole.WriteLine("- Time spent: {0}", DateTime.Now.Subtract(startedTime));
            NonBlockingConsole.WriteLine("====================================================================================");
            NonBlockingConsole.DumpToFile(destination);

            NonBlockingConsole.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        public enum BackupDirection
        {
            Source,

            Destination
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/bb513869.aspx
        /// </summary>
        /// <param name="root"></param>
        /// <param name="source"></param>
        public static void TraverseTree(string root, BackupDirection direction)
        {
            var attr = File.GetAttributes(root);

            if (attr.HasFlag(FileAttributes.Directory))
            {
                if (Directory.Exists(root))
                {
                    // Data structure to hold names of subfolders to be
                    // examined for files.
                    var dirs = new Stack<string>();

                    dirs.Push(root);

                    while (dirs.Count > 0)
                    {
                        var currentDir = dirs.Pop();
                        string[] subDirs;

                        try
                        {
                            subDirs = Directory.GetDirectories(currentDir);
                        }
                            // An UnauthorizedAccessException exception will be thrown if we do not have
                            // discovery permission on a folder or file. It may or may not be acceptable 
                            // to ignore the exception and continue enumerating the remaining files and 
                            // folders. It is also possible (but unlikely) that a DirectoryNotFound exception 
                            // will be raised. This will happen if currentDir has been deleted by
                            // another application or thread after our call to Directory.Exists. The 
                            // choice of which exceptions to catch depends entirely on the specific task 
                            // you are intending to perform and also on how much you know with certainty 
                            // about the systems on which this code will run.
                        catch (UnauthorizedAccessException e)
                        {
                            NonBlockingConsole.WriteLine(e.Message);
                            continue;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            NonBlockingConsole.WriteLine(e.Message);
                            continue;
                        }

                        string[] files;

                        try
                        {
                            files = Directory.GetFiles(currentDir);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            NonBlockingConsole.WriteLine(e.Message);
                            continue;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            NonBlockingConsole.WriteLine(e.Message);
                            continue;
                        }

                        // Perform the required action on each file here.
                        // Modify this block to perform your required task.
                        foreach (var file in files)
                        {
                            try
                            {
                                AddFileToSourceList(file, direction);
                            }
                            catch (FileNotFoundException e)
                            {
                                // If file was deleted by a separate application
                                //  or thread since the call to TraverseTree()
                                // then just continue.
                                NonBlockingConsole.WriteLine(e.Message);
                                continue;
                            }
                        }

                        // Push the subdirectories onto the stack for traversal.
                        // This could also be done before handing the files.
                        foreach (var str in subDirs)
                        {
                            dirs.Push(str);
                        }
                    }
                }
                else
                {
                    NonBlockingConsole.WriteLine("Directory {0} not exists.", root);
                }
            }
            else
            {
                AddFileToSourceList(root, direction);
            }
        }

        private static void AddFileToSourceList(string file, BackupDirection direction)
        {
            // Perform whatever action is required in your scenario.
            var fi = new FileInfo(file);
            NonBlockingConsole.WriteLine("Analyzing {0} file {1}...", direction == BackupDirection.Source ? "source" : "destination", fi.Name);

            // Adds file to the files list
            switch (direction)
            {
                case BackupDirection.Source:
                    sourceFiles.Add(fi);
                    break;
                case BackupDirection.Destination:
                    destinationFiles.Add(fi);
                    break;
            }            
        }
    }
}
