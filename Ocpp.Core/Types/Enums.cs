using System.Runtime.Serialization;

namespace Ocpp.Core.Types;

// OCPP 1.6 enumerations (spec section 7). Members whose wire value is not a valid C# identifier
// (dots, hyphens, mixed case) carry an [EnumMember] with the exact string; the rest serialize by name.
// See EnumMemberJsonConverter for the serialization behavior.

/// <summary>7.2 AuthorizationStatus.</summary>
public enum AuthorizationStatus { Accepted, Blocked, Expired, Invalid, ConcurrentTx }

/// <summary>7.3 AvailabilityStatus (ChangeAvailability.conf).</summary>
public enum AvailabilityStatus { Accepted, Rejected, Scheduled }

/// <summary>7.4 AvailabilityType (ChangeAvailability.req).</summary>
public enum AvailabilityType { Inoperative, Operative }

/// <summary>7.5 CancelReservationStatus.</summary>
public enum CancelReservationStatus { Accepted, Rejected }

/// <summary>7.6 ChargePointErrorCode.</summary>
public enum ChargePointErrorCode
{
    ConnectorLockFailure, EVCommunicationError, GroundFailure, HighTemperature, InternalError,
    LocalListConflict, NoError, OtherError, OverCurrentFailure, OverVoltage, PowerMeterFailure,
    PowerSwitchFailure, ReaderFailure, ResetFailure, UnderVoltage, WeakSignal
}

/// <summary>7.7 ChargePointStatus (connector status).</summary>
public enum ChargePointStatus
{
    Available, Preparing, Charging, SuspendedEVSE, SuspendedEV, Finishing, Reserved, Unavailable, Faulted
}

/// <summary>7.9 ChargingProfileKindType.</summary>
public enum ChargingProfileKindType { Absolute, Recurring, Relative }

/// <summary>7.10 ChargingProfilePurposeType.</summary>
public enum ChargingProfilePurposeType { ChargePointMaxProfile, TxDefaultProfile, TxProfile }

/// <summary>7.11 ChargingProfileStatus (SetChargingProfile.conf).</summary>
public enum ChargingProfileStatus { Accepted, Rejected, NotSupported }

/// <summary>7.12 ChargingRateUnitType.</summary>
public enum ChargingRateUnitType { W, A }

/// <summary>7.20 ClearCacheStatus.</summary>
public enum ClearCacheStatus { Accepted, Rejected }

/// <summary>7.21 ClearChargingProfileStatus.</summary>
public enum ClearChargingProfileStatus { Accepted, Unknown }

/// <summary>7.22 ConfigurationStatus (ChangeConfiguration.conf).</summary>
public enum ConfigurationStatus { Accepted, Rejected, RebootRequired, NotSupported }

/// <summary>7.23 DataTransferStatus.</summary>
public enum DataTransferStatus { Accepted, Rejected, UnknownMessageId, UnknownVendorId }

/// <summary>7.24 DiagnosticsStatus.</summary>
public enum DiagnosticsStatus { Idle, Uploaded, UploadFailed, Uploading }

/// <summary>7.25 FirmwareStatus.</summary>
public enum FirmwareStatus { Downloaded, DownloadFailed, Downloading, Idle, InstallationFailed, Installing, Installed }

/// <summary>7.26 GetCompositeScheduleStatus.</summary>
public enum GetCompositeScheduleStatus { Accepted, Rejected }

/// <summary>7.30 Location (SampledValue).</summary>
public enum Location { Body, Cable, EV, Inlet, Outlet }

/// <summary>7.31 Measurand.</summary>
public enum Measurand
{
    [EnumMember(Value = "Current.Export")] CurrentExport,
    [EnumMember(Value = "Current.Import")] CurrentImport,
    [EnumMember(Value = "Current.Offered")] CurrentOffered,
    [EnumMember(Value = "Energy.Active.Export.Register")] EnergyActiveExportRegister,
    [EnumMember(Value = "Energy.Active.Import.Register")] EnergyActiveImportRegister,
    [EnumMember(Value = "Energy.Reactive.Export.Register")] EnergyReactiveExportRegister,
    [EnumMember(Value = "Energy.Reactive.Import.Register")] EnergyReactiveImportRegister,
    [EnumMember(Value = "Energy.Active.Export.Interval")] EnergyActiveExportInterval,
    [EnumMember(Value = "Energy.Active.Import.Interval")] EnergyActiveImportInterval,
    [EnumMember(Value = "Energy.Reactive.Export.Interval")] EnergyReactiveExportInterval,
    [EnumMember(Value = "Energy.Reactive.Import.Interval")] EnergyReactiveImportInterval,
    [EnumMember(Value = "Frequency")] Frequency,
    [EnumMember(Value = "Power.Active.Export")] PowerActiveExport,
    [EnumMember(Value = "Power.Active.Import")] PowerActiveImport,
    [EnumMember(Value = "Power.Factor")] PowerFactor,
    [EnumMember(Value = "Power.Offered")] PowerOffered,
    [EnumMember(Value = "Power.Reactive.Export")] PowerReactiveExport,
    [EnumMember(Value = "Power.Reactive.Import")] PowerReactiveImport,
    [EnumMember(Value = "RPM")] RPM,
    [EnumMember(Value = "SoC")] SoC,
    [EnumMember(Value = "Temperature")] Temperature,
    [EnumMember(Value = "Voltage")] Voltage
}

