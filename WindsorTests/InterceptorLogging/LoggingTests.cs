using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using FluentAssertions;
using NLog;
using NLog.Config;
using NLog.Fluent;
using NLog.Targets;
using NUnit.Framework;

namespace WindsorTests.InterceptorLogging
{
    public abstract class TraceInterceptor : IInterceptor
    {
        protected abstract long MakeNextCallId();

        protected abstract void ReactOnEntry(long id, IInvocation invocation);
        protected abstract void ReactOnReturn(long id, DateTime startTime, IInvocation invocation, object value);
        protected abstract bool ReactException(long id, DateTime startTime, IInvocation invocation, Exception ex);
        public void Intercept(IInvocation invocation)
        {
            var callId = MakeNextCallId();
            ReactOnEntry(callId, invocation);
            var startTime = DateTime.UtcNow;
            try
            {
                invocation.Proceed();
            }
            catch (Exception ex) // Allow avoiding getting on the stack
                when (ReactException(callId, startTime, invocation, ex))
            {
                throw; // If ReactException returns true, then we rethrow and get on the stack
            }
            if (invocation.ReturnValue is Task)
            {
                var task = (Task) invocation.ReturnValue;
                task.ContinueWith(t =>
                {
                    if (t.IsFaulted)
                        ReactException(callId, startTime, invocation, t.Exception);
                    else if (t.IsCanceled)
                        ReactException(callId, startTime, invocation, new TaskCanceledException(task));
                    else
                        ReactOnReturn(callId, startTime, invocation, ((dynamic)t).Result);
                },  default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
            } else 
                ReactOnReturn(callId, startTime, invocation, invocation.ReturnValue);
        }
    }

    public interface ISequence<out TKey>
    {
        TKey Next();
    }
    public class NlogInterceptor : TraceInterceptor
    {
        #region Logger and its default
        private ILogger _logger;
        public ILogger Logger
        {
            get { return _logger ?? DefaultLogger; }
            set { _logger = value; }
        }
        public static ILogger DefaultLogger = null;
        #endregion
        #region LogLevels and their defaults
        private static LogLevel _defaultEntryLogLevel;
        public static LogLevel DefaultLogLevel = LogLevel.Trace;

        private LogLevel _entryLogLevel;
        public LogLevel EntryLogLevel
        {
            get { return _entryLogLevel ?? DefaultEntryLogLevel; }
            set { _entryLogLevel = value; }
        }
        public static LogLevel DefaultEntryLogLevel
        {
            get { return _defaultEntryLogLevel ?? DefaultLogLevel; }
            set { _defaultEntryLogLevel = value; }
        }

        private LogLevel _returnLogLevel;
        public LogLevel ReturnLogLevel
        {
            get { return _returnLogLevel ?? DefaultReturnLogLevel; }
            set { _returnLogLevel = value; }
        }
        private static LogLevel _defaultReturnLogLevel;
        public static LogLevel DefaultReturnLogLevel
        {
            get { return _defaultReturnLogLevel ?? DefaultLogLevel; }
            set { _defaultReturnLogLevel = value; }
        }

        private LogLevel _exceptionLogLevel;
        public LogLevel ExceptionLogLevel
        {
            get { return _exceptionLogLevel ?? DefaultExceptionLogLevel; }
            set { _exceptionLogLevel = value; }
        }
        private static LogLevel _defaultExceptionLogLevel = LogLevel.Error;
        public static LogLevel DefaultExceptionLogLevel
        {
            get { return _defaultExceptionLogLevel ?? DefaultLogLevel; }
            set { _defaultExceptionLogLevel = value; }
        }
        #endregion
        #region Formatting and their defaults
        private Func<long, IInvocation, FormattableString> _entryFormattableString;
        public Func<long, IInvocation, FormattableString> EntryFormattableString
        {
            get { return _entryFormattableString ?? DefaultEntryFormattableString; }
            set { _entryFormattableString = value; }
        }
        public static Func<long, IInvocation, FormattableString> DefaultEntryFormattableString { get; set; }
            = _makeDefaultEntryFormattableString;
        private static FormattableString _makeDefaultEntryFormattableString(long callId, IInvocation invocation)
        {
            return $"[{callId}] {invocation.Method.Name}({string.Join(", ", invocation.Arguments)})";
        }

