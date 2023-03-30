﻿using System.Collections;
using System.Reflection;
using TypeShape.Applications.RandomGenerator;
using TypeShape.ReflectionProvider;
using Xunit;

namespace TypeShape.Tests;

public abstract class TypeShapeProviderTests
{
    protected abstract ITypeShapeProvider Provider { get; }
    protected abstract bool SupportsNonPublicMembers { get; }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void TypeShapeReportsExpectedInfo<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();

        Assert.NotNull(shape);
        Assert.Same(Provider, shape.Provider);
        Assert.Equal(typeof(T), shape.Type);
        Assert.Equal(typeof(T), shape.AttributeProvider);

        TypeKind expectedKind = GetExpectedTypeKind(testCase.Value, Provider is ReflectionTypeShapeProvider);
        Assert.Equal(expectedKind, shape.Kind);

        static TypeKind GetExpectedTypeKind(T value, bool isReflectionProvider)
        {
            if (typeof(T).IsEnum)
            {
                return TypeKind.Enum;
            }
            else if (typeof(T).IsValueType && default(T) is null)
            {
                return TypeKind.Nullable;
            }

            if (value is IEnumerable && value is not string)
            {
                if (value is IDictionary)
                {
                    return isReflectionProvider ? TypeKind.Dictionary | TypeKind.Enumerable : TypeKind.Dictionary;
                }

                return TypeKind.Enumerable;
            }

            return TypeKind.None;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetProperties<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        var visitor = new PropertyTestVisitor();
        foreach (IProperty property in shape.GetProperties(nonPublic: SupportsNonPublicMembers, includeFields: true))
        {
            Assert.Equal(typeof(T), property.DeclaringType.Type);
            property.Accept(visitor, testCase.Value);
        }
    }

    private sealed class PropertyTestVisitor : IPropertyVisitor
    {
        public object? VisitProperty<TDeclaringType, TPropertyType>(IProperty<TDeclaringType, TPropertyType> property, object? state)
        {
            TDeclaringType obj = (TDeclaringType)state!;
            TPropertyType propertyType = default!;

            if (property.HasGetter)
            {
                var getter = property.GetGetter();
                propertyType = getter(ref obj);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => property.GetGetter());
            }

            if (property.HasSetter)
            {
                var setter = property.GetSetter();
                setter(ref obj, propertyType);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => property.GetSetter());
            }

