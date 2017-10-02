using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public struct NoFormatReady : IFormatReady
    {
        public double Preference => double.NaN;
        private readonly object _value;

        public NoFormatReady(object value)
        {
            _value = value;
        }

        public object Format() => _value;

        public bool Equals(NoFormatReady other)
        {
            return Equals(_value, other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is NoFormatReady && Equals((NoFormatReady) obj);
        }

        public override int GetHashCode()
        {
            return _value?.GetHashCode() ?? 0;
        }

        public static bool operator ==(NoFormatReady left, NoFormatReady right)
            => left.Equals(right);

        public static bool operator !=(NoFormatReady left, NoFormatReady right) => !(left == right);
    }
}