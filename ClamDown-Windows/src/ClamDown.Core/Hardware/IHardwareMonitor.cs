using ClamDown.Core.Models;

namespace ClamDown.Core.Hardware;

public interface IHardwareMonitor : IDisposable
{
    SystemTelemetry Read();
}
