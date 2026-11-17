# WinDown — Windows 11 port of ClamDown

This repository contains the Windows port starter project as a compressed source archive.

## Reconstruct the project

The archive is stored as five base64 text parts so it can be committed through the Git data API without binary-upload support in this connection.

Run PowerShell from the repository root:

```powershell
Set-ExecutionPolicy -Scope Process Bypass
.\rebuild-source.ps1
```

The script concatenates the parts, decodes the gzip stream, restores the ZIP archive, and extracts the project into `ClamDown-Windows\`.

## Scope and safety status

- .NET solution split into Core, Service, Desktop (WinUI 3), and tests
- Bounded keep-awake sessions using the Windows power-request API
- Named-pipe service control for Start / Stop / Status
- Read-only hardware telemetry via LibreHardwareMonitor
- No embedded-controller writes or fan-speed control
- Does not override Windows lid-close policy
- Not production-ready: thermal/battery safety interlocks are not yet wired into the privileged service, the named-pipe ACL needs client authorization hardening, and Windows build/test validation has not been run

See the extracted project's README files before building or installing.
