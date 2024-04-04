﻿using TypeShape.Applications.CborSerializer.Converters;

namespace TypeShape.Applications.CborSerializer;

public static partial class CborSerializer
{
    private sealed class Builder : ITypeShapeVisitor
    {
        private readonly TypeDictionary _cache = new();

        public CborConverter<T> BuildConverter<T>(ITypeShape<T> typeShape)
        {
            if (s_builtInConverters.TryGetValue(typeof(T), out CborConverter? defaultConverter))
            {
                return (CborConverter<T>)defaultConverter;
            }

            return _cache.GetOrAdd<CborConverter<T>>(typeShape, this, self => new DelayedCborConverter<T>(self));
        }

        public object? VisitType<T>(ITypeShape<T> objectShape, object? state)
        {
            CborPropertyConverter<T>[] properties = objectShape
                .GetProperties()
                .Select(prop => (CborPropertyConverter<T>)prop.Accept(this)!)
                .ToArray();

            // Prefer the default constructor if available.
            IConstructorShape? ctor = objectShape
                .GetConstructors()
                .MinBy(ctor => ctor.ParameterCount);

            return ctor != null
                ? (CborObjectConverter<T>)ctor.Accept(this, state: properties)!
                : new CborObjectConverter<T>(properties);
        }

        public object? VisitProperty<TDeclaringType, TPropertyType>(IPropertyShape<TDeclaringType, TPropertyType> property, object? state)
        {
            CborConverter<TPropertyType> propertyConverter = BuildConverter(property.PropertyType);
            return new CborPropertyConverter<TDeclaringType, TPropertyType>(property, propertyConverter);
        }

        public object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructorShape<TDeclaringType, TArgumentState> constructor, object? state)
        {
            var properties = (CborPropertyConverter<TDeclaringType>[])state!;

            if (constructor.ParameterCount == 0)
            {
                return new CborObjectConverterWithDefaultCtor<TDeclaringType>(constructor.GetDefaultConstructor(), properties);
            }

            CborPropertyConverter<TArgumentState>[] constructorParams = constructor
                .GetParameters()
                .Select(param => (CborPropertyConverter<TArgumentState>)param.Accept(this)!)
                .ToArray();

            return new CborObjectConverterWithParameterizedCtor<TDeclaringType, TArgumentState>(
                constructor.GetArgumentStateConstructor(),
                constructor.GetParameterizedConstructor(),
                constructorParams,
                properties);
        }

        public object? VisitConstructorParameter<TArgumentState, TParameterType>(IConstructorParameterShape<TArgumentState, TParameterType> parameter, object? state)
        {
            CborConverter<TParameterType> paramConverter = BuildConverter(parameter.ParameterType);
            return new CborPropertyConverter<TArgumentState, TParameterType>(parameter, paramConverter);
        }

        public object? VisitEnumerable<TEnumerable, TElement>(IEnumerableTypeShape<TEnumerable, TElement> enumerableTypeShape, object? state)
        {
            CborConverter<TElement> elementConverter = BuildConverter(enumerableTypeShape.ElementType);
            Func<TEnumerable, IEnumerable<TElement>> getEnumerable = enumerableTypeShape.GetGetEnumerable();

            return enumerableTypeShape.ConstructionStrategy switch
            {
                CollectionConstructionStrategy.Mutable =>
                    new CborMutableEnumerableConverter<TEnumerable, TElement>(
                        elementConverter,
                        getEnumerable,
                        enumerableTypeShape.GetDefaultConstructor(),
                        enumerableTypeShape.GetAddElement()),

                CollectionConstructionStrategy.Enumerable =>
                    new CborEnumerableConstructorEnumerableConverter<TEnumerable, TElement>(
                        elementConverter,
                        getEnumerable,
                        enumerableTypeShape.GetEnumerableConstructor()),

                CollectionConstructionStrategy.Span =>
                    new CborSpanConstructorEnumerableConverter<TEnumerable, TElement>(
                        elementConverter,
                        getEnumerable,
                        enumerableTypeShape.GetSpanConstructor()),

                _ => new CborEnumerableConverter<TEnumerable, TElement>(elementConverter, getEnumerable),
            };
        }

        public object? VisitDictionary<TDictionary, TKey, TValue>(IDictionaryShape<TDictionary, TKey, TValue> dictionaryShape, object? state) where TKey : notnull
        {
            CborConverter<TKey> keyConverter = BuildConverter(dictionaryShape.KeyType);
            CborConverter<TValue> valueConverter = BuildConverter(dictionaryShape.ValueType);
            Func<TDictionary, IReadOnlyDictionary<TKey, TValue>> getDictionary = dictionaryShape.GetGetDictionary();

            return dictionaryShape.ConstructionStrategy switch
            {
                CollectionConstructionStrategy.Mutable => 
                    new CborMutableDictionaryConverter<TDictionary, TKey, TValue>(
                        keyConverter,
                        valueConverter,
                        getDictionary,
                        dictionaryShape.GetDefaultConstructor(),
                        dictionaryShape.GetAddKeyValuePair()),

                CollectionConstructionStrategy.Enumerable => 
                    new CborEnumerableConstructorDictionaryConverter<TDictionary, TKey, TValue>(
                        keyConverter,
                        valueConverter,
                        getDictionary,
                        dictionaryShape.GetEnumerableConstructor()),

                CollectionConstructionStrategy.Span =>
                    new CborSpanConstructorDictionaryConverter<TDictionary, TKey, TValue>(
                        keyConverter,
                        valueConverter,
                        getDictionary,
                        dictionaryShape.GetSpanConstructor()),

                _ => new CborDictionaryConverter<TDictionary, TKey, TValue>(keyConverter, valueConverter, getDictionary),
            };
        }

        public object? VisitNullable<T>(INullableTypeShape<T> nullableTypeShape, object? state) where T : struct
        {
            CborConverter<T> elementConverter = BuildConverter(nullableTypeShape.ElementType);
            return new CborNullableConverter<T>(elementConverter);
        }

        public object? VisitEnum<TEnum, TUnderlying>(IEnumTypeShape<TEnum, TUnderlying> enumTypeShape, object? state) where TEnum : struct, Enum
        {
            return new CborEnumConverter<TEnum>();
        }

        private static readonly Dictionary<Type, CborConverter> s_builtInConverters = new CborConverter[]
        {
            new BoolConverter(),
            new StringConverter(),
            new SByteConverter(),
            new Int16Converter(),
            new Int32Converter(),
            new Int64Converter(),
            new Int128Converter(),
            new ByteConverter(),
            new ByteArrayConverter(),
            new UInt16Converter(),
            new UInt32Converter(),
            new UInt64Converter(),
            new UInt128Converter(),
            new CharConverter(),
            new HalfConverter(),
            new SingleConverter(),
            new DoubleConverter(),
            new DecimalConverter(),
            new DateTimeConverter(),
            new DateTimeOffsetConverter(),
            new TimeSpanConverter(),
            new DateOnlyConverter(),
            new TimeOnlyConverter(),
            new GuidConverter(),
            new UriConverter(),
            new VersionConverter(),
            new BigIntegerConverter(),
            new RuneConverter(),
            new ObjectConverter(),
        }.ToDictionary(conv => conv.Type);
    }
}
