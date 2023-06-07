using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace Bifrost
{
    internal class LocalFileCopy
    {
        Logger Logger = Logger.GetLogger();
        JsonConfig jsonConf = JsonConfig.GetConfig();
        public LocalFileCopy()
        {

        }
        string backupFilePath = null; 
        int backupMaxLengthConfigurated = 0; 
        int backupMaxAge = 1;
        public void Start(FileEventObject serviceObject)
        {
            try
            {
                backupFilePath = jsonConf.backupDirectory;
                backupMaxLengthConfigurated = jsonConf.backupLengthDays;
            }
            catch (Exception ex){
                Logger.LogException(ex);
            }
            try
            {
                backupMaxAge = backupMaxLengthConfigurated;
            }
            catch(Exception ex)
            {
                Logger.log("Ex: " + ex.StackTrace, LogEventType.ERROR);
            }

            if (!Directory.Exists(backupFilePath))
            {
                Directory.CreateDirectory(backupFilePath);
            }
            Task.Run(async () =>
            {
                    //Handle new QueueObject
                using(FileStream sourceStream = File.Open(serviceObject.path, FileMode.Open, FileAccess.Read))
                {
                    using (FileStream destinationStream = File.Create(Path.Combine(backupFilePath, Path.GetFileName(serviceObject.filename))))
                    {
                        await sourceStream.CopyToAsync(destinationStream);
                                
                    }
                }


                try
                {
                    File.Delete(serviceObject.path);
                    Logger.log("Deleted file: " + serviceObject.path);
                }
                catch (Exception ex) 
                {
                    Logger.log("Error deleting file " + serviceObject.path, LogEventType.ERROR);
                    Logger.log(ex.Message, LogEventType.ERROR);
                    Logger.log(ex.StackTrace, LogEventType.ERROR);
                }

                

                //Check backup age
                string[] files = Directory.GetFiles(backupFilePath);

                foreach(string file in files)
                {
                    try
                    {
                        DateTime creationTime = File.GetCreationTime(file);
                        if(DateTime.Now.Subtract(creationTime).TotalDays > backupMaxAge)
                        {
                            File.Delete(file);
                            Logger.log("Deleted backup: " + file, LogEventType.DEBUG);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.log("Error deleting backup " + serviceObject.path, LogEventType.ERROR);
                        Logger.log(ex.Message, LogEventType.ERROR);
                        Logger.log(ex.StackTrace, LogEventType.ERROR);

                    }
                }
                  
               
            });
        }

    }

    internal class LocalFileTransfer
    {
        Logger Logger = Logger.GetLogger();
        LocalFileCopy LocalFileCopy = new LocalFileCopy();

        public async void TransferLocalFile(FileEventObject fo)
        {
            await Task.Run(async () =>
            {
                string newFileName;
                if (fo.serviceType == "local")
                {
                    try
                    {
                        if(fo.rename)
                        {
                            newFileName = RenameFile(fo);
                        }
                        else
                        {
                            newFileName = fo.path;
                        }
                        
                        
                        using (FileStream sourceStream = File.Open(newFileName, FileMode.Open, FileAccess.Read))
                        {
                            Logger.log($"Copying {Path.GetFileName(newFileName)} to {fo.RemotePath}");
                            using (FileStream destinationStream = File.Create(Path.Combine(fo.RemotePath, Path.GetFileName(newFileName))))
                            {
                                Logger.log($"Local file copy process: {newFileName}", LogEventType.DEBUG);
                                await sourceStream.CopyToAsync(destinationStream);
                                LocalFileCopy.Start(fo);

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogException(e);
                    }
                }
            });
        }

        private string RenameFile(FileEventObject _fo)
        {
            string filePath = _fo.path;

            // Generate timestamp in the desired format
            string timestamp = DateTime.Now.ToString("yyyy-MM-ddTHH-mm-ss");

            // Get the directory, file name, and extension from the original file path
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            // Create the new file name with the timestamp and desired extension
            string newFileName = $"{_fo.filenameAddition}_{timestamp}.txt";

            // Build the new file path with the renamed file name
            string newFilePath = Path.Combine(directory, newFileName);
            Logger.log($"attempting to rename {_fo.path} to {newFilePath}", LogEventType.DEBUG);
            // Rename the file and change the file extension
            File.Move(filePath, newFilePath);
            Logger.log($"Renamed succeeded: {newFilePath}", LogEventType.DEBUG);
            return newFilePath;

        }

    }
}

