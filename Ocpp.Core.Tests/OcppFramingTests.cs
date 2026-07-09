using System.Text.Json.Nodes;
using Ocpp.Core.Protocol;
using Xunit;

namespace Ocpp.Core.Tests;

public class OcppFramingTests
{
    private sealed class SamplePayload
    {
        public string CurrentTime { get; set; } = "";
        public int Interval { get; set; }
    }

    [Fact]
    public void SerializeCall_ProducesFourElementArray()
    {
        var raw = OcppJson.SerializeCall("abc123", "Heartbeat", new { });
        var arr = JsonNode.Parse(raw)!.AsArray();

        Assert.Equal(4, arr.Count);
        Assert.Equal(2, arr[0]!.GetValue<int>());
        Assert.Equal("abc123", arr[1]!.GetValue<string>());
        Assert.Equal("Heartbeat", arr[2]!.GetValue<string>());
    }

    [Fact]
    public void SerializeCallResult_ProducesThreeElementArray()
    {
        var raw = OcppJson.SerializeCallResult("id1", new SamplePayload { CurrentTime = "2026-07-09T00:00:00Z", Interval = 300 });
        var frame = OcppJson.ParseFrame(raw);

        Assert.Equal(MessageType.CallResult, frame.MessageType);
        Assert.Equal("id1", frame.UniqueId);
        var payload = frame.DeserializePayload<SamplePayload>()!;
        Assert.Equal(300, payload.Interval);
        Assert.Equal("2026-07-09T00:00:00Z", payload.CurrentTime);
    }

    [Fact]
    public void SerializeCallError_RoundTrips()
    {
        var raw = OcppJson.SerializeCallError("id2", OcppErrorCode.NotSupported, "nope", """{"hint":"x"}""");
        var frame = OcppJson.ParseFrame(raw);

        Assert.Equal(MessageType.CallError, frame.MessageType);
        Assert.Equal("id2", frame.UniqueId);
        Assert.Equal(OcppErrorCode.NotSupported, frame.ErrorCode);
        Assert.Equal("nope", frame.ErrorDescription);
    }

    [Fact]
    public void ParseFrame_ParsesCall()
    {
        var frame = OcppJson.ParseFrame("""[2,"u1","BootNotification",{"chargePointModel":"M","chargePointVendor":"V"}]""");

        Assert.Equal(MessageType.Call, frame.MessageType);
        Assert.Equal("u1", frame.UniqueId);
        Assert.Equal("BootNotification", frame.Action);
        Assert.Equal("M", frame.Payload!["chargePointModel"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("[2,\"id\"]")]
    [InlineData("[9,\"id\",\"x\"]")]
    public void ParseFrame_RejectsMalformed(string raw)
    {
        Assert.Throws<OcppFormatException>(() => OcppJson.ParseFrame(raw));
    }
}
