using System.Xml.Linq;
using TypeShape.Abstractions;
using TypeShape.Applications.PrettyPrinter;

namespace TypeShape.Applications.GraphSearch
{
    public static partial class GraphSearch
    {
        private sealed class Builder<TMatch> : TypeShapeVisitor
        {
            private readonly TypeDictionary _cache = new();

            public MatchDelegate<T, TMatch> BuildDelegate<T>(ITypeShape<T> shape)
            {
                return _cache.GetOrAdd<MatchDelegate<T, TMatch>>(
                    shape,
                    this,
                    self => (value, predicate) => self.Result(value, predicate));
            }
            
            public override object VisitDictionary<TDictionary, TKey, TValue>(
                IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
            {
                var getter = dictionaryShape.GetGetDictionary();
                var valueMatcher = BuildDelegate(dictionaryShape.ValueType);
                return new MatchDelegate<TDictionary, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var dict = getter(value);
                        foreach (var (_, kvpValue) in dict)
                        {
                            if (kvpValue is TMatch m && predicate(m))
                            {
                                return m;
                            }

                            if (valueMatcher(kvpValue, predicate) is { } mn)
                            {
                                return mn;
                            }
                        }
                    }

                    return default;
                });
            }

            public override object VisitEnumerable<TEnumerable, TElement>(
                IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
            {
                var getter = enumerableShape.GetGetEnumerable();
                var elementMatcher = BuildDelegate(enumerableShape.ElementType);
                return new MatchDelegate<TEnumerable, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var enumerable = getter(value);
                        foreach (var element in enumerable)
                        {
                            if (element is TMatch m && predicate(m))
                            {
                                return m;
                            }

                            if (elementMatcher(element, predicate) is { } mn)
                            {
                                return mn;
                            }
                        }
                    }

                    return default;
                });
            }

            public override object VisitNullable<T>(INullableTypeShape<T> nullableShape, object? state = null)
                where T : struct
            {
                var elementMatcher = BuildDelegate(nullableShape.ElementType);
                return new MatchDelegate<T?, TMatch>((value, predicate) =>
                {
                    if (value is { } v)
                    {
                        if (v is TMatch m && predicate(m))
                        {
                            return m;
                        }

                        if (elementMatcher(v, predicate) is { } mn)
                        {
                            return mn;
                        }
                    }

                    return default;
                });
            }

            public override object VisitProperty<TDeclaringType, TPropertyType>(
                IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
            {
                var getter = propertyShape.GetGetter();
                var propMatcher = BuildDelegate(propertyShape.PropertyType);
                return new MatchDelegate<TDeclaringType, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var propValue = getter(ref value);
                        if (propValue is TMatch m && predicate(m))
                        {
                            return m;
                        }

                        if (propMatcher(propValue, predicate) is { } mn)
                        {
                            return mn;
                        }
                    }

                    return default;
                });
            }

            public override object VisitType<T>(ITypeShape<T> typeShape, object? state = null)
            {
                var propMatchers = typeShape
                    .GetProperties()
                    .Where(prop => prop.HasGetter)
                    .Select(prop => (MatchDelegate<T, TMatch>?) prop.Accept(this, state)!)
                    .Where(prop => prop is not null)
                    .ToArray();

                return new MatchDelegate<T, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        if (value is TMatch m && predicate(m))
                        {
                            return m;
                        }

                        foreach (var propMatch in propMatchers)
                        {
                            if (propMatch(value, predicate) is { } mn)
                            {
                                return mn;
                            }
                        }
                    }

                    return default;
                });
            }
        }
    }
}