using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Castle.Windsor.Diagnostics.Extensions;

namespace WindsorTests.InterceptorLogging
{
    public interface IFormatReady
    {
        double Preference { get; }
        Object Format();
    }

    public struct NoFormatReady: IFormatReady
    {
        public double Preference => Double.NaN;
        private object _object;
        public NoFormatReady(object o)
        {
            _object = o;
        }

        public object Format() => _object;
    }

    public interface IArgumentFormatter
    {
        IFormatReady For(object o);
    }
    public class ObjectArgumentFormatter: IArgumentFormatter
    {
        private struct ObjectReady: IFormatReady
        {
            private readonly object _object;

            public ObjectReady(object obj)
            {
                _object = obj;
            }

            public double Preference => double.PositiveInfinity;
            public object Format() => _object;
        }

        public IFormatReady For(object o) => new ObjectReady(o);
    }

    public abstract class DictionaryArgumentFormatter : IArgumentFormatter
    {
        protected class Spec
        {
            public readonly Func<object, object> Formatter;
            public readonly double? Preference;

            public Spec(Func<object, object> formatter, double? preference)
            {
                Formatter = formatter;
                Preference = preference;
            }
            public Spec(Spec other): this(other.Formatter, other.Preference) { }

        }
        struct Ready : IFormatReady
        {
            private readonly DictionaryArgumentFormatter _parent;
            private readonly object _object;
            private readonly Spec _spec;
            public Ready(DictionaryArgumentFormatter parent, object o, Spec spec)
            {
                _parent = parent;
                _object = o;
                _spec = spec;
            }

            public double Preference => _spec?.Preference ?? _parent.DefaultPreference;
            public object Format() => _spec?.Formatter(_object);
        }
        /// <summary>
        /// Contains the specification registered by user
        /// </summary>
        protected IDictionary<Type, Spec> TypeFormatters { get; }
        /// <summary>
        /// Contains a lookup from types to spec, which have <see cref="Spec.Preference"/> of <see cref="double.NaN"/> converted to <see cref="DefaultPreference"/> 
        /// </summary>
        /// <remarks>Cleaned whenever new types are registered in <see cref="TypeFormatters"/> or <see cref="DefaultPreference"/> changes</remarks>
        private ConditionalWeakTable<Type, Spec> _lookupTable;
        protected DictionaryArgumentFormatter(IDictionary<Type, Spec> typeFormatters = null)
        {
            TypeFormatters = typeFormatters ?? new Dictionary<Type, Spec>();
            _lookupTable = new ConditionalWeakTable<Type, Spec>();
        }

        public void Add(Type type, Func<object, object> formatter, double? preference = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            lock (TypeFormatters)
                try
                {
                    TypeFormatters.Add(type, new Spec(formatter, preference));
                }
                finally
                {
                    ClearLookupTable();
                }
        }

        protected void ClearLookupTable()
        {
            lock ( TypeFormatters)
                _lookupTable = new ConditionalWeakTable<Type, Spec>();
        }
        public void Add<TSource, TResult>(Func<TSource, TResult> formatter, double? preference = null)
        {
            if ( formatter == null )
                throw new ArgumentNullException(nameof(formatter));
            lock ( TypeFormatters )
                Add(typeof(TSource), src => formatter((TSource)src), preference);
        }
        public void Add<TSource>(Func<TSource, object> formatter, double? preference = null)
            => Add(typeof(TSource), src => formatter((TSource)src), preference);

        private static IEnumerable<Type> BaseTypes(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }
        protected virtual IEnumerable<Type> SearchTypes(Type type)
            => BaseTypes(type).Concat(type.GetInterfaces());

        protected Spec TryType(Type type)
        {
            Contract.Requires(!ReferenceEquals(type, null));
            Contract.Requires(Monitor.IsEntered(_lookupTable));
            return _lookupTable.GetValue(type,
                t =>
                {
                    Spec formatter;
                    TypeFormatters.TryGetValue(t, out formatter);
                    return formatter;
                });
        }

        protected Spec FindType(Type type)
        {
            Contract.Requires(!ReferenceEquals(type, null));
            Contract.Requires(Monitor.IsEntered(_lookupTable));
            var best = SearchTypes(type)
                .Select(TryType)
                .Where(x => !ReferenceEquals(x, null))
                .Where(x => !double.IsNaN(x.Preference ?? 0))
                .OrderBy(x => x.Preference ?? DefaultPreference)
                .FirstOrDefault();
            return best;
        }

        protected Spec Lookup(Type type)
        {
            lock (_lookupTable)
                return _lookupTable.GetValue(type, FindType);
        }

