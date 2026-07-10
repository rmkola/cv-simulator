using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ocpp.Core.Types;

/// <summary>
/// Serializes timestamps as UTC ISO 8601 with millisecond precision and a 'Z' suffix
/// (e.g. <c>2026-07-10T05:34:52.013Z</c>), matching the format the other devices on the system emit.
/// On read, any offset (or 'Z') is normalized to UTC so stored values are always UTC+0.
/// Also applies to <see cref="Nullable{DateTimeOffset}"/>.
/// </summary>
public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();
        return DateTimeOffset.Parse(s!, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToUniversalTime().ToString(Format, CultureInfo.InvariantCulture));
}
