using System;
using System.Collections.Generic;
using System.Linq;
using Castle.DynamicProxy;

namespace WindsorTests.InterceptorLogging
{
    public abstract class DefaultNLogInterceptorBase<TKey> : NLogInterceptorBase<TKey>
    {
        protected override FormattableString EntryLogMessage(TKey callId, IInvocation invocation)
        {
            if (ReferenceEquals(invocation, null))
                throw new ArgumentNullException(nameof(invocation));
            return $"[{callId}] {invocation.Method.Name}({string.Join(", ", invocation.Arguments)})";
        }

        protected override FormattableString ReturnLogMessage(TKey callId, DateTime startTime, IInvocation invocation,
            object value)
        {
            if (ReferenceEquals(invocation, null))
                throw new ArgumentNullException(nameof(invocation));
            return $"[{callId}] {invocation.Method.Name}(...) = {value}";
        }

        protected override FormattableString ExceptionLogMessage(TKey callId, DateTime startTime, IInvocation invocation,
            Exception ex)
        {
            if (ReferenceEquals(invocation, null))
                throw new ArgumentNullException(nameof(invocation));
            return
                $"[{callId}] {invocation.Method.Name}({string.Join(", ", invocation.Arguments)}): {ExtractMessage(ex)}";
        }

        #region Helpers for string-conversions

        protected static IEnumerable<Exception> InnerChain(Exception ex)
        {
            while (ex != null)
            {
                yield return ex;
                ex = ex.InnerException;
            }
        }

        protected static FormattableString ExtractAggregateException(AggregateException ex)
        {
            if (ReferenceEquals(ex, null))
                throw new ArgumentNullException(nameof(ex));
            return $"{ex.Message} ({string.Join(", ", ex.InnerExceptions.Select(ExtractMessage))})";
        }

        protected static FormattableString ExtractNormalException(Exception ex)
            => $"{string.Join(" -> ", InnerChain(ex).Select(x => x.Message))}";

        protected static FormattableString ExtractMessage(Exception ex)
        {
            var exception = ex as AggregateException;
            if (exception != null)
                return ExtractAggregateException(exception);
            return ExtractNormalException(ex);
        }

        #endregion
    }
}