using Castle.DynamicProxy;
using NLog;

namespace WindsorTests.InterceptorLogging
{
    public interface INLogInterceptor : IInterceptor
    {
        ILogger Logger { get; set; }
        LogLevel EntryLogLevel { get; set; }
        LogLevel ReturnLogLevel { get; set; }
        LogLevel ExceptionLogLevel { get; set; }
    }
}