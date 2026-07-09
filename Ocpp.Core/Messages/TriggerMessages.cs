using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Remote Trigger profile (spec 6.51 / 6.52) ----

public sealed class TriggerMessageRequest
{
    public MessageTrigger RequestedMessage { get; set; }
    public int? ConnectorId { get; set; }
}

public sealed class TriggerMessageResponse
{
    public TriggerMessageStatus Status { get; set; }
}