        private Func<long, DateTime, IInvocation, object, FormattableString> _returnFormattableString;
        public Func<long, DateTime, IInvocation, object, FormattableString> ReturnFormattableString
        {
            get { return _returnFormattableString ?? DefaultReturnFormattableString; }
            set { _returnFormattableString = value; }
        }
        public static Func<long, DateTime, IInvocation, object, FormattableString> DefaultReturnFormattableString { get; set; }
            = _makeDefaultReturnFormattableString;
        private static FormattableString _makeDefaultReturnFormattableString(
            long callId, DateTime startTime, IInvocation invocation, object value)
        {
            return $"[{callId}] {invocation.Method.Name}(...) = {value}";
        }

        private Func<long, DateTime, IInvocation, Exception, FormattableString> _exceptionFormattableString;
        public Func<long, DateTime, IInvocation, Exception, FormattableString> ExceptionFormattableString
        {
            get { return _exceptionFormattableString ?? DefaultExceptionFormattableString; }
            set { _exceptionFormattableString = value; }
        }
        public static Func<long, DateTime, IInvocation, Exception, FormattableString> DefaultExceptionFormattableString { get; set; }
            = _makeDefaultExceptionFormattableString;

        public static Func<Exception, FormattableString> ExtractMessage { get; set; } = _defaultExtractMessage;

        private static IEnumerable<Exception> InnerChain(Exception ex)
        {
            while (ex != null)
            {
                yield return ex;
                ex = ex.InnerException;
            }
        }

        private static FormattableString _extractAggregateException(AggregateException ex)
        {
            return $"{ex.Message} ({string.Join(", ", ex.InnerExceptions.Select(_defaultExtractMessage))})";
        }
        private static FormattableString _extractNormalException(Exception ex)
            => $"{string.Join(" -> ", InnerChain(ex).Select(x => x.Message))}";
        private static FormattableString _defaultExtractMessage(Exception ex)
        {
            if (ex is AggregateException)
                return _extractAggregateException((AggregateException) ex);
            return _extractNormalException(ex);
        }
        
        private static FormattableString _makeDefaultExceptionFormattableString(long callId, DateTime startTime, IInvocation invocation, Exception ex)
        {
            return $"[{callId}] {invocation.Method.Name}({string.Join(", ", invocation.Arguments)}): {ExtractMessage?.Invoke(ex)}";
        }
        #endregion

        public static Func<long> DefaultNextCallId { get; set; } = _makeDefaultNextCallId;
        private static long _nextSharedCallId = DateTime.UtcNow.Ticks;
        private static long _makeDefaultNextCallId() => Interlocked.Increment(ref _nextSharedCallId);
        private Func<long> _nextCallId;

        protected override long MakeNextCallId() => NextCallId();

        public Func<long> NextCallId
        {
            get { return _nextCallId ?? DefaultNextCallId; }
            set { _nextCallId = value; }
        }

        public NlogInterceptor(
            ILogger logger = null,
            LogLevel entryLogLevel = null,
            LogLevel returnLogLevel = null,
            LogLevel exceptionLogLevel = null,
            Func<long, IInvocation, FormattableString> entryLogMessage = null,
            Func<long, DateTime, IInvocation, object, FormattableString> returnLogMessage = null,
            Func<long, DateTime, IInvocation, Exception, FormattableString> exceptionLogMessage = null,
            Func<long>  nextCallId = null
        )
        {
            _logger = logger;
            _entryLogLevel = entryLogLevel;
            _returnLogLevel = returnLogLevel;
            _exceptionLogLevel = exceptionLogLevel;
            _entryFormattableString = entryLogMessage;
            _returnFormattableString = returnLogMessage;
            _exceptionFormattableString = exceptionLogMessage;
            _nextCallId = nextCallId;
        }

