using System;
using Castle.DynamicProxy;

namespace WindsorTests.InterceptorLogging.Interface
{
    public interface IDefaultNLogInterceptor<TKey> : INLogInterceptor
    {
        Func<TKey, IInvocation, FormattableString> EntryFormattableString { get; set; }
        Func<TKey, DateTime, IInvocation, object, FormattableString> ReturnFormattableString { get; set; }
        Func<TKey, DateTime, IInvocation, Exception, FormattableString> ExceptionFormattableString { get; set; }
    }
}