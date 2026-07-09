using System.Text.Json.Serialization;

namespace Ocpp.Core.Types;

/// <summary>7.27 IdTagInfo — status information about an identifier, returned in Authorize /
/// StartTransaction / StopTransaction responses.</summary>
public sealed class IdTagInfo
{
    /// <summary>Optional. Date at which idTag expires. After this the Charge Point should stop using it.</summary>
    public DateTimeOffset? ExpiryDate { get; set; }

    /// <summary>Optional. Parent idTag of the idTag.</summary>
    public string? ParentIdTag { get; set; }

    /// <summary>Required. Whether the idTag has been accepted or not by the Central System.</summary>
    public AuthorizationStatus Status { get; set; }
}

/// <summary>7.29 KeyValue — a configuration key/value pair.</summary>
public sealed class KeyValue
{
    /// <summary>Required. Name of the key.</summary>
    public string Key { get; set; } = "";

    /// <summary>Required. False if the value can be set with ChangeConfiguration.</summary>
    [JsonPropertyName("readonly")]
    public bool Readonly { get; set; }

    /// <summary>Optional. Value as string (RO keys may omit it).</summary>
    public string? Value { get; set; }
}

/// <summary>7.1 AuthorizationData — an entry of a local authorization list.</summary>
public sealed class AuthorizationData
{
    /// <summary>Required. The identifier to which this authorization applies.</summary>
    public string IdTag { get; set; } = "";

    /// <summary>Optional (required in a full update). Status information about the identifier.
    /// Absence in a differential update means the entry is deleted.</summary>
    public IdTagInfo? IdTagInfo { get; set; }
}
