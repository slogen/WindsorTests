using System;
using Castle.DynamicProxy;

namespace WindsorTests.InterceptorLogging
{
    public interface IDefaultNLogInterceptor<TKey> : INLogInterceptor<TKey>
    {
        Func<TKey, IInvocation, FormattableString> EntryFormattableString { get; set; }
        Func<TKey, DateTime, IInvocation, object, FormattableString> ReturnFormattableString { get; set; }
        Func<TKey, DateTime, IInvocation, Exception, FormattableString> ExceptionFormattableString { get; set; }
    }
}