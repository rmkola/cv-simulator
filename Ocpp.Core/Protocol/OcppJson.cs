using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ocpp.Core.Types;

namespace Ocpp.Core.Protocol;

/// <summary>
/// Shared JSON configuration and OCPP-J frame (de)serialization helpers.
/// OCPP field names are camelCase; optional fields are omitted when null.
/// </summary>
public static class OcppJson
{
    public static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Indented variant for human-readable logging.</summary>
    public static readonly JsonSerializerOptions PrettyOptions = CreateOptions(indented: true);

    private static JsonSerializerOptions CreateOptions(bool indented = false)
    {
        var o = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = indented,
        };
        // Emit timestamps as UTC with millisecond precision and a 'Z' suffix (e.g. 2026-07-10T05:34:52.013Z).
        o.Converters.Add(new UtcDateTimeOffsetConverter());
        o.Converters.Add(new EnumMemberJsonConverterFactory());
        return o;
    }

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>Builds a CALL frame: [2, uniqueId, action, payload].</summary>
    public static string SerializeCall(string uniqueId, string action, object payload)
    {
        var arr = new JsonArray(
            (int)MessageType.Call,
            uniqueId,
            action,
            PayloadNode(payload));
        return arr.ToJsonString(Options);
    }

    /// <summary>Builds a CALLRESULT frame: [3, uniqueId, payload].</summary>
    public static string SerializeCallResult(string uniqueId, object payload)
    {
        var arr = new JsonArray(
            (int)MessageType.CallResult,
            uniqueId,
            PayloadNode(payload));
        return arr.ToJsonString(Options);
    }

    /// <summary>Builds a CALLERROR frame: [4, uniqueId, errorCode, errorDescription, errorDetails].</summary>
    public static string SerializeCallError(string uniqueId, string errorCode, string errorDescription, string errorDetailsJson)
    {
        JsonNode details;
        try { details = JsonNode.Parse(string.IsNullOrWhiteSpace(errorDetailsJson) ? "{}" : errorDetailsJson) ?? new JsonObject(); }
        catch { details = new JsonObject(); }

        var arr = new JsonArray(
            (int)MessageType.CallError,
            uniqueId,
            errorCode,
            errorDescription ?? "",
            details);
        return arr.ToJsonString(Options);
    }

    private static JsonNode PayloadNode(object payload)
    {
        // Serialize the typed payload then reparse into a node so it can be embedded in the array.
        var json = JsonSerializer.Serialize(payload, payload.GetType(), Options);
        return JsonNode.Parse(json) ?? new JsonObject();
    }

    /// <summary>
    /// Parses a raw OCPP-J frame. Throws <see cref="OcppFormatException"/> when the outer array
    /// structure is malformed.
    /// </summary>
    public static OcppFrame ParseFrame(string raw)
    {
        JsonNode? root;
        try { root = JsonNode.Parse(raw); }
        catch (Exception ex) { throw new OcppFormatException("Frame is not valid JSON.", ex); }

        if (root is not JsonArray arr || arr.Count < 3)
            throw new OcppFormatException("Frame must be a JSON array with at least 3 elements.");

        int typeId;
        try { typeId = arr[0]!.GetValue<int>(); }
        catch (Exception ex) { throw new OcppFormatException("First element (MessageTypeId) must be a number.", ex); }

        var uniqueId = arr[1]?.GetValue<string>()
            ?? throw new OcppFormatException("Second element (uniqueId) must be a string.");

        switch ((MessageType)typeId)
        {
            case MessageType.Call:
                if (arr.Count < 4)
                    throw new OcppFormatException("CALL frame must have 4 elements.");
                return new OcppFrame
                {
                    MessageType = MessageType.Call,
                    UniqueId = uniqueId,
                    Action = arr[2]?.GetValue<string>() ?? throw new OcppFormatException("CALL action must be a string."),
                    Payload = arr[3],
                };

            case MessageType.CallResult:
                return new OcppFrame
                {
                    MessageType = MessageType.CallResult,
                    UniqueId = uniqueId,
                    Payload = arr[2],
                };

            case MessageType.CallError:
                if (arr.Count < 5)
                    throw new OcppFormatException("CALLERROR frame must have 5 elements.");
                return new OcppFrame
                {
                    MessageType = MessageType.CallError,
                    UniqueId = uniqueId,
                    ErrorCode = arr[2]?.GetValue<string>() ?? OcppErrorCode.GenericError,
                    ErrorDescription = arr[3]?.GetValue<string>() ?? "",
                    Payload = arr[4],
                };

            default:
                throw new OcppFormatException($"Unknown MessageTypeId {typeId}.");
        }
    }
}

/// <summary>A parsed OCPP-J frame independent of the source string's lifetime.</summary>
public sealed class OcppFrame
{
    public MessageType MessageType { get; init; }
    public string UniqueId { get; init; } = "";

    /// <summary>Action name (CALL only).</summary>
    public string? Action { get; init; }

    /// <summary>Payload node: request (CALL), result (CALLRESULT) or errorDetails (CALLERROR).</summary>
    public JsonNode? Payload { get; init; }

    /// <summary>Error code (CALLERROR only).</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Error description (CALLERROR only).</summary>
    public string? ErrorDescription { get; init; }

    public T? DeserializePayload<T>() =>
        Payload is null ? default : Payload.Deserialize<T>(OcppJson.Options);
}

/// <summary>Raised when an OCPP-J frame cannot be parsed at the transport/framing level.</summary>
public sealed class OcppFormatException : Exception
{
    public OcppFormatException(string message, Exception? inner = null) : base(message, inner) { }
}
