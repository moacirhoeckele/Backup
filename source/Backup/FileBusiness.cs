using System;
using System.Collections.Generic;
using System.IO;

namespace Backup
{
    public static class FileBusiness
    {
        /// <summary>
        /// Solution found at https://msdn.microsoft.com/en-us/library/bb513869.aspx
        /// </summary>
        /// <param name="root">This parameter can be a file or path</param>
        /// <returns>A list of files</returns>
        public static List<FileInfo> TraverseTree(string root)
        {
            var filesList = new List<string>();
            var ret = new List<FileInfo>();
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
                            LogBusiness.Log(e.Message, LogType.File, LogLevel.Error);
                            continue;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            LogBusiness.Log(e.Message, LogType.File, LogLevel.Error);
                            continue;
                        }

                        string[] files;

                        try
                        {
                            files = Directory.GetFiles(currentDir);
                        }
                        catch (UnauthorizedAccessException e)
                        {
                            LogBusiness.Log(e.Message, LogType.File, LogLevel.Error);
                            continue;
                        }
                        catch (DirectoryNotFoundException e)
                        {
                            LogBusiness.Log(e.Message, LogType.File, LogLevel.Error);
                            continue;
                        }

                        // Perform the required action on each file here.
                        // Modify this block to perform your required task.
                        foreach (var file in files)
                        {
                            try
                            {
                                filesList.Add(file);
                            }
                            catch (FileNotFoundException e)
                            {
                                // If file was deleted by a separate application
                                //  or thread since the call to TraverseTree()
                                // then just continue.
                                LogBusiness.Log(e.Message, LogType.File, LogLevel.Error);
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
                    LogBusiness.Log(string.Format("Directory {0} not exists.", root), LogType.File, LogLevel.Warning);
                }
            }
            else
            {
                filesList.Add(root);
            }

            foreach (var f in filesList)
            {
                var fi = new FileInfo(f);
                ret.Add(fi);
                LogBusiness.Log(string.Format("Analyzing file {0}", fi.Name), LogType.Console, LogLevel.Info);
            }

            return ret;
        }

        public static void DeleteFile(string fullName)
        {
            try
            {
                File.Delete(fullName);
            }
            catch (UnauthorizedAccessException uae)
            {
                //TODO: do something here
            }
        }
    }
}
