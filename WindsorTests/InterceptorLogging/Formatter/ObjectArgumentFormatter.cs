using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public class ObjectArgumentFormatter : IArgumentFormatter
    {
        public IFormatReady Prepare(object value) => new ObjectReady(value);

        private struct ObjectReady : IFormatReady
        {
            private readonly object _object;

            public ObjectReady(object obj)
            {
                _object = obj;
            }

            public double Preference => double.PositiveInfinity;
            public object Format() => _object;
        }
    }
}