using System.Threading;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public class DefaultDictionaryArgumentFormatter : DictionaryArgumentFormatter
    {
        public DefaultDictionaryArgumentFormatter()
        {
            Add<CancellationToken>(ct => $"CancellationToken(can={ct.CanBeCanceled}, is={ct.IsCancellationRequested})");
            Add<Thread>(t => $"Thread(id={t.ManagedThreadId}, name={t.Name})");
        }
    }
}