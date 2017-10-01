using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Castle.MicroKernel;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace WindsorTests.LifesStyle
{
    public static class WindsorExtensions
    {
        private static IHandler FirstMatchingScopeRootSelector(this IEnumerable<Type> types, IHandler[] resolutionStack)
        {
            var selected = types.SelectMany((t, ti) =>
                    resolutionStack.Select(
                            (h, hi) =>
                                new
                                {
                                    h,
                                    hi,
                                    ti,
                                    canApply =
                                    t.GetTypeInfo().IsAssignableFrom((TypeInfo) h.ComponentModel.Implementation)
                                })
                        .Where(x => x.canApply))
                .OrderBy(x => x.hi)
                .ThenBy(x => x.ti)
                .FirstOrDefault();
            return selected?.h;
        }

        public static BoundToAnyCapture<T> BoundToAny<T>(
            this LifestyleGroup<T> lifestyleGroup)
            where T : class
        {
            Contract.Requires(!ReferenceEquals(lifestyleGroup, null));
            return new BoundToAnyCapture<T>(lifestyleGroup);
        }

        public class BoundToAnyCapture<T> where T : class
        {
            private readonly List<Type> _types = new List<Type>();

            public BoundToAnyCapture(LifestyleGroup<T> lifeStyleGroup)
            {
                LifeStyleGroup = lifeStyleGroup;
            }

            public LifestyleGroup<T> LifeStyleGroup { get; }

            public ComponentRegistration<T> Final()
            {
                return LifeStyleGroup.BoundTo(_types.FirstMatchingScopeRootSelector);
            }

            public BoundToAnyCapture<T> Or(Type t)
            {
                _types.Add(t);
                return this;
            }

            public BoundToAnyCapture<T> Or<TBound>() => Or(typeof(TBound));

            public ComponentRegistration<T> OfType(params Type[] types)
            {
                foreach (var type in types)
                    Or(type);
                return Final();
            }

            public ComponentRegistration<T> OfType<TBound1>() => Or<TBound1>().Final();
            public ComponentRegistration<T> OfType<TBound1, TBound2>() => Or<TBound1>().Or<TBound2>().Final();

            public ComponentRegistration<T> OfType<TBound1, TBound2, TBound3>()
                => Or<TBound1>().Or<TBound2>().Or<TBound3>().Final();
        }
    }
}