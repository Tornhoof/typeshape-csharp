using System.Collections.Generic;
using System.Xml.Linq;
using TypeShape.Abstractions;
using TypeShape.Applications.PrettyPrinter;

namespace TypeShape.Applications.GraphSearch;

public static partial class GraphSearch
{
    private sealed class Builder : TypeShapeVisitor
    {
        private readonly TypeDictionary _cache = new();

        public BreadthFirstSearch<T, TMatch> BuildBreadthFirstSearch<T, TMatch>(ITypeShape<T> shape)
        {
            return _cache.GetOrAdd<BreadthFirstSearch<T, TMatch>>(
                shape,
                this,
                delayedValueFactory: self => (value, predicate) => self.Result(value, predicate), typeof(TMatch));
        }

        public DepthFirstSearch<T, TMatch> BuildDepthFirstSearch<T, TMatch>(ITypeShape<T> shape)
        {
            return _cache.GetOrAdd<DepthFirstSearch<T, TMatch>>(
                shape,
                this,
                delayedValueFactory: self => (value, predicate) => self.Result(value, predicate), typeof(TMatch)); 
        }

        public override object? VisitDictionary<TDictionary, TKey, TValue>(
            IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state = null)
        {
            if (state is Type t && typeof(TValue) == t)
            {
                var getter = dictionaryShape.GetGetDictionary();
                return new BreadthFirstSearch<TDictionary, TValue>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var dict = getter(value);
                        foreach (var (_, kvpValue) in dict)
                            if (predicate(kvpValue))
                                return kvpValue;
                    }

                    return default;
                });
            }

            return null;
        }

        public override object? VisitEnumerable<TEnumerable, TElement>(
            IEnumerableTypeShape<TEnumerable, TElement> enumerableShape, object? state = null)
        {
            if (state is Type t && typeof(TElement) == t)
            {
                var getter = enumerableShape.GetGetEnumerable();
                return new BreadthFirstSearch<TEnumerable, TElement>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var enumerable = getter(value);
                        foreach (var element in enumerable)
                            if (predicate(element))
                                return element;
                    }

                    return default;
                });
            }

            return null;
        }

        public override object? VisitProperty<TDeclaringType, TPropertyType>(
            IPropertyShape<TDeclaringType, TPropertyType> propertyShape, object? state = null)
        {
            if (state is Type t && typeof(TPropertyType) == t)
            {
                var getter = propertyShape.GetGetter();
                return new BreadthFirstSearch<TDeclaringType, TPropertyType>((value, predicate) =>
                {
                    if (value is not null)
                    {
                        var propValue = getter(ref value);
                        if (predicate(propValue)) return propValue;
                    }

                    return default;
                });
            }

            return null;
        }

        public override object? VisitType<T>(ITypeShape<T> typeShape, object? state = null)
        {
            IPropertyShape[] properties = typeShape
                .GetProperties()
                .Where(prop => prop.HasGetter)
                .Select(prop => (IPropertyShape?) prop.Accept(this, state)!)
                .Where(prop => prop is not null)
                .ToArray();
            List<Delegate> delegates = new();
            foreach (var propertyShape in properties)
            {
                Delegate? del = (Delegate?) propertyShape.Accept(this, state);
                if (del is not null)
                {
                    delegates.Add(del);
                }
            }

            return Delegate.Combine(delegates.ToArray());
        }
    }
}