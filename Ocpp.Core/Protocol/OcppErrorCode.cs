namespace Ocpp.Core.Protocol;

/// <summary>
/// Standard OCPP-J RPC error codes used in a CALLERROR frame (OCPP 1.6, section 4.2.3 of the
/// JSON transport specification). These describe RPC-level failures, not business results.
/// </summary>
public static class OcppErrorCode
{
    /// <summary>Requested Action is not known by receiver.</summary>
    public const string NotImplemented = "NotImplemented";

    /// <summary>Requested Action is recognized but not supported by the receiver.</summary>
    public const string NotSupported = "NotSupported";

    /// <summary>An internal error occurred and the receiver was not able to process the requested Action successfully.</summary>
    public const string InternalError = "InternalError";

    /// <summary>Payload for Action is incomplete.</summary>
    public const string ProtocolError = "ProtocolError";

    /// <summary>During the processing of Action a security issue occurred preventing receiver from completing the Action successfully.</summary>
    public const string SecurityError = "SecurityError";

    /// <summary>Payload for Action is syntactically incorrect or not conform the PDU structure for Action.</summary>
    public const string FormationViolation = "FormationViolation";

    /// <summary>Payload is syntactically correct but at least one field contains an invalid value.</summary>
    public const string PropertyConstraintViolation = "PropertyConstraintViolation";

    /// <summary>Payload for Action is syntactically correct but at least one of the fields violates occurrence constraints.</summary>
    public const string OccurenceConstraintViolation = "OccurenceConstraintViolation";

    /// <summary>Payload for Action is syntactically correct but at least one of the fields violates data type constraints (e.g. "somestring" = 12).</summary>
    public const string TypeConstraintViolation = "TypeConstraintViolation";

    /// <summary>Any other error not covered by the previous ones.</summary>
    public const string GenericError = "GenericError";
}
