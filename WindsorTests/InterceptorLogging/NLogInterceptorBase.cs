using System;
using System.Diagnostics.CodeAnalysis;
using Castle.DynamicProxy;
using NLog;
using NLog.Fluent;

namespace WindsorTests.InterceptorLogging
{
    public abstract class NLogInterceptorBase<TKey> : TraceInterceptor<TKey>, INLogInterceptor<TKey>
    {
        public abstract ILogger Logger { get; set; }
        public abstract LogLevel EntryLogLevel { get; set; }
        public abstract LogLevel ReturnLogLevel { get; set; }
        public abstract LogLevel ExceptionLogLevel { get; set; }
        protected abstract FormattableString EntryLogMessage(TKey callId, IInvocation invocation);

        protected abstract FormattableString ReturnLogMessage(TKey callId, DateTime startTime, IInvocation invocation,
            object value);

        protected abstract FormattableString ExceptionLogMessage(TKey callId, DateTime startTime, IInvocation invocation,
            Exception ex);

        protected string MemberName(IInvocation invocation) => invocation.Method.Name;
        protected string FilePath(IInvocation invocation) => null;
        protected int LineNumber(IInvocation invocation) => 0;

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override void ReactOnEntry(TKey id, IInvocation invocation)
        {
            if (!Logger.IsEnabled(EntryLogLevel))
                return;
            var fmtString = EntryLogMessage(id, invocation);
            if (fmtString == null)
                return;
            Logger.Log(EntryLogLevel)
                .Message(fmtString.Format, fmtString.GetArguments())
                .Property("callId", id)
                .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override void ReactOnReturn(TKey id, DateTime startTime, IInvocation invocation, object value)
        {
            if (!Logger.IsEnabled(ReturnLogLevel))
                return;
            var fmtString = ReturnLogMessage(id, startTime, invocation, value);
            if (fmtString == null)
                return;
            Logger.Log(ReturnLogLevel)
                .Message(fmtString.Format, fmtString.GetArguments())
                .Property("callId", id)
                .Property("startTime", startTime)
                .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override bool ReactException(TKey id, DateTime startTime, IInvocation invocation, Exception ex)
        {
            if (!Logger.IsEnabled(ExceptionLogLevel))
                return false;
            var fmtString = ExceptionLogMessage(id, startTime, invocation, ex);
            if (fmtString == null)
                return false;
            Logger.Log(ReturnLogLevel)
                .Message(fmtString.Format, fmtString.GetArguments())
                .Exception(ex)
                .Property("callId", id)
                .Property("startTime", startTime)
                .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
            return false;
        }
    }
}