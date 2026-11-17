namespace ClamDown.Core.Hardware;

public sealed record HardwareCapabilities(
    bool CpuTemperatureAvailable,
    bool GpuTemperatureAvailable,
    bool FanReadingsAvailable,
    bool FanWriteSupported,
    string Provider,
    IReadOnlyList<string> Notes);
