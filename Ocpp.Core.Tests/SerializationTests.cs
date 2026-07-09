using System.Text.Json;
using System.Text.Json.Nodes;
using Ocpp.Core.Messages;
using Ocpp.Core.Protocol;
using Ocpp.Core.Types;
using Xunit;

namespace Ocpp.Core.Tests;

public class SerializationTests
{
    private static JsonNode Node<T>(T value) =>
        JsonNode.Parse(JsonSerializer.Serialize(value, OcppJson.Options))!;

    [Fact]
    public void BootNotification_OmitsNullOptionalFields()
    {
        var node = Node(new BootNotificationRequest { ChargePointModel = "M1", ChargePointVendor = "Acme" });

        Assert.Equal("M1", node["chargePointModel"]!.GetValue<string>());
        Assert.Equal("Acme", node["chargePointVendor"]!.GetValue<string>());
        Assert.Null(node["firmwareVersion"]);   // null optional omitted
        Assert.Null(node["iccid"]);
    }

    [Fact]
    public void StatusNotification_UsesCamelCaseAndEnumNames()
    {
        var node = Node(new StatusNotificationRequest
        {
            ConnectorId = 1,
            ErrorCode = ChargePointErrorCode.NoError,
            Status = ChargePointStatus.Charging,
        });

        Assert.Equal(1, node["connectorId"]!.GetValue<int>());
        Assert.Equal("NoError", node["errorCode"]!.GetValue<string>());
        Assert.Equal("Charging", node["status"]!.GetValue<string>());
    }

    [Fact]
    public void KeyValue_SerializesReadonlyFieldLowercase()
    {
        var node = Node(new KeyValue { Key = "HeartbeatInterval", Readonly = false, Value = "300" });

        Assert.Equal("HeartbeatInterval", node["key"]!.GetValue<string>());
        Assert.False(node["readonly"]!.GetValue<bool>());
        Assert.Equal("300", node["value"]!.GetValue<string>());
    }

    [Fact]
    public void Measurand_UsesDottedWireValue()
    {
        var sv = new SampledValue { Value = "42", Measurand = Measurand.EnergyActiveImportRegister };
        var node = Node(sv);

        Assert.Equal("Energy.Active.Import.Register", node["measurand"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(Phase.L1N, "L1-N")]
    [InlineData(Phase.L3L1, "L3-L1")]
    public void Phase_UsesHyphenatedWireValue(Phase phase, string expected)
    {
        var node = Node(new SampledValue { Value = "1", Phase = phase });
        Assert.Equal(expected, node["phase"]!.GetValue<string>());
    }

    [Theory]
    [InlineData(UnitOfMeasure.KWh, "kWh")]
    [InlineData(UnitOfMeasure.Kvarh, "kvarh")]
    [InlineData(UnitOfMeasure.KVA, "kVA")]
    [InlineData(UnitOfMeasure.Var, "var")]
    public void UnitOfMeasure_PreservesExactCasing(UnitOfMeasure unit, string expected)
    {
        var node = Node(new SampledValue { Value = "1", Unit = unit });
        Assert.Equal(expected, node["unit"]!.GetValue<string>());
    }

    [Fact]
    public void SpecialEnums_RoundTrip()
    {
        var original = new SampledValue
        {
            Value = "3.14",
            Measurand = Measurand.PowerActiveImport,
            Phase = Phase.L2N,
            Context = ReadingContext.SamplePeriodic,
            Unit = UnitOfMeasure.KW,
            Location = Location.Outlet,
            Format = ValueFormat.Raw,
        };

        var json = JsonSerializer.Serialize(original, OcppJson.Options);
        var back = JsonSerializer.Deserialize<SampledValue>(json, OcppJson.Options)!;

        Assert.Equal(original.Measurand, back.Measurand);
        Assert.Equal(original.Phase, back.Phase);
        Assert.Equal(original.Context, back.Context);
        Assert.Equal(original.Unit, back.Unit);
    }

    [Fact]
    public void ChargingProfile_NestedSchedule_RoundTrips()
    {
        var profile = new ChargingProfile
        {
            ChargingProfileId = 1,
            StackLevel = 0,
            ChargingProfilePurpose = ChargingProfilePurposeType.TxProfile,
            ChargingProfileKind = ChargingProfileKindType.Absolute,
            ChargingSchedule = new ChargingSchedule
            {
                ChargingRateUnit = ChargingRateUnitType.A,
                ChargingSchedulePeriod =
                {
                    new ChargingSchedulePeriod { StartPeriod = 0, Limit = 16m, NumberPhases = 3 },
                    new ChargingSchedulePeriod { StartPeriod = 3600, Limit = 8m },
                },
            },
        };

        var json = JsonSerializer.Serialize(profile, OcppJson.Options);
        var back = JsonSerializer.Deserialize<ChargingProfile>(json, OcppJson.Options)!;

        Assert.Equal(ChargingProfilePurposeType.TxProfile, back.ChargingProfilePurpose);
        Assert.Equal(2, back.ChargingSchedule.ChargingSchedulePeriod.Count);
        Assert.Equal(16m, back.ChargingSchedule.ChargingSchedulePeriod[0].Limit);
        Assert.Equal(3, back.ChargingSchedule.ChargingSchedulePeriod[0].NumberPhases);
        Assert.Null(back.ChargingSchedule.ChargingSchedulePeriod[1].NumberPhases);
    }

    [Fact]
    public void FullCallFrame_ForBootNotification_ParsesBack()
    {
        var raw = OcppJson.SerializeCall("m1", OcppAction.BootNotification,
            new BootNotificationRequest { ChargePointModel = "Model-X", ChargePointVendor = "CW" });

        var frame = OcppJson.ParseFrame(raw);
        var payload = frame.DeserializePayload<BootNotificationRequest>()!;

        Assert.Equal(OcppAction.BootNotification, frame.Action);
        Assert.Equal("Model-X", payload.ChargePointModel);
        Assert.Equal("CW", payload.ChargePointVendor);
    }
}
