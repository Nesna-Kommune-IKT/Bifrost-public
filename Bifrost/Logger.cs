using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bifrost
{
    public enum LogEventType
    {
        CREATE = 001,
        CHANGE = 002,
        DELETE = 003,
        RENAME = 004,
        DEBUG = 005,
        ERROR = 006,
        WARNING = 007,
        DEVINFO = 008,
        INFO = 009
    }
    public sealed class Logger
    {
        private static Logger _logger;
        private static readonly object _syncLock = new object();
        private EventLog _eventLog;
        private int eventId;
        string logpath = "";

        private Logger()
        {
            if (true)
            {
                _eventLog = new EventLog();
                if (!EventLog.SourceExists("Bifrost Nesna Kommune"))
                {
                    EventLog.CreateEventSource(
                        "Bifrost Nesna Kommune", "Application");
                }
                _eventLog.Source = "Bifrost Nesna Kommune";
                _eventLog.Log = "Application";
            }
        }
        public static Logger GetLogger()
        {
            if (_logger == null)
            {
                lock (_syncLock)
                {
                    if (_logger == null)
                    {
                        _logger = new Logger();
                    }
                }
            }
            return _logger;
        }
        public void LogException(Exception ex)
        {
            if (ex != null)
            {
                log($"Message: {ex.Message}");
                log("Stacktrace:");
                log(ex.StackTrace);
                LogException(ex.InnerException);
            }
        }

        public void log(string message, LogEventType logEventType = LogEventType.INFO)
        {            
            WriteToFile(message, logEventType);
        }


        public void WriteToFile(string message, FileEventObject serviceObject)
        {
            switch (serviceObject.logEventType)
            {
                case LogEventType.CREATE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.CHANGE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.DELETE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.RENAME:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.DEBUG:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.WARNING:
                    _eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId = (int)serviceObject.logEventType);
                    break;
                case LogEventType.ERROR:
                    _eventLog.WriteEntry(message, EventLogEntryType.Error, eventId = (int)serviceObject.logEventType);
                    break;
                default:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)serviceObject.logEventType);
                    break;
            }
        }
        public void WriteToFile(string message, LogEventType logEventType)
        {
            
            switch (logEventType)
            {
                case LogEventType.CREATE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)logEventType);
                    break;
                case LogEventType.CHANGE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)logEventType);
                    break;
                case LogEventType.DELETE:
                    _eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId = (int)logEventType);
                    break;
                case LogEventType.RENAME:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)logEventType);
                    break;
                case LogEventType.DEBUG:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)logEventType);
                    break;
                case LogEventType.WARNING:
                    _eventLog.WriteEntry(message, EventLogEntryType.Warning, eventId = (int)logEventType);
                    break;
                case LogEventType.ERROR:
                    _eventLog.WriteEntry(message, EventLogEntryType.Error, eventId = (int)logEventType);
                    break;
                default:
                    _eventLog.WriteEntry(message, EventLogEntryType.Information, eventId = (int)logEventType);
                    break;
            }
        }
    }
}
