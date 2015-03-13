﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using log4net.Core;
using Newtonsoft.Json;

namespace Log4EventHub {
    public class LoggingEventSerializer
    {

        private readonly Process _currentProcess = Process.GetCurrentProcess();

        public string ApplicationName { get; set; }

        public string SerializeLoggingEvents(IEnumerable<LoggingEvent> loggingEvents)
        {
            var sb = new StringBuilder();

            foreach (var loggingEvent in loggingEvents)
            {
                sb.AppendLine(SerializeLoggingEvent(loggingEvent));
            }

            return sb.ToString();
        }

        private string SerializeLoggingEvent(LoggingEvent loggingEvent)
        {
            dynamic payload = new ExpandoObject();
            payload.level = loggingEvent.Level.DisplayName;
            payload.time = loggingEvent.TimeStamp.ToString("o");
            payload.machine = Environment.MachineName;
            payload.process = _currentProcess.ProcessName;
            payload.thread = loggingEvent.ThreadName;
            payload.message = loggingEvent.RenderedMessage;
            payload.logger = loggingEvent.LoggerName;
            if (!string.IsNullOrEmpty(ApplicationName))
                payload.application = ApplicationName;

            //If any custom properties exist, add them to the dynamic object
            //i.e. if someone added loggingEvent.Properties["xx:traceId"] = "helloWorld"
            foreach (var key in loggingEvent.Properties.GetKeys())
            {
                ((IDictionary<string, object>)payload)[RemoveSpecialCharacters(key)] = loggingEvent.Properties[key];
            }

            var exception = loggingEvent.ExceptionObject;
            if (exception != null)
            {
                payload.exception = new ExpandoObject();
                payload.exception.message = exception.Message;
                payload.exception.type = exception.GetType().Name;
                payload.exception.stackTrace = exception.StackTrace;
                if (exception.InnerException != null)
                {
                    payload.exception.innerException = new ExpandoObject();
                    payload.exception.innerException.message = exception.InnerException.Message;
                    payload.exception.innerException.type = exception.InnerException.GetType().Name;
                    payload.exception.innerException.stackTrace = exception.InnerException.StackTrace;
                }
            }

            return JsonConvert.SerializeObject(payload, Formatting.None);
        }

        private static string RemoveSpecialCharacters(string str)
        {
            var sb = new StringBuilder(str.Length);
            foreach (var c in str.Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_'))
            {
                sb.Append(c);
            }
            return sb.ToString();
        }
    
    }
}
