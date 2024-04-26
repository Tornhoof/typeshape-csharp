﻿using System.Buffers;
using BenchmarkDotNet.Attributes;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using TypeShape;
using TypeShape.Applications.JsonSerializer;
using TypeShape.ReflectionProvider;

public static class JsonData
{
    public static readonly MyPoco Value = new MyPoco(@string: "myString")
    {
        List = [1, 2, 3],
        Dict = new() { ["key1"] = 42, ["key2"] = -1 },
    };

    public static readonly byte[] Utf8JsonValue = JsonSerializer.SerializeToUtf8Bytes(Value);

    public static readonly JsonTypeInfo<MyPoco> StjReflectionInfo = (JsonTypeInfo<MyPoco>)JsonSerializerOptions.Default.GetTypeInfo(typeof(MyPoco));
    public static readonly JsonTypeInfo<MyPoco> StjSourceGenInfo = StjContext.Default.MyPoco;
    public static readonly JsonTypeInfo<MyPoco> StjSourceGenInfo_fastPath = StjContext_FastPath.Default.MyPoco;

    public static readonly TypeShapeJsonConverter<MyPoco> TypeShapeReflection = TypeShapeJsonSerializer.CreateConverter<MyPoco>(ReflectionTypeShapeProvider.Default);
    public static readonly TypeShapeJsonConverter<MyPoco> TypeShapeSourceGen = TypeShapeJsonSerializer.CreateConverter<MyPoco>();
}

[MemoryDiagnoser]
public class JsonSerializeBenchmark
{
    private readonly ArrayBufferWriter<byte> _bufferWriter;
    private readonly Utf8JsonWriter _writer;
    
    public JsonSerializeBenchmark()
    {
        _bufferWriter = new();
        _writer = new(_bufferWriter);
    }

    [Benchmark(Baseline = true)]
    public void Serialize_StjReflection()
    {
        JsonSerializer.Serialize(_writer, JsonData.Value, JsonData.StjSourceGenInfo);
        Reset();
    }

    [Benchmark]
    public void Serialize_StjSourceGen()
    {
        JsonSerializer.Serialize(_writer, JsonData.Value, JsonData.StjSourceGenInfo);
        Reset();
    }

    [Benchmark]
    public void Serialize_StjSourceGen_FastPath()
    {
        JsonSerializer.Serialize(_writer, JsonData.Value, JsonData.StjSourceGenInfo_fastPath);
        Reset();
    }

    [Benchmark]
    public void Serialize_TypeShapeReflection()
    {
        JsonData.TypeShapeReflection.Write(_writer, JsonData.Value);
        Reset();
    }

    [Benchmark]
    public void Serialize_TypeShapeSourceGen()
    {
        JsonData.TypeShapeSourceGen.Write(_writer, JsonData.Value);
        Reset();
    }
    
    public void Reset()
    {
        _bufferWriter.ResetWrittenCount();
        _writer.Reset();
    }
}

[MemoryDiagnoser]
public class JsonDeserializeBenchmark
{
    [Benchmark(Baseline = true)]
    public MyPoco? Deserialize_StjReflection()
        => JsonSerializer.Deserialize(JsonData.Utf8JsonValue, JsonData.StjReflectionInfo);

    [Benchmark]
    public MyPoco? Deserialize_StjSourceGen()
        => JsonSerializer.Deserialize(JsonData.Utf8JsonValue, JsonData.StjSourceGenInfo);

    [Benchmark]
    public MyPoco? Deserialize_TypeShapeReflection()
        => JsonData.TypeShapeReflection.Deserialize(JsonData.Utf8JsonValue);

    [Benchmark]
    public MyPoco? Deserialize_TypeShapeSourceGen()
        => JsonData.TypeShapeSourceGen.Deserialize(JsonData.Utf8JsonValue);
}

[GenerateShape]
public partial class MyPoco
{
    public MyPoco(bool @bool = true, string @string = "str")
    {
        Bool = @bool;
        String = @string;
    }

    public bool Bool { get; }
    public string String { get; }
    public List<int>? List { get; set; }
    public Dictionary<string, int>? Dict { get; set; }
}

[JsonSerializable(typeof(MyPoco), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class StjContext : JsonSerializerContext;

[JsonSerializable(typeof(MyPoco), GenerationMode = JsonSourceGenerationMode.Serialization)]
public partial class StjContext_FastPath : JsonSerializerContext;
