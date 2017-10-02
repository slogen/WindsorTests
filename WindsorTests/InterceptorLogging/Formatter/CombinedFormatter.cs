using System.Diagnostics.Contracts;
using System.Linq;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public class CombinedFormatter : IArgumentFormatter
    {
        private readonly IArgumentFormatter[] _formatters;

        public CombinedFormatter(params IArgumentFormatter[] formatters)
        {
            Contract.Requires(!ReferenceEquals(formatters, null));
            Contract.Requires(formatters.Length > 0);
            _formatters = formatters;
        }

        public IFormatReady Prepare(object value)
        {
            return _formatters.Select(f => f.Prepare(value))
                .Where(x => !double.IsNaN(x.Preference))
                .OrderBy(x => x.Preference)
                .Concat(new IFormatReady[] {new NoFormatReady(value)})
                .First();
        }
    }
}