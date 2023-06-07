using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WinSCP;

namespace Bifrost
{
    public class ServiceAgent
    {
        //
        Logger Logger = Logger.GetLogger();
        public string serviceName { get; set; }
        public string hostname { get; set; }
        public string scp_username { get; set; }
        public string scp_password { get; set; }
        public string scp_RemoteRoot { get; set; }
        public string scp_hostkey { get; set; }
        public string localFolder { get; set; }
        public List<string> filetypes { get; set; }
        public bool developer { get; set; }
        public bool developerInstance { get; set; }
        public string serviceType { get; set; }
        public bool rename { get; set; }
        public string filenameAddition { get; set; }
        public ConcurrentQueue<FileEventObject> queue { get; set; }

        public void StartWatcher()
        {
            var watcher = new FileSystemWatcher();
            watcher.Path = localFolder;
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter = NotifyFilters.Attributes
                                | NotifyFilters.CreationTime
                                | NotifyFilters.DirectoryName
                                | NotifyFilters.FileName
                                | NotifyFilters.LastAccess
                                | NotifyFilters.LastWrite
                                | NotifyFilters.Security
                                | NotifyFilters.Size;
            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Deleted += OnDelete;
            watcher.Renamed += OnRename;
            watcher.Error += OnError;

        }
        private void OnChanged(object sender, FileSystemEventArgs e) 
        {
        }
        private void OnCreated(object sender, FileSystemEventArgs e) 
        {
            AddToQueue(e.FullPath, e.Name);
        }
        private void OnRename(object sender, FileSystemEventArgs e) { }
        private void OnDelete(object sender, FileSystemEventArgs e) { }
        private void OnError(object sender, ErrorEventArgs e) =>
            Logger.LogException(e.GetException());

        private void AddToQueue(string value, string filename)
        {
            FileEventObject _si = new FileEventObject();
            _si.serviceName = serviceName;
            _si.filename = filename;
            _si.path = value;
            _si.RemotePath = scp_RemoteRoot;
            _si.serviceType = serviceType;
            _si.filenameAddition = filenameAddition;
            Logger.log("ENQUEUE: " + _si.filename, LogEventType.INFO);
            queue.Enqueue(_si);
        }
    }

    public sealed class JsonConfig
    {
        Logger Logger { get; set; }
        private static JsonConfig _config;
        private static readonly object _syncLock = new object();
        public string logDirectory { get; set; }
        public string backupDirectory { get; set; }
        public List<ServiceAgent> services { get; set; }
        public int backupLengthDays { get; set; }
        public ConcurrentQueue<FileEventObject> queue { get; set; }
        public static JsonConfig GetConfig()
        {
            if (_config == null)
            {
                lock (_syncLock)
                {
                    if (_config == null)
                    {
                        _config = new JsonConfig();
                    }
                }
            }
            return _config;
        }

        public void ParseJsonConfiguration()
        {
            Logger = Logger.GetLogger();
            string json = null;

            string jsonFile = AppDomain.CurrentDomain.BaseDirectory + "\\ApplicationConfig.json";
            if (!File.Exists(jsonFile))
            {
                using (StreamWriter sw = File.CreateText(jsonFile))
                {
                    Logger.log("JSON File does not exist", LogEventType.ERROR);
                }
            }
            else
            {
                Logger.log("JSON File does exist at: " + jsonFile, LogEventType.DEBUG);
            }
            try
            {
                Logger.log("Attempting to read JSON: " + jsonFile, LogEventType.DEBUG);
                json = File.ReadAllText(jsonFile);
                Logger.log("JSON File content: " + json, LogEventType.DEBUG);
            }
            catch (Exception ex)
            {
                Logger.log("Json failed: " + json);
                Logger.LogException(ex);
                return;
            }
            try
            {
                backupLengthDays = backupLengthDays;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
            try
            {
                Logger.log("Attempting to deserialize: " + json, LogEventType.DEBUG);
                _config = JsonSerializer.Deserialize<JsonConfig>(json);
                Logger.log("Success in deserialize", LogEventType.DEBUG);

            }
            catch (Exception ex)
            {
                Logger.log("Failed to deserialize: " + json, LogEventType.DEBUG);
                Logger.log("Failed to deserialize(_config): " + _config, LogEventType.DEBUG);
                Logger.LogException(ex);
            }
            
            logDirectory = _config.logDirectory;
            backupDirectory = _config.backupDirectory;
            backupLengthDays = _config.backupLengthDays;
            Logger.log($"Configured Log Directory: {logDirectory}", LogEventType.DEBUG);
            Logger.log($"Configured Backup Directory: {backupDirectory}", LogEventType.DEBUG);
            Logger.log($"Configured Backup Lengt: {backupLengthDays}", LogEventType.DEBUG);

            foreach (ServiceAgent serviceSetting in _config.services)
            {
                Logger.log($"Parsing service {serviceSetting.serviceName} with filename {serviceSetting.filenameAddition}", LogEventType.DEBUG);
                serviceSetting.queue = queue; 
                serviceSetting.StartWatcher();
            }
        }
    }

    public static class FileTransferHelper
    {
        public static async Task TransferLocaLFilesAsync(FileEventObject fileEventObject)
        {
            var LocalFileTransferThread = new LocalFileTransfer();
            Logger Logger = Logger.GetLogger();
            try
            {
                LocalFileTransferThread.TransferLocalFile(fileEventObject);
            }
            catch (Exception ex)
            {
                Logger.LogException (ex);
            }
        }

        public static async Task TransferFileAsync(string remotePath, SessionOptions sessionOption, FileEventObject serviceObject)
        {
            var localFileCopyThread = new LocalFileCopy();
            Logger Logger = Logger.GetLogger();
            using (Session session = new Session())
            {
                Logger.log($"{serviceObject.serviceName} - Attempting session");
                try
                {
                    session.Open(sessionOption);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);
                }
                

                TransferOptions transferOptions = new TransferOptions
                {
                    TransferMode = TransferMode.Binary
                };
                

                string remoteFilePath = remotePath + serviceObject.filename.Substring(0).Replace('\\', '/');
                string directoryPath = Path.GetDirectoryName(remoteFilePath);
                string remoteDirectoryPath = directoryPath.Substring(0).Replace('\\', '/');
                Logger.log($"{serviceObject.serviceName} - Starting transferring {serviceObject.path} to remote: {remoteFilePath}");

                if (!session.FileExists(remoteDirectoryPath))
                {
                    Logger.log($"{serviceObject.serviceName} no remote dir - creating remote directory {remoteFilePath}");
                    session.CreateDirectory(remoteDirectoryPath);
                }

                
                TransferOperationResult transferResult = session.PutFiles(serviceObject.path, remoteFilePath, false, transferOptions);

                transferResult.Check();
                Logger.log($"{serviceObject.serviceName} - {serviceObject.path} succeeded");
                foreach (TransferEventArgs transfer in transferResult.Transfers)
                {
                    Logger.log($"{serviceObject.serviceName} - {transfer.FileName} succeeded");
                }
                localFileCopyThread.Start(serviceObject);
            }

        }
    }
}
