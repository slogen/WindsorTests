using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using WindsorTests.InterceptorLogging.Interface;

namespace WindsorTests.InterceptorLogging.Formatter
{
    public class SequenceFormatter : IArgumentFormatter
    {
        private readonly IArgumentFormatter _innerFormatter;

        public SequenceFormatter(IArgumentFormatter innerFormatter)
        {
            _innerFormatter = innerFormatter;
        }

        public double DefaultPreference { get; set; } = -.1;
        public int PrefixPrintCount { get; } = 10;

        public IFormatReady Prepare(object value)
        {
            if (value == null)
                return null;
            var c = value as ICollection;
            if (ReferenceEquals(c, null))
                return _innerFormatter.Prepare(value);
            return new Ready(this, c);
        }

        public double Preference(object value)
        {
            return value is IEnumerable ? DefaultPreference : double.NaN;
        }


        protected PartPrint Parts(ICollection collection) => new PartPrint(this, collection);

        protected class PartPrint
        {
            private readonly SequenceFormatter _parent;
            private readonly ICollection _parts;

            public PartPrint(SequenceFormatter parent, ICollection parts)
            {
                _parent = parent;
                _parts = parts;
            }

            protected IEnumerable<object> Objects(ICollection collection)
            {
                var it = collection.GetEnumerator();
                try
                {
                    while (it.MoveNext())
                        yield return it.Current;
                }
                finally
                {
                    (it as IDisposable)?.Dispose();
                }
            }

            public override string ToString()
            {
                var count = _parts.Count;
                return
                    $"#{count}({string.Join(", ", Objects(_parts).Take(_parent.PrefixPrintCount))}{(count > _parent.PrefixPrintCount ? "..." : "")})";
            }
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
    }
}