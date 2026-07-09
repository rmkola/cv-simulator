namespace Ocpp.Core.Types;

/// <summary>7.43 SampledValue — a single sampled measurement value.</summary>
public sealed class SampledValue
{
    /// <summary>Required. Value as a "Raw" (decimal) number or "SignedData". Field type is string per spec.</summary>
    public string Value { get; set; } = "";

    /// <summary>Optional. Type of detail value: start, end or sample. Default = Sample.Periodic.</summary>
    public ReadingContext? Context { get; set; }

    /// <summary>Optional. Raw or signed data. Default = Raw.</summary>
    public ValueFormat? Format { get; set; }

    /// <summary>Optional. Type of measurement. Default = Energy.Active.Import.Register.</summary>
    public Measurand? Measurand { get; set; }

    /// <summary>Optional. Indicates how the measured value is to be interpreted (which phase(s)).</summary>
    public Phase? Phase { get; set; }

    /// <summary>Optional. Location of measurement. Default = Outlet.</summary>
    public Location? Location { get; set; }

    /// <summary>Optional. Unit of the value. Default = Wh if measurand is an energy value.</summary>
    public UnitOfMeasure? Unit { get; set; }
}

/// <summary>7.33 MeterValue — a collection of one or more sampled values taken at one point in time.</summary>
public sealed class MeterValue
{
    /// <summary>Required. Timestamp for measured value(s).</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Required. One or more measured values.</summary>
    public List<SampledValue> SampledValue { get; set; } = new();
}
