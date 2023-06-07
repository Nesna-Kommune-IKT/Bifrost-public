using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;
using System.IO;
using System.Configuration;
using System.Collections.Specialized;
using WinSCP;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics.Eventing.Reader;
using System.Collections;


namespace Bifrost
{
    public struct FileEventObject
    {
        public int uid;
        public string path;
        public string serviceName;
        public string filename;
        public LogEventType logEventType;
        public string serviceType;
        public string filenameAddition;
        public string RemotePath;
        public bool rename;

        public FileEventObject(int uid = 0, string path = "", string serviceName = "dev", 
            string filename = "",string filenameaddition = "", string serviceType = "remote", string remotePath = "", bool rename = false, 
            LogEventType logEventType = LogEventType.DEBUG)
        {
            this.uid = uid;
            this.path = path;
            this.serviceName = serviceName; 
            this.filename = filename;
            this.logEventType = logEventType;
            this.filenameAddition = filenameaddition;
            this.serviceType = serviceType;
            this.RemotePath = remotePath;
            this.rename = rename;
        }

    }
    public partial class Service1 : ServiceBase
    {
        int _uid = 1;

        ConcurrentQueue<FileEventObject> localQueue = new ConcurrentQueue<FileEventObject>();
        JsonConfig jsonConf = JsonConfig.GetConfig();
        Logger Logger = Logger.GetLogger();
        
        
        public Service1()
        {
            InitializeComponent();
            
        }
        /// <summary>
        /// Starting point of the service.
        /// /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {

            Logger.log("Tjeneste startet " + DateTime.Now, LogEventType.INFO);
            jsonConf.queue = localQueue;
            jsonConf.ParseJsonConfiguration();
            ScpThread();
        }


        protected override void OnStop()
        {
            Logger.log("Tjeneste stoppet " + DateTime.Now);
        }

        public void ScpThread()
        {
            var fileTransferThread = new FileTransferThread(localQueue, "FileTransferThread");
            fileTransferThread.Start();
        }

        private void eventLog1_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

        }
    }
}
