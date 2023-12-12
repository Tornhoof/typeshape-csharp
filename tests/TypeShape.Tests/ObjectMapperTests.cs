﻿using TypeShape.Applications.ObjectMapper;
using TypeShape.Applications.StructuralEquality;
using TypeShape.ReflectionProvider;
using Xunit;

namespace TypeShape.Tests;

public abstract class ObjectMapperTests
{
    protected abstract ITypeShapeProvider Provider { get; }

    [Theory]
    [MemberData(nameof(TestTypes.GetTestCases), MemberType = typeof(TestTypes))]
    public void MapToTheSameType_ProducesEqualCopy<T>(TestCase<T> testCase)
    {
        if (testCase.IsMultiDimensionalArray || testCase.IsAbstract)
        {
            return;
        }

        (Mapper<T, T> mapper, IEqualityComparer<T> comparer, ITypeShape<T> shape) = GetMapperAndEqualityComparer<T>();

        T? mappedValue = mapper(testCase.Value);

        if (testCase.Value != null && typeof(T) != typeof(object) && shape.Kind != TypeKind.None)
        {
            Assert.NotSame((object?)mappedValue, (object?)testCase.Value);
        }

        if (testCase.IsStack)
        {
            Assert.Equal(testCase.Value, mapper(mappedValue), comparer!);
        }
        else if (!testCase.DoesNotRoundtrip)
        {
            Assert.Equal(testCase.Value, mappedValue, comparer!);
        }
    }

    [Fact]
    public void MapsToMatchingType()
    {
        Assert.Throws<InvalidOperationException>(() => GetMapper<WeatherForecast, WeatherForecastDTO>());
        Mapper<WeatherForecastDTO, WeatherForecast> mapper = GetMapper<WeatherForecastDTO, WeatherForecast>();

        var weatherForecastDTO = new WeatherForecastDTO
        {
            Id = "id",
            Date = DateTime.Parse("1975-01-01"),
            DatesAvailable = [DateTime.Parse("1975-01-01"), DateTime.Parse("1976-01-01")],
            Summary = "Summary",
            SummaryField = "SummaryField",
            TemperatureCelsius = 42,
            SummaryWords = ["Summary", "Words"],
            TemperatureRanges = new()
            {
                ["Range1"] = new() { Low = 1, High = 2 },
                ["Range2"] = new() { Low = 3, High = 4 },
            }
        };

        var weatherForecast = mapper(weatherForecastDTO);

        Assert.Equal(weatherForecastDTO.Date, weatherForecast.Date);
        Assert.Equal(weatherForecastDTO.DatesAvailable, weatherForecast.DatesAvailable!);
        Assert.Equal(weatherForecastDTO.TemperatureCelsius, weatherForecast.TemperatureCelsius);
        Assert.Equal(weatherForecastDTO.SummaryWords, weatherForecast.SummaryWords!);
        Assert.Equal(new Dictionary<string, HighLowTemps> { ["Range1"] = new() { High = 2 }, ["Range2"] = new() { High = 4 }, }, weatherForecast.TemperatureRanges!);
        Assert.Null(weatherForecast.UnmatchedProperty);
    }

    private Mapper<TFrom, TTo> GetMapper<TFrom, TTo>()
    {
        ITypeShape<TFrom>? fromShape = Provider.GetShape<TFrom>();
        ITypeShape<TTo>? toShape = Provider.GetShape<TTo>();
        Assert.NotNull(fromShape);
        Assert.NotNull(toShape);

        return Mapper.Create(fromShape, toShape);
    }

    private (Mapper<T, T>, IEqualityComparer<T>, ITypeShape<T>) GetMapperAndEqualityComparer<T>()
    {
        ITypeShape<T>? shape = Provider.GetShape<T>();
        Assert.NotNull(shape);
        return (Mapper.Create(shape, shape), StructuralEqualityComparer.Create(shape), shape);
    }
}

public sealed class MapperTests_Reflection : ObjectMapperTests
{
    protected override ITypeShapeProvider Provider { get; } = new ReflectionTypeShapeProvider(useReflectionEmit: false);
}

public sealed class MapperTests_ReflectionEmit : ObjectMapperTests
{
    protected override ITypeShapeProvider Provider { get; } = new ReflectionTypeShapeProvider(useReflectionEmit: true);
}

public sealed class MapperTests_SourceGen : ObjectMapperTests
{
    protected override ITypeShapeProvider Provider { get; } = SourceGenProvider.Default;
}