        protected string MemberName(IInvocation invocation) => invocation.Method.Name;
        protected string FilePath(IInvocation invocation) => null;
        protected int LineNumber(IInvocation invocation) => 0;

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override void ReactOnEntry(long id, IInvocation invocation)
        {
            if (Logger.IsEnabled(EntryLogLevel))
            {
                var fmtString = EntryFormattableString?.Invoke(id, invocation);
                if (fmtString == null)
                    return;
                Logger.Log(EntryLogLevel)
                    .Message(fmtString.Format, fmtString.GetArguments())
                    .Property("callId", id)
                    .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
            }
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override void ReactOnReturn(long id, DateTime startTime, IInvocation invocation, object value)
        {
            if (Logger.IsEnabled(ReturnLogLevel))
            {
                var fmtString = ReturnFormattableString?.Invoke(id, startTime, invocation, value);
                if (fmtString == null)
                    return;
                Logger.Log(ReturnLogLevel)
                    .Message(fmtString.Format, fmtString.GetArguments())
                    .Property("callId", id)
                    .Property("startTime", startTime)
                    .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
            }
        }

        [SuppressMessage("ReSharper", "ExplicitCallerInfoArgument")]
        protected override bool ReactException(long id, DateTime startTime, IInvocation invocation, Exception ex)
        {
            if (Logger.IsEnabled(ExceptionLogLevel))
            {
                var fmtString = ExceptionFormattableString?.Invoke(id, startTime, invocation, ex);
                if (fmtString == null)
                    return false;
                Logger.Log(ReturnLogLevel)
                    .Message(fmtString.Format, fmtString.GetArguments())
                    .Exception(ex)
                    .Property("callId", id)
                    .Property("startTime", startTime)
                    .Write(MemberName(invocation), FilePath(invocation), LineNumber(invocation));
            }
            return false;
        }
    }
    public class LoggingTests
    {
        public class Foo
        {
            private long cnt;
            public CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
            public virtual async Task<string> FAsync(TimeSpan waitTime)
            {
                var v = await GAsync(Interlocked.Increment(ref cnt)).ConfigureAwait(false);
                await Task.Delay(waitTime).ConfigureAwait(false);
                return v;
            }

            public virtual async Task<string> GAsync(long i)
            {
                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                await Task.Yield();
                if ( i == 2 )
                    throw new KeyNotFoundException($"GAsync({i}) throws");
                return i.ToString();
            }
            public virtual string F(TimeSpan waitTime)
            {
                var v = G(Interlocked.Increment(ref cnt));
                Thread.Sleep(waitTime);
                return v;
            }

            public virtual string G(long i)
            {
                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                if (i == 2)
                    throw new KeyNotFoundException($"G({i}) throws");
                return i.ToString();
            }
        }

        public class TestTarget : Target
        {
            public ICollection<LogEventInfo> LogEvents = new List<LogEventInfo>();
            public TestTarget() { }
            protected override void Write(LogEventInfo logEvent)
            {
                LogEvents.Add(logEvent);
            }
        }


        private CancellationToken CancellationToken => default(CancellationToken);

        public class Counter
        {
            private long _value;
            public long Value { get; }
            public Counter(long value = 0) { _value = value; }
            public long Next() => Interlocked.Increment(ref _value);
        }

