using NLog;

namespace WindsorTests.InterceptorLogging
{
    public class NLogInterceptorDefaults
    {
        public static ILogger Logger = LogManager.GetLogger("LogInterceptor");

        public static LogLevel LogLevel = LogLevel.Trace;

        private static LogLevel _defaultEntryLogLevel;

        private static LogLevel _defaultReturnLogLevel;

        private static LogLevel _defaultExceptionLogLevel = LogLevel.Error;

        public static LogLevel EntryLogLevel
        {
            get { return _defaultEntryLogLevel ?? LogLevel; }
            set { _defaultEntryLogLevel = value; }
        }

        public static LogLevel ReturnLogLevel
        {
            get { return _defaultReturnLogLevel ?? LogLevel; }
            set { _defaultReturnLogLevel = value; }
        }

        public static LogLevel ExceptionLogLevel
        {
            get { return _defaultExceptionLogLevel ?? LogLevel; }
            set { _defaultExceptionLogLevel = value; }
        }
    }
}