        private double _defaultPreference = -1;

        public double DefaultPreference
        {
            get { return _defaultPreference; }
            set
            {
                _defaultPreference = value;
                ClearLookupTable();
            }
        }

        public IFormatReady For(object o)
        {
            if (ReferenceEquals(o, null))
                return new NoFormatReady(null);
            var got = Lookup(o.GetType());
            if ( ReferenceEquals(got, null) )
                return new NoFormatReady(o);
            return new Ready(this, o, got);
        }
    }


    public class DefaultDictionaryArgumentFormatter : DictionaryArgumentFormatter
    {
        public DefaultDictionaryArgumentFormatter() : base()
        {
            Add<CancellationToken>(ct => $"CancellationToken(can={ct.CanBeCanceled}, is={ct.IsCancellationRequested})");
            Add<Thread>(t => $"Thread(id={t.ManagedThreadId}, name={t.Name})");
        }
    }

    public class SequenceFormatter : IArgumentFormatter
    {
        private readonly IArgumentFormatter _innerFormatter;

        public SequenceFormatter(IArgumentFormatter innerFormatter)
        {
            _innerFormatter = innerFormatter;
        }

        public double DefaultPreference { get; set; } = -.1;
        public int PrefixPrintCount { get; } = 10;
        public double Preference(object o)
        {
            return (o is IEnumerable) ? DefaultPreference : double.NaN;
        }

        protected class PartPrint
        {
            private readonly int _count;
            private IEnumerable _parts;
            public PartPrint(int count, IEnumerable parts)
            {
                _count = count;
                _parts = parts;
            }

            public override string ToString()
            {
                return $"#{_count}({string.Join(", ", _parts)}";
            }
        }

        protected IEnumerable<object> Take(ICollection c, int count)
        {
            var it = c.GetEnumerator();
            try
            {
                for (var i = 0; i < count && it.MoveNext(); ++i)
                    yield return it.Current;
            }
            finally
            {
                (it as IDisposable)?.Dispose();
            }
        }

        protected PartPrint Parts(ICollection c)
        {
            var total = c.Count;
            return total <= PrefixPrintCount
                ? new PartPrint(total, c)
                : new PartPrint(total, Take(c, PrefixPrintCount)
                    .Select(o => _innerFormatter.For(o).Format())
                    .Concat(new[] {"..."}));
        }

        struct Ready : IFormatReady
        {
            private readonly SequenceFormatter _parent;
            private readonly ICollection _collection;

            public Ready(SequenceFormatter parent, ICollection collection)
            {
                Contract.Requires(!ReferenceEquals(parent, null));
                Contract.Requires(!ReferenceEquals(collection, null));
                _parent = parent;
                _collection = collection;
            }

            public double Preference => _parent.DefaultPreference;
            public object Format() => _parent.Parts(_collection);
        }

        public IFormatReady For(object o)
        {
            if (o == null)
                return null;
            var c = o as ICollection;
            if (ReferenceEquals(c, null))
                return _innerFormatter.For(o);
            return new Ready(this, c);
        }
    }

    public class CombinedFormatter : IArgumentFormatter
    {
        private readonly IArgumentFormatter[] _formatters;

        public CombinedFormatter(params IArgumentFormatter[] formatters)
        {
            Contract.Requires(!ReferenceEquals(formatters, null));
            Contract.Requires(formatters.Length > 0);
            _formatters = formatters;
        }

        public IFormatReady For(object o)
        {
            return _formatters.Select(f => f.For(o))
                .Where(x => !double.IsNaN(x.Preference))
                .OrderBy(x => x.Preference)
                .Concat(new IFormatReady[]{ new NoFormatReady(o) })
                .First();
        }
    }

    public class ArgumentFormatterWindsorInstaller: IWindsorInstaller
    {
        public static ArgumentFormatterWindsorInstaller Default = new ArgumentFormatterWindsorInstaller();

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Kernel.Resolver.AddSubResolver(new CollectionResolver(container.Kernel));

            container.Register(
                Classes.FromAssemblyInThisApplication()
                    .BasedOn(typeof(IArgumentFormatter))
                    .If(x => !x.IsAbstract && !x.IsInterface)
                    .WithServiceSelf()
                    .WithServiceFromInterface(typeof(IArgumentFormatter))
                    .Configure(r => r.IsFallback()));
            container.Register(
                Component.For<IArgumentFormatter>()
                    .UsingFactoryMethod(kernel =>
                        new CombinedFormatter(kernel.ResolveAll<IArgumentFormatter>())));
        }
    }
}
