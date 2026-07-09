namespace Ocpp.Core.Protocol;

/// <summary>
/// OCPP-J RPC framing message type identifiers (first element of the wire array).
/// </summary>
public enum MessageType
{
    /// <summary>Request from sender to receiver: [2, uniqueId, action, payload].</summary>
    Call = 2,

    /// <summary>Successful response: [3, uniqueId, payload].</summary>
    CallResult = 3,

    /// <summary>Error response: [4, uniqueId, errorCode, errorDescription, errorDetails].</summary>
    CallError = 4,
}
