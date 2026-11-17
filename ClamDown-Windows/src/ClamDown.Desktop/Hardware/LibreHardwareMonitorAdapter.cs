using ClamDown.Core.Hardware;
using ClamDown.Core.Models;
using LibreHardwareMonitor.Hardware;

namespace ClamDown.Desktop.Hardware;

/// <summary>
/// Read-only sensor adapter. Fan write/control is intentionally not implemented.
/// Sensor values are reported only when the provider returns a fresh reading.
/// </summary>
public sealed class LibreHardwareMonitorAdapter : IHardwareMonitor
{
    private readonly Computer _computer;

    public LibreHardwareMonitorAdapter()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };
        _computer.Open();
    }

    public SystemTelemetry Read()
    {
        var now = DateTimeOffset.UtcNow;
        double? cpu = null;
        double? gpu = null;
        var fans = new List<SensorReading>();
        var warnings = new List<string>();

        foreach (var hardware in _computer.Hardware)
        {
            try
            {
                hardware.Update();
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.Value is not float value || !float.IsFinite(value))
                        continue;

                    if (sensor.SensorType == SensorType.Temperature)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu &&
                            sensor.Name.Contains("CPU Package", StringComparison.OrdinalIgnoreCase))
                            cpu = value;

                        if ((hardware.HardwareType == HardwareType.GpuNvidia ||
                             hardware.HardwareType == HardwareType.GpuAmd) &&
                            gpu is null)
                            gpu = value;
                    }

                    if (sensor.SensorType == SensorType.Fan)
                    {
                        fans.Add(new SensorReading(
                            $"{hardware.Identifier}/{sensor.Identifier}",
                            $"{hardware.Name} · {sensor.Name}",
                            value,
                            "RPM",
                            now,
                            true,
                            "LibreHardwareMonitor"));
                    }
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Could not update {hardware.Name}: {ex.Message}");
            }
        }

        var battery = ReadBatteryPercent();
        var onAc = ReadAcState();

        if (cpu is null)
            warnings.Add("CPU temperature is unavailable. Sensor exposure depends on laptop/board firmware.");
        if (gpu is null)
            warnings.Add("GPU temperature is unavailable or the GPU is asleep.");

        return new SystemTelemetry(
            now, battery, onAc, cpu, gpu, fans,
            "LibreHardwareMonitor (read-only)",
            warnings);
    }

    private static int? ReadBatteryPercent()
    {
        try
        {
            var status = NativeMethods.GetSystemPowerStatus(out var s);
            return status ? (s.BatteryLifePercent == 255 ? null : s.BatteryLifePercent) : null;
        }
        catch { return null; }
    }

    private static bool? ReadAcState()
    {
        try
        {
            if (!NativeMethods.GetSystemPowerStatus(out var s)) return null;
            return s.ACLineStatus switch { 0 => false, 1 => true, _ => null };
        }
        catch { return null; }
    }

    public void Dispose() => _computer.Close();

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        internal struct SYSTEM_POWER_STATUS
        {
            public byte ACLineStatus;
            public byte BatteryFlag;
            public byte BatteryLifePercent;
            public byte SystemStatusFlag;
            public uint BatteryLifeTime;
            public uint BatteryFullLifeTime;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        internal static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);
    }
}
