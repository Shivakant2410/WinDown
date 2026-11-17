# ClamDown for Windows

A Windows 11-native companion project inspired by the macOS ClamDown project.
This is an **architecture and implementation starter**, not a production-ready
lid-control or fan-control utility.

## Design

- `ClamDown.Core`: shared contracts, session policy, safety evaluation.
- `ClamDown.Service`: Windows background service; owns power requests and
  supervises session state.
- `ClamDown.Desktop`: WinUI 3 front end (initial shell).
- `ClamDown.Tests`: unit tests for safety policy.

## Important Windows limitations

1. `PowerCreateRequest` / `PowerSetRequest` request that Windows does not idle
   sleep while work is active. They do **not** guarantee that a laptop ignores
   its lid-close action. Configure **Control Panel → Power Options → Choose
   what closing the lid does → Do nothing** if you explicitly want closed-lid
   operation. OEM firmware and enterprise policy may still override this.
2. Fan control is not standardized across Intel, AMD, NVIDIA, and laptop OEMs.
   The first release should monitor sensors and expose fan control only through
   a positively identified, tested vendor adapter. Do not write arbitrary EC
   registers or disable firmware thermal protection.
3. Keep the laptop on a hard, ventilated surface. Never run a closed-lid
   high-load session in a bag or enclosed space.
4. This starter deliberately does not change lid policy, write fan speeds, or
   install a privileged IPC endpoint. Those require explicit consent, device
   qualification, and security review.

## Requirements

- Windows 11 x64
- .NET 10 SDK
- Visual Studio 2026 with the Windows App SDK / WinUI workload for the desktop UI

## Build

```powershell
dotnet restore ClamDown.sln
dotnet test ClamDown.sln
dotnet build src/ClamDown.Service/ClamDown.Service.csproj -c Release
```

Open `src/ClamDown.Desktop/ClamDown.Desktop.csproj` in Visual Studio to build
the WinUI 3 desktop shell.

## Current implementation

- Safe system power request wrapper with deterministic release.
- Session lifecycle and thermal/battery safety policy.
- Background service skeleton.
- WinUI 3 shell placeholder.
- Unit tests for cut-off decisions.

## Planned milestones

1. Secure named-pipe IPC with Windows ACLs and client identity validation.
2. Battery and thermal sensor providers with freshness/validity metadata.
3. Lid-state detection and guided configuration (no silent policy changes).
4. Vendor-qualified fan adapters with read-only discovery first.
5. Watchdog, crash recovery journal, installer, signing and hardware test matrix.

## Hardware/UI milestone update

The desktop project now includes a WinUI 3 dashboard shell with read-only
LibreHardwareMonitor telemetry for CPU/GPU temperatures and fan tachometers,
plus Windows battery/AC status via GetSystemPowerStatus. It targets Windows 11
x64 and currently prioritizes Intel CPUs and NVIDIA GPUs, while enabling
LibreHardwareMonitor's AMD sensor discovery too.

The Start Session control is deliberately disabled behind an explanatory
dialog: service IPC and durable session recovery are not yet implemented.
No fan write operations or lid-policy changes are made by this preview.

Sensor availability varies by OEM firmware, GPU power state, permissions and
hardware generation. Missing sensors are displayed as unavailable, not zero.
