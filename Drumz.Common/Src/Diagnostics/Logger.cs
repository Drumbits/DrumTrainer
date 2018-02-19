using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drumz.Common.Diagnostics
{
    public class Logger
    {
        public delegate void LogEventHandler(Level level, string message);

        public enum Level
        {
            Debug,
            Info,
            Warning,
            Error
        };

        public static readonly Logger Instance = new Logger();

        private Logger()
        {
        }

        public event LogEventHandler Log;

        public void Tell(Level level, string message)
        {
            if (Log != null) Log(level, message);
        }
        private static readonly object mutex = new object();
        public static void TellF(Level level, string messageFormat, params object[] args)
        {
            lock (mutex)
            {
                Instance.Tell(level, string.Format(messageFormat, args));

            }
        }
    }
}
