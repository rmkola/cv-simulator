using Ocpp.Core.Types;

namespace Ocpp.Core.Messages;

// ---- Local Auth List Management profile (spec 6.x) ----

// 6.27 / 6.28 GetLocalListVersion
public sealed class GetLocalListVersionRequest { }

public sealed class GetLocalListVersionResponse
{
    public int ListVersion { get; set; }
}

// 6.41 / 6.42 SendLocalList
public sealed class SendLocalListRequest
{
    public int ListVersion { get; set; }
    public List<AuthorizationData>? LocalAuthorizationList { get; set; }
    public UpdateType UpdateType { get; set; }
}

public sealed class SendLocalListResponse
{
    public UpdateStatus Status { get; set; }
}
