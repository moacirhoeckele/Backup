using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Backup
{
    internal static class BackupBusiness
    {
        private static BackupProcess backupProcess;

        public static void StartPipeline(BackupProcess backupProcessParam)
        {
            backupProcess = backupProcessParam;
            backupProcess.StartedTime = DateTime.Now;
            backupProcess.BackupBaseFolder = ConfigurationManager.AppSettings["BackupBaseFolder"];
            backupProcess.BackupBasePath = Path.Combine(backupProcess.Destination, backupProcess.BackupBaseFolder);

            Console.Title = "Backup Process";
            LogBusiness.Log("====================================================================================", LogType.File, LogLevel.None);
            LogBusiness.Log("====================================== BACKUP ======================================", LogType.File, LogLevel.None);
            LogBusiness.Log("====================================================================================", LogType.File, LogLevel.None);

            var bufferSize = Convert.ToInt32(ConfigurationManager.AppSettings["BufferSize"]);
            var sourceFilesBuffer = new BlockingCollection<FileInfo>(bufferSize);

            try
            {
                var token = new CancellationToken();
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    var f = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);

                    var stage1 = f.StartNew(() => GatherSourcesFiles(sourceFilesBuffer, cts));
                    var stage2 = f.StartNew(() => CopyFiles(sourceFilesBuffer, cts));

                    Task.WaitAll(stage1, stage2);
                }                
            }
            catch (Exception ex)
            {
                LogBusiness.Log(ex.Message, LogType.File, LogLevel.Error);
            }
            finally
            {
                LogBusiness.Log("====================================================================================", LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== Total of source files: {0}", backupProcess.SourceFiles.Count)), LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== Total of copied files: {0}", backupProcess.CopiedFiles.Count)), LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== Total of deleted files on destination: {0}", backupProcess.DeletedFiles.Count)), LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== Start time: {0}", backupProcess.StartedTime)), LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== End time: {0}", DateTime.Now)), LogType.File, LogLevel.None);
                LogBusiness.Log(string.Format("{0,-81} ==", string.Format("== Time spent: {0}", DateTime.Now.Subtract(backupProcess.StartedTime).ToString("g"))), LogType.File, LogLevel.None);
                LogBusiness.Log("====================================================================================", LogType.File, LogLevel.None);
                LogBusiness.DumpToFile(backupProcess.Destination);
            }
        }

        private static void GatherSourcesFiles(BlockingCollection<FileInfo> sourceFilesBuffer, CancellationTokenSource cts)
        {
            try
            {
                var token = cts.Token;
                backupProcess.SourceFiles = new List<FileInfo>();

                foreach (var source in backupProcess.Sources.TakeWhile(source => !token.IsCancellationRequested))
                {
                    backupProcess.SourceFiles.AddRange(FileBusiness.TraverseTree(source));
                }

                backupProcess.SourceFiles.ForEach(x => sourceFilesBuffer.Add(x, token));
            }
            catch (Exception e)
            {
                // If an exception occurs, notify all other pipeline stages.
                cts.Cancel();
                if (!(e is OperationCanceledException))
                {
                    throw;
                }
            }
            finally
            {
                sourceFilesBuffer.CompleteAdding();
            }
        }

        private static void CopyFiles(BlockingCollection<FileInfo> sourceFilesBuffer, CancellationTokenSource cts)
        {
            try
            {
                var token = cts.Token;
                backupProcess.CopiedFiles = new List<FileInfo>();
                backupProcess.DestinationFiles = new List<FileInfo>();
                backupProcess.DeletedFiles = new List<FileInfo>();

                // To copy a folder's contents to a new location:
                // Create a new target folder, if necessary.
                if (!Directory.Exists(backupProcess.BackupBasePath))
                {
                    Directory.CreateDirectory(backupProcess.BackupBasePath);
                    LogBusiness.Log(string.Format("Creating backup destination folder {0}", backupProcess.BackupBasePath), LogType.File, LogLevel.Info);
                }

                foreach (var fileToCopy in sourceFilesBuffer.GetConsumingEnumerable())
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    backupProcess.CopiedFiles.Add(fileToCopy);

                    // Changes the source root by the destination root plus the base folder.
                    // TODO: Search for a better way to do this.
                    var targetPath = Path.Combine(backupProcess.BackupBasePath, fileToCopy.Directory.FullName.Replace(fileToCopy.Directory.Root.Name, string.Empty));

                    // Use Path class to manipulate file and directory paths.
                    var destFile = Path.Combine(targetPath, fileToCopy.Name);

                    // Delete the destination file if it exists and his modification date 
                    // is older than the source file's modification date.
                    if (File.Exists(destFile))
                    {
                        var destFileFi = new FileInfo(destFile);

                        if (destFileFi.LastWriteTime < fileToCopy.LastWriteTime)
                        {
                            // To copy a file to another location and 
                            // overwrite the destination file if it already exists.
                            try
                            {
                                File.Copy(fileToCopy.FullName, destFile, true);
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // Set new file attributes and try to copy again.
                                File.SetAttributes(destFile, FileAttributes.Normal);
                                File.Copy(fileToCopy.FullName, destFile, true);
                            }
                            finally
                            {
                                LogBusiness.Log(string.Format("Rewriting existing file {0} to {1}...", fileToCopy.FullName, destFile), LogType.Console, LogLevel.Info);
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
                            LogBusiness.Log(string.Format("Creating directory {0}...", targetPath), LogType.Console, LogLevel.Info);
                        }

                        // To copy a file to another location
                        File.Copy(fileToCopy.FullName, destFile);

                        LogBusiness.Log(string.Format("Copying new file {0} to {1}...", fileToCopy.FullName, destFile), LogType.Console, LogLevel.Info);
                    }
                }

                // Gathering all files from backup
                FileBusiness.TraverseTree(backupProcess.BackupBasePath).ForEach(x => backupProcess.DestinationFiles.Add(x));

                // Comparing backup file against source files to find deleted files
                // TODO: I think is possible improve it.
                foreach (var destFI in backupProcess.DestinationFiles)
                {
                    var destFile = destFI.FullName.Replace(backupProcess.BackupBasePath + "\\", string.Empty);
                    var canDeleteFile = true;

                    foreach (var sourceFI in backupProcess.CopiedFiles)
                    {
                        if (sourceFI.FullName.Replace(sourceFI.Directory.Root.Name, string.Empty) == destFile)
                        {
                            canDeleteFile = false;
                        }
                    }

                    if (canDeleteFile)
                    {
                        backupProcess.DeletedFiles.Add(destFI);
                    }
                }

                // Excludes the files
                foreach (var fileToDelete in backupProcess.DeletedFiles)
                {
                    FileBusiness.DeleteFile(fileToDelete.FullName);
                    LogBusiness.Log(string.Format("Deleting nonexistent file {0}...", fileToDelete.FullName), LogType.Console, LogLevel.Info);
                }
            }
            catch (Exception e)
            {
                cts.Cancel();
                if (!(e is OperationCanceledException))
                {
                    throw;
                }
            }
        }
    }
}
