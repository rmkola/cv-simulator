namespace Ocpp.Core.Domain;

/// <summary>A charging transaction. The transactionId is assigned by the Central System in the
/// StartTransaction.conf response.</summary>
public sealed class Transaction
{
    public int TransactionId { get; set; }
    public int ConnectorId { get; }
    public string IdTag { get; }
    public int MeterStart { get; }
    public DateTimeOffset StartTime { get; }

    public int? MeterStop { get; set; }
    public DateTimeOffset? StopTime { get; set; }

    public bool IsActive => StopTime is null;

    public Transaction(int connectorId, string idTag, int meterStart, DateTimeOffset startTime)
    {
        ConnectorId = connectorId;
        IdTag = idTag;
        MeterStart = meterStart;
        StartTime = startTime;
    }
}
