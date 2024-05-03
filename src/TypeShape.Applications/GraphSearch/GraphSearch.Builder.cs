using TypeShape.Abstractions;

namespace TypeShape.Applications.GraphSearch
{
    public static partial class GraphSearch
    {
        private sealed class Builder<TMatch> : TypeShapeVisitor
        {
            private readonly TypeDictionary _cache = new();

            public BreadthFirstSearch<T, TMatch> BuildBreadthFirstSearch<T>(ITypeShape<T> shape)
            {
                return _cache.GetOrAdd<BreadthFirstSearch<T, TMatch>>(
                    shape,
                    this,
                    self => (value, predicate) => self.Result(value, predicate));
            }

            public DepthFirstSearch<T, TMatch> BuildDepthFirstSearch<T>(ITypeShape<T> shape)
            {
                return _cache.GetOrAdd<DepthFirstSearch<T, TMatch>>(
                    shape,
                    this,
                    self => (value, predicate) => self.Result(value, predicate));
            }

            public override object? VisitDictionary<TDictionary, TKey, TValue>(
                IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
            {
                var getter = dictionaryShape.GetGetDictionary();
                var valueTypeShape = BuildBreadthFirstSearch(dictionaryShape.ValueType);
                return new BreadthFirstSearch<TDictionary, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var dict = getter(value);
                        foreach (var (_, kvpValue) in dict)
                        {
                            valueTypeShape(kvpValue, predicate);
                        }
                    }

                    return default;
                });
            }

            public override object? VisitEnumerable<TEnumerable, TElement>(
                IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
            {
                var getter = enumerableShape.GetGetEnumerable();
                var elementBfs = BuildBreadthFirstSearch(enumerableShape.ElementType);
                return new BreadthFirstSearch<TEnumerable, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var enumerable = getter(value);
                        foreach (var element in enumerable)
                        {
                            elementBfs(element, predicate);
                        }
                    }

                    return default;
                });
            }

            public override object? VisitProperty<TDeclaringType, TPropertyType>(
                IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
            {
                var getter = propertyShape.GetGetter();
                var propBfs = BuildBreadthFirstSearch(propertyShape.PropertyType);
                return new BreadthFirstSearch<TDeclaringType, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var propValue = getter(ref value);
                        propBfs(propValue, predicate);
                    }

                    return default;
                });
            }

            public override object? VisitType<T>(ITypeShape<T> typeShape, object? state = null)
            {
                var bfsFunctors = typeShape
                    .GetProperties()
                    .Where(prop => prop.HasGetter)
                    .Select(prop => (BreadthFirstSearch<T, TMatch>?) prop.Accept(this, state)!)
                    .Where(prop => prop is not null)
                    .ToArray();

                return new BreadthFirstSearch<T, TMatch>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        foreach (var breadthFirstSearch in bfsFunctors)
                        {
                            breadthFirstSearch(value, predicate);
                        }
                    }

                    return default;
                });
            }
        }
    }
}