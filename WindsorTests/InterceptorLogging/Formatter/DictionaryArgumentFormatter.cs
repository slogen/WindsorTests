using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public abstract class DictionaryArgumentFormatter : IArgumentFormatter
    {
        private double _defaultPreference = -1;

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

        /// <summary>
        /// Contains the specification registered by user
        /// </summary>
        protected IDictionary<Type, Spec> TypeFormatters { get; }

        public double DefaultPreference
        {
            get { return _defaultPreference; }
            set
            {
                _defaultPreference = value;
                ClearLookupTable();
            }
        }

        public IFormatReady Prepare(object value)
        {
            if (ReferenceEquals(value, null))
                return new NoFormatReady(null);
            var got = Lookup(value.GetType());
            if (ReferenceEquals(got, null))
                return new NoFormatReady(value);
            return new Ready(this, value, got);
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void Add(Type type, Func<object, object> formatter, double? preference = null)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            lock (TypeFormatters)
            {
                try
                {
                    TypeFormatters.Add(type, new Spec(formatter, preference));
                }
                finally
                {
                    ClearLookupTable();
                }
            }
        }

        protected void ClearLookupTable()
        {
            lock (TypeFormatters)
            {
                _lookupTable = new ConditionalWeakTable<Type, Spec>();
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void Add<TSource, TResult>(Func<TSource, TResult> formatter, double? preference = null)
        {
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            lock (TypeFormatters)
            {
                Add(typeof(TSource), src => formatter((TSource) src), preference);
            }
        }

        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public void Add<TSource>(Func<TSource, object> formatter, double? preference = null)
            => Add(typeof(TSource), src => formatter((TSource) src), preference);

        private static IEnumerable<Type> BaseTypes(Type type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        protected virtual IEnumerable<Type> SearchTypes(Type type)
        {
            if (ReferenceEquals(type, null))
                throw new ArgumentNullException(nameof(type));
            return BaseTypes(type).Concat(type.GetInterfaces());
        }

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
            {
                return _lookupTable.GetValue(type, FindType);
            }
        }

        protected class Spec
        {
            public Spec(Func<object, object> formatter, double? preference)
            {
                Formatter = formatter;
                Preference = preference;
            }

            public Func<object, object> Formatter { get; }
            public double? Preference { get; }
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
    }
}