            return null;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetConstructors<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        var visitor = new ConstructorTestVisitor();
        foreach (IConstructor ctor in shape.GetConstructors(nonPublic: SupportsNonPublicMembers))
        {
            Assert.Equal(typeof(T), ctor.DeclaringType.Type);
            ctor.Accept(visitor, typeof(T));
        }
    }

    private sealed class ConstructorTestVisitor : IConstructorVisitor, IConstructorParameterVisitor
    {
        public object? VisitConstructor<TDeclaringType, TArgumentState>(IConstructor<TDeclaringType, TArgumentState> constructor, object? state)
        {
            var expectedType = (Type)state!;
            Assert.Equal(typeof(TDeclaringType), expectedType);

            int parameterCount = constructor.ParameterCount;
            IConstructorParameter[] parameters = constructor.GetParameters().ToArray();
            Assert.Equal(parameterCount, parameters.Length);

            if (parameterCount == 0)
            {
                var defaultCtor = constructor.GetDefaultConstructor();
                TDeclaringType defaultValue = defaultCtor();
                Assert.NotNull(defaultValue);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => constructor.GetDefaultConstructor());
            }

            int i = 0;
            TArgumentState argumentState = constructor.GetArgumentStateConstructor().Invoke();
            foreach (IConstructorParameter parameter in parameters)
            {
                Assert.Equal(i++, parameter.Position);
                argumentState = (TArgumentState)parameter.Accept(this, argumentState)!;
            }

            var parameterizedCtor = constructor.GetParameterizedConstructor();
            Assert.NotNull(parameterizedCtor);

            if (typeof(TDeclaringType).Assembly == Assembly.GetExecutingAssembly())
            {
                TDeclaringType value = parameterizedCtor.Invoke(argumentState);
                Assert.NotNull(value);
            }
            return null;
        }

        public object? VisitConstructorParameter<TArgumentState, TParameter>(IConstructorParameter<TArgumentState, TParameter> parameter, object? state)
        {
            var argState = (TArgumentState)state!;
            var setter = parameter.GetSetter();

            TParameter? value = parameter.HasDefaultValue ? parameter.DefaultValue : default;
            setter(ref argState, value!);
            return argState;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetEnumType<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        if (shape.Kind.HasFlag(TypeKind.Enum))
        {
            IEnumType enumType = shape.GetEnumType();
            Assert.Equal(typeof(T), enumType.Type.Type);
            Assert.Equal(typeof(T).GetEnumUnderlyingType(), enumType.UnderlyingType.Type);
            var visitor = new EnumTestVisitor();
            enumType.Accept(visitor, typeof(T));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => shape.GetEnumType());
        }
    }

    private sealed class EnumTestVisitor : IEnumTypeVisitor
    {
        public object? VisitEnum<TEnum, TUnderlying>(IEnumType<TEnum, TUnderlying> enumType, object? state) where TEnum : struct, Enum
        {
            var type = (Type)state!;
            Assert.Equal(typeof(TEnum), type);
            Assert.Equal(typeof(TUnderlying), type.GetEnumUnderlyingType());
            return null;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetNullableType<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        if (shape.Kind.HasFlag(TypeKind.Nullable))
        {
            INullableType nullableType = shape.GetNullableType();
            Assert.Equal(typeof(T), nullableType.Type.Type);
            Assert.Equal(typeof(T).GetGenericArguments()[0], nullableType.ElementType.Type);
            var visitor = new NullableTestVisitor();
            nullableType.Accept(visitor, typeof(T));
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => shape.GetNullableType());
        }
    }

    private sealed class NullableTestVisitor : INullableTypeVisitor
    {
        public object? VisitNullable<T>(INullableType<T> nullableType, object? state) where T : struct
        {
            var type = (Type)state!;
            Assert.Equal(typeof(T?), type);
            Assert.Equal(typeof(T), nullableType.ElementType.Type);
            return null;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetDictionaryType<T>(TestCase<T> testCase)
    {
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        if (shape.Kind.HasFlag(TypeKind.Dictionary))
        {
            IDictionaryType dictionaryType = shape.GetDictionaryType();
            Assert.Equal(typeof(T), dictionaryType.Type.Type);
            if (typeof(T).IsGenericType)
            {
                Assert.Equal(typeof(T).GetGenericArguments()[0], dictionaryType.KeyType.Type);
                Assert.Equal(typeof(T).GetGenericArguments()[1], dictionaryType.ValueType.Type);
            }
            else
            {
                Assert.Equal(typeof(object), dictionaryType.KeyType.Type);
                Assert.Equal(typeof(object), dictionaryType.ValueType.Type);
            }

            var visitor = new DictionaryTestVisitor();
            dictionaryType.Accept(visitor, testCase.Value);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => shape.GetDictionaryType());
        }
    }

    private sealed class DictionaryTestVisitor : IDictionaryTypeVisitor
    {
        public object? VisitDictionaryType<TDictionary, TKey, TValue>(IDictionaryType<TDictionary, TKey, TValue> dictionaryType, object? state) where TKey : notnull
        {
            var dictionary = (TDictionary)state!;
            var getter = dictionaryType.GetGetDictionary();
            int count = getter(dictionary).Count();

            if (dictionaryType.IsMutable)
            {
                var adder = dictionaryType.GetAddKeyValuePair();
                RandomGenerator<TKey> keyGenerator = RandomGenerator.Create((IType<TKey>)dictionaryType.KeyType);
                TKey newKey = keyGenerator.GenerateValue(size: 1000, seed: 42);
                adder(ref dictionary, new(newKey, default!));
                Assert.Equal(count + 1, getter(dictionary).Count());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => dictionaryType.GetAddKeyValuePair());
            }

            return null;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void GetEnumerableType<T>(TestCase<T> testCase)
    {
        _ = testCase; // not used here
        IType<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);

        if (shape.Kind.HasFlag(TypeKind.Enumerable))
        {
            IEnumerableType enumerableType = shape.GetEnumerableType();
            Assert.Equal(typeof(T), enumerableType.Type.Type);

            if (typeof(T).IsGenericType)
            {
                if (shape.Kind.HasFlag(TypeKind.Dictionary))
                {
                    Assert.Equal(typeof(KeyValuePair<,>).MakeGenericType(typeof(T).GetGenericArguments()), enumerableType.ElementType.Type);
                }
                else
                {
                    Assert.Equal(typeof(T).GetGenericArguments()[0], enumerableType.ElementType.Type);
                }
            }
            else if (typeof(T).IsArray)
            {
                Assert.Equal(typeof(T).GetElementType(), enumerableType.ElementType.Type);
            }
            else
            {
                Assert.Equal(typeof(object), enumerableType.ElementType.Type);
            }

            var visitor = new EnumerableTestVisitor();
            enumerableType.Accept(visitor, testCase.Value);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() => shape.GetEnumerableType());
        }
    }

    private sealed class EnumerableTestVisitor : IEnumerableTypeVisitor
    {
        public object? VisitEnumerableType<TEnumerable, TElement>(IEnumerableType<TEnumerable, TElement> enumerableType, object? state)
        {
            var enumerable = (TEnumerable)state!;
            var getter = enumerableType.GetGetEnumerable();
            int count = getter(enumerable).Count();

            if (enumerableType.IsMutable)
            {
                var adder = enumerableType.GetAddElement();
                RandomGenerator<TElement> elementGenerator = RandomGenerator.Create((IType<TElement>)enumerableType.ElementType);
                TElement newElement = elementGenerator.GenerateValue(size: 1000, seed: 42);
                adder(ref enumerable, newElement);
                Assert.Equal(count + 1, getter(enumerable).Count());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => enumerableType.GetAddElement());
            }

            return null;
        }
    }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void ReturnsExpectedAttributeProviders<T>(TestCase<T> testCase)
    {
        if (testCase.IsTuple)
        {
            return; // tuples don't report attribute metadata.
        }

        IType<T> shape = Provider.GetShape<T>()!;

        foreach (IProperty property in shape.GetProperties(nonPublic: SupportsNonPublicMembers, includeFields: true))
        {
            ICustomAttributeProvider? attributeProvider = property.AttributeProvider;
            Assert.NotNull(attributeProvider);

            if (property.IsField)
            {
                FieldInfo fieldInfo = Assert.IsAssignableFrom<FieldInfo>(attributeProvider);
                Assert.Equal(typeof(T), fieldInfo.ReflectedType);
                Assert.Equal(property.Name, fieldInfo.Name);
                Assert.Equal(property.PropertyType.Type, fieldInfo.FieldType);
            }
            else
            {
                PropertyInfo propertyInfo = Assert.IsAssignableFrom<PropertyInfo>(attributeProvider);
                Assert.True(propertyInfo.DeclaringType!.IsAssignableFrom(typeof(T)));
                Assert.Equal(property.Name, propertyInfo.Name);
                Assert.Equal(property.PropertyType.Type, propertyInfo.PropertyType);
                Assert.True(!property.HasGetter || propertyInfo.CanRead);
                Assert.True(!property.HasSetter || propertyInfo.CanWrite);
            }
        }

        foreach (IConstructor constructor in shape.GetConstructors(nonPublic: SupportsNonPublicMembers))
        {
            ICustomAttributeProvider? attributeProvider = constructor.AttributeProvider;

            ParameterInfo[] parameters;
            if (attributeProvider is null)
            {
                Assert.True(typeof(T).IsValueType);
                parameters = Array.Empty<ParameterInfo>();
            }
            else
            {
                ConstructorInfo ctorInfo = Assert.IsAssignableFrom<ConstructorInfo>(attributeProvider);
                Assert.Equal(typeof(T), ctorInfo.DeclaringType);
                Assert.Equal(constructor.ParameterCount, constructor.ParameterCount);
                parameters = ctorInfo.GetParameters();
            }

            int i = 0;
            foreach (IConstructorParameter ctorParam in constructor.GetParameters())
            {
                if (i < parameters.Length)
                {
                    ParameterInfo actualParameter = parameters[i];
                    Assert.Equal(actualParameter.Position, ctorParam.Position);
                    Assert.Equal(actualParameter.ParameterType, ctorParam.ParameterType.Type);
                    Assert.Equal(actualParameter.Name, ctorParam.Name);

                    ParameterInfo paramInfo = Assert.IsAssignableFrom<ParameterInfo>(ctorParam.AttributeProvider);
                    Assert.Equal(actualParameter.Position, paramInfo.Position);
                    Assert.Equal(actualParameter.Name, paramInfo.Name);
                    Assert.Equal(actualParameter.ParameterType, paramInfo.ParameterType);
                }
                else
                {
                    MemberInfo memberInfo = Assert.IsAssignableFrom<MemberInfo>(ctorParam.AttributeProvider);

                    Assert.Equal(typeof(T), memberInfo.DeclaringType);
                    Assert.Equal(memberInfo.Name, ctorParam.Name);
                    Assert.False(ctorParam.HasDefaultValue);
                    Assert.Equal(i, ctorParam.Position);
                    Assert.True(memberInfo is PropertyInfo or FieldInfo);

                    if (memberInfo is PropertyInfo p)
                    {
                        Assert.Equal(p.PropertyType, ctorParam.ParameterType.Type);
                        Assert.NotNull(p.SetMethod);
                    }
                    else if (memberInfo is FieldInfo f)
                    {
                        Assert.Equal(f.FieldType, ctorParam.ParameterType.Type);
                        Assert.False(f.IsInitOnly);
                    }
                }

                i++;
            }
        }
    }
}

public sealed class TypeShapeProviderTests_Reflection : TypeShapeProviderTests
{
    protected override ITypeShapeProvider Provider { get; } = new ReflectionTypeShapeProvider(useReflectionEmit: false);
    protected override bool SupportsNonPublicMembers => true;
}

public sealed class TypeShapeProviderTests_ReflectionEmit : TypeShapeProviderTests
{
    protected override ITypeShapeProvider Provider { get; } = new ReflectionTypeShapeProvider(useReflectionEmit: true);
    protected override bool SupportsNonPublicMembers => true;
}

public sealed class TypeShapeProviderTests_SourceGen : TypeShapeProviderTests
{
    protected override ITypeShapeProvider Provider { get; } = SourceGenTypeShapeProvider.Default;
    protected override bool SupportsNonPublicMembers => false;
}

