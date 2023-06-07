using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WinSCP;

namespace Bifrost
{
    /// <summary>
    /// A data class for sessions that is contained in the JSON config
    /// </summary>

    public class SessionOptionsManager
    {
        private Dictionary<string, SessionOptions> _options;

        public SessionOptionsManager()
        {
            _options = new Dictionary<string, SessionOptions>();
        }

        public void AddSessionOptions(string serviceName, SessionOptions options)
        {
            _options[serviceName] = options;
        }

        public SessionOptions GetSessionOptions(string serviceName)
        {
            if (_options.ContainsKey(serviceName))
            {
                return _options[serviceName];
            }
            else
            {
                return new SessionOptions();
            }
        }
    }
    /// <summary>
    /// FileTransferThread utilizes TransferFileAsync method.
    ///
    /// </summary>
    public class FileTransferThread
    {
        private readonly string _remotePath;
        private readonly string _hostname;
        private readonly string _username;
        private readonly string _password;
        private readonly string _hostkey;
        private readonly ConcurrentQueue<FileEventObject> _localPaths;
        private readonly string _serviceName;
        Logger Logger = Logger.GetLogger();
        JsonConfig conf = JsonConfig.GetConfig();
        SessionOptionsManager optionsManager = new SessionOptionsManager();
        LocalFileCopy localFileCopyThread = new LocalFileCopy();
        public FileTransferThread(ConcurrentQueue<FileEventObject> localPaths, string serviceName)
        {
            _localPaths = localPaths;
            _serviceName = serviceName;
        }
        // 
        public void Start()
        {
            String remotePath;
            foreach (var service in conf.services) 
            {
                if (service.serviceType == "local")
                {
                    // TODO: Remove this function
                }
                else
                {
                    SessionOptions sessionOption = new SessionOptions
                    {
                        Protocol = Protocol.Sftp,
                        HostName = service.hostname,
                        UserName = service.scp_username,
                        Password = service.scp_password,
                        SshHostKeyFingerprint = service.scp_hostkey
                    };
                    optionsManager.AddSessionOptions(service.serviceName, sessionOption);
                }

            }
            SessionOptions options;
            
            Logger.log($"Starting FileTransferThread {_serviceName}");

            Task.Run(async () =>
            {
                while (true)
                {
                    if(_localPaths.TryDequeue(out FileEventObject _fileEventObject)) 
                    {
                        if(_fileEventObject.serviceType == "local")
                        {
                            await FileTransferHelper.TransferLocaLFilesAsync(_fileEventObject);
                        }
                        else if (_fileEventObject.serviceType == "remote")
                        {
                            switch (_fileEventObject.serviceName)
                            {
                                case "developer":
                                    options = optionsManager.GetSessionOptions(_fileEventObject.serviceName);
                                    remotePath = _fileEventObject.RemotePath;
                                    break;
                                default:
                                    options = optionsManager.GetSessionOptions(_fileEventObject.serviceName);
                                    remotePath = _fileEventObject.RemotePath;
                                    break;
                            }
                            await FileTransferHelper.TransferFileAsync(remotePath, options, _fileEventObject);
                        }
                        else
                        {
                            return;
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            });
        }
    }
}
