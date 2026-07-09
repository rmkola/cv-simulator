namespace Ocpp.Core.Protocol;

/// <summary>
/// Thrown by an incoming-call handler to make the client answer with a CALLERROR frame instead
/// of a CALLRESULT. Also raised locally when a remote peer answers one of our calls with a CALLERROR.
/// </summary>
public sealed class OcppCallErrorException : Exception
{
    public string ErrorCode { get; }
    public string ErrorDescription { get; }
    public string ErrorDetailsJson { get; }

    public OcppCallErrorException(
        string errorCode,
        string errorDescription = "",
        string errorDetailsJson = "{}")
        : base($"{errorCode}: {errorDescription}")
    {
        ErrorCode = errorCode;
        ErrorDescription = errorDescription ?? "";
        ErrorDetailsJson = string.IsNullOrWhiteSpace(errorDetailsJson) ? "{}" : errorDetailsJson;
    }
}
