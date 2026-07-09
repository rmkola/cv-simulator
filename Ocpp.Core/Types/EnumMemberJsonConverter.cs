using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocpp.Core.Types;

/// <summary>
/// Serializes enums as strings, honoring <see cref="EnumMemberAttribute"/> so OCPP enum values
/// that are not valid C# identifiers (e.g. <c>Energy.Active.Import.Register</c>, <c>L1-N</c>,
/// <c>SoC</c>) round-trip correctly. Enum members without the attribute use their C# name.
/// Applied globally via <see cref="EnumMemberJsonConverterFactory"/>.
/// </summary>
public sealed class EnumMemberJsonConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    private static readonly Dictionary<TEnum, string> ToWire = new();
    private static readonly Dictionary<string, TEnum> FromWire = new(StringComparer.Ordinal);

    static EnumMemberJsonConverter()
    {
        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var value = (TEnum)field.GetValue(null)!;
            var wire = field.GetCustomAttribute<EnumMemberAttribute>()?.Value ?? field.Name;
            ToWire[value] = wire;
            FromWire[wire] = value;
        }
    }

    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var raw = reader.GetString();
        if (raw is not null && FromWire.TryGetValue(raw, out var value))
            return value;

        throw new JsonException($"'{raw}' is not a valid value for enum {typeof(TEnum).Name}.");
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        if (ToWire.TryGetValue(value, out var wire))
            writer.WriteStringValue(wire);
        else
            writer.WriteStringValue(value.ToString());
    }
}

/// <summary>Applies <see cref="EnumMemberJsonConverter{TEnum}"/> to every enum type.</summary>
public sealed class EnumMemberJsonConverterFactory : JsonConverterFactory
{
    private static readonly ConcurrentDictionary<Type, JsonConverter> Cache = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
        Cache.GetOrAdd(typeToConvert, static t =>
            (JsonConverter)Activator.CreateInstance(
                typeof(EnumMemberJsonConverter<>).MakeGenericType(t))!);
}
