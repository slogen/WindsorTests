namespace WindsorTests.InterceptorLogging.Interface
{
    public interface IFormatReady
    {
        double Preference { get; }
        object Format();
    }
}