        [Test]
        public async Task TestAspectOrientedLoggingAsync()
        {
            var testTarget = new TestTarget();
            var logCfg = new LoggingConfiguration();
            logCfg.AddTarget("testtarget", testTarget);
            logCfg.AddRule(LogLevel.Trace, LogLevel.Fatal, testTarget);
            var logger = new LogFactory(logCfg).GetLogger("testLogger");
            var counter = new Counter();
            Func<long> nextId = counter.Next;

            using (var cw = new WindsorContainer())
            {
                cw.Register(
                    Component.For<ILogger>().Instance(logger),
                    Component.For<Counter>(),
                    Component.For<NlogInterceptor>()
                        .DependsOn(Dependency.OnValue("nextCallId", nextId)),
                    Component.For<Foo>().Interceptors<NlogInterceptor>());
                var foo = cw.Resolve<Foo>();
                await foo.FAsync(TimeSpan.FromSeconds(0.01)).ConfigureAwait(false);
                Func<Task> a =
                    async () => await foo.FAsync(TimeSpan.FromSeconds(.05)).ConfigureAwait(false);
                a.ShouldThrow<KeyNotFoundException>().And.Message.Should().Be("GAsync(2) throws");
                await foo.FAsync(TimeSpan.FromSeconds(.05)).ConfigureAwait(false);
                foo.CancellationTokenSource.Cancel();
                a = async () => await foo.FAsync(TimeSpan.FromSeconds(.05)).ConfigureAwait(false);
                a.ShouldThrow<TaskCanceledException>();
            }
            var expect = new[]
            {
                "[1] FAsync(00:00:00.0100000)",
                "[2] GAsync(1)",
                "[2] GAsync(...) = 1",
                "[1] FAsync(...) = 1",
                "[3] FAsync(00:00:00.0500000)",
                "[4] GAsync(2)",
                "[4] GAsync(2): One or more errors occurred. (GAsync(2) throws)",
                "[3] FAsync(00:00:00.0500000): One or more errors occurred. (GAsync(2) throws)",
                "[5] FAsync(00:00:00.0500000)",
                "[6] GAsync(3)",
                "[6] GAsync(...) = 3",
                "[5] FAsync(...) = 3",
                "[7] FAsync(00:00:00.0500000)",
                "[8] GAsync(4)",
                "[8] GAsync(4): A task was canceled.",
                "[7] FAsync(00:00:00.0500000): A task was canceled.",
            }.OrderBy(x => x).ToList();
            // We cannot rely strictly on the ordering of logging, since the logging async results can 
            // be reordered. So we sort the texts for efficient comparison
            var actual = testTarget.LogEvents
                .Select(x => x.FormattedMessage)
                .OrderBy(x => x)
                .ToList();
            Console.WriteLine("----- EXPECTED -----{0}{1}{0}----- ACTUAL -----{0}{2}{0}",
                Environment.NewLine, 
                string.Join(Environment.NewLine, expect), 
                string.Join(Environment.NewLine, actual));
            actual.ShouldBeEquivalentTo(expect, cfg => cfg.ExcludingMissingMembers().WithStrictOrdering());
        }
        [Test]
        public void TestAspectOrientedLogging()
        {
            var testTarget = new TestTarget();
            var logCfg = new LoggingConfiguration();
            logCfg.AddTarget("testtarget", testTarget);
            logCfg.AddRule(LogLevel.Trace, LogLevel.Fatal, testTarget);
            var logger = new LogFactory(logCfg).GetLogger("testLogger");
            var counter = new Counter();
            Func<long> nextId = counter.Next;

            using (var cw = new WindsorContainer())
            {
                cw.Register(
                    Component.For<ILogger>().Instance(logger),
                    Component.For<Counter>(),
                    Component.For<NlogInterceptor>()
                        .DependsOn(Dependency.OnValue("nextCallId", nextId)),
                    Component.For<Foo>().Interceptors<NlogInterceptor>());
                var foo = cw.Resolve<Foo>();
                foo.F(TimeSpan.FromSeconds(0.01));
                Action a = () => foo.F(TimeSpan.FromSeconds(.05));
                a.ShouldThrow<KeyNotFoundException>().And.Message.Should().Be("G(2) throws");
                foo.F(TimeSpan.FromSeconds(.05));
                foo.CancellationTokenSource.Cancel();
                a = () => foo.F(TimeSpan.FromSeconds(.05));
                a.ShouldThrow<OperationCanceledException>();
            }
            var expect = new[]
            {
                "[1] F(00:00:00.0100000)",
                "[2] G(1)",
                "[2] G(...) = 1",
                "[1] F(...) = 1",
                "[3] F(00:00:00.0500000)",
                "[4] G(2)",
                "[4] G(2): G(2) throws",
                "[3] F(00:00:00.0500000): G(2) throws",
                "[5] F(00:00:00.0500000)",
                "[6] G(3)",
                "[6] G(...) = 3",
                "[5] F(...) = 3",
                "[7] F(00:00:00.0500000)",
                "[8] G(4)",
                "[8] G(4): The operation was canceled.",
                "[7] F(00:00:00.0500000): The operation was canceled.",
            }.Select(msg => new { FormattedMessage = msg }).ToList();
            testTarget.LogEvents
                .ShouldBeEquivalentTo(expect, cfg => cfg.ExcludingMissingMembers().WithStrictOrdering());
        }
    }
}