/// <summary>7.32 MessageTrigger (TriggerMessage.req).</summary>
public enum MessageTrigger
{
    BootNotification, DiagnosticsStatusNotification, FirmwareStatusNotification, Heartbeat, MeterValues, StatusNotification
}

/// <summary>7.34 Phase.</summary>
public enum Phase
{
    L1, L2, L3, N,
    [EnumMember(Value = "L1-N")] L1N,
    [EnumMember(Value = "L2-N")] L2N,
    [EnumMember(Value = "L3-N")] L3N,
    [EnumMember(Value = "L1-L2")] L1L2,
    [EnumMember(Value = "L2-L3")] L2L3,
    [EnumMember(Value = "L3-L1")] L3L1
}

/// <summary>7.35 ReadingContext.</summary>
public enum ReadingContext
{
    [EnumMember(Value = "Interruption.Begin")] InterruptionBegin,
    [EnumMember(Value = "Interruption.End")] InterruptionEnd,
    [EnumMember(Value = "Other")] Other,
    [EnumMember(Value = "Sample.Clock")] SampleClock,
    [EnumMember(Value = "Sample.Periodic")] SamplePeriodic,
    [EnumMember(Value = "Transaction.Begin")] TransactionBegin,
    [EnumMember(Value = "Transaction.End")] TransactionEnd,
    [EnumMember(Value = "Trigger")] Trigger
}

/// <summary>7.36 Reason (StopTransaction.req).</summary>
public enum Reason
{
    DeAuthorized, EmergencyStop, EVDisconnected, HardReset, Local, Other, PowerLoss, Reboot, Remote, SoftReset, UnlockCommand
}

/// <summary>7.37 RecurrencyKindType.</summary>
public enum RecurrencyKindType { Daily, Weekly }

/// <summary>7.38 RegistrationStatus (BootNotification.conf).</summary>
public enum RegistrationStatus { Accepted, Pending, Rejected }

/// <summary>7.39 RemoteStartStopStatus.</summary>
public enum RemoteStartStopStatus { Accepted, Rejected }

/// <summary>7.40 ReservationStatus (ReserveNow.conf).</summary>
public enum ReservationStatus { Accepted, Faulted, Occupied, Rejected, Unavailable }

/// <summary>7.41 ResetStatus.</summary>
public enum ResetStatus { Accepted, Rejected }

/// <summary>7.42 ResetType.</summary>
public enum ResetType { Hard, Soft }

/// <summary>7.44 TriggerMessageStatus.</summary>
public enum TriggerMessageStatus { Accepted, Rejected, NotImplemented }

/// <summary>7.45 UnitOfMeasure.</summary>
public enum UnitOfMeasure
{
    Wh,
    [EnumMember(Value = "kWh")] KWh,
    [EnumMember(Value = "varh")] Varh,
    [EnumMember(Value = "kvarh")] Kvarh,
    W,
    [EnumMember(Value = "kW")] KW,
    VA,
    [EnumMember(Value = "kVA")] KVA,
    [EnumMember(Value = "var")] Var,
    [EnumMember(Value = "kvar")] Kvar,
    A, V, Celsius, Fahrenheit, K, Percent
}

/// <summary>7.46 UnlockStatus.</summary>
public enum UnlockStatus { Unlocked, UnlockFailed, NotSupported }

/// <summary>7.47 UpdateStatus (SendLocalList.conf).</summary>
public enum UpdateStatus { Accepted, Failed, NotSupported, VersionMismatch }

/// <summary>7.48 UpdateType (SendLocalList.req).</summary>
public enum UpdateType { Differential, Full }

/// <summary>7.49 ValueFormat (SampledValue).</summary>
public enum ValueFormat { Raw, SignedData }
