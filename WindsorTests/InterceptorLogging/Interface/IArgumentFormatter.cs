namespace WindsorTests.InterceptorLogging.Interface
{
    public interface IArgumentFormatter
    {
        IFormatReady Prepare(object value);
    }
}