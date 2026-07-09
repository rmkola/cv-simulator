namespace Ocpp.Core.Protocol;

public enum LogDirection
{
    /// <summary>Frame sent by this endpoint.</summary>
    Outgoing,

    /// <summary>Frame received from the peer.</summary>
    Incoming,

    /// <summary>Local informational / diagnostic line (connect, disconnect, errors).</summary>
    Info,
}

/// <summary>One line in the OCPP traffic log.</summary>
public sealed record OcppLogEntry(
    DateTimeOffset Timestamp,
    LogDirection Direction,
    MessageType? MessageType,
    string? Action,
    string? UniqueId,
    string Raw)
{
    public static OcppLogEntry Information(string message) =>
        new(DateTimeOffset.UtcNow, LogDirection.Info, null, null, null, message);
}
