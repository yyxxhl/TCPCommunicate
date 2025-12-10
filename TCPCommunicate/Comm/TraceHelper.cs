using System;
using System.Diagnostics;

namespace TCPCommunicate.Comm
{
    internal class TraceHelper
    {
        private static TraceHelper _traceHelper;

        private static DateTime _dateTime;
        public Action<string, int, Exception> ActionLog;

        public static TraceHelper Instance
        {
            get
            {
                if (_traceHelper == null || DateTime.Now.Date > _dateTime)
                {
                    _dateTime = DateTime.Now.Date;
                    Trace.Listeners.Clear();
                    Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(Console.Out));
                }
                return _traceHelper;
            }
        }

        static TraceHelper()
        {
            _dateTime = DateTime.Now.Date.AddDays(-1);
            _traceHelper = new TraceHelper();
            Trace.AutoFlush = true;
            Trace.IndentSize = 0;
        }

        public void Error(string message, Exception ex = null)
        {
            Log(message, MessageType.Error, ex);
        }

        public void Warning(string message, Exception ex = null)
        {
            Log(message, MessageType.Warning, ex);
        }

        public void Info(string message, Exception ex = null)
        {
            Log(message, MessageType.Info, ex);
        }

        private void Log(string message, MessageType type, Exception ex = null)
        {
            if (ex != null)
            {
                type = MessageType.Error;
            }
            if (message.Contains("心跳包"))
            {
                return;
            }
            ActionLog?.Invoke(message, (int)type, ex);
        }
    }

    internal enum MessageType
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}