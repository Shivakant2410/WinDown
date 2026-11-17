# ClamDown Desktop

WinUI 3 dashboard for read-only hardware telemetry and service-backed, bounded
keep-awake control. The Start button requests a 60-minute session; Stop releases
the Windows execution-state request. The service also enforces a four-hour
maximum and releases the request on expiry or service shutdown.

The dashboard does not change lid-close policy. Thermal/battery cutoffs are not
yet enforced in the service, so this build is not a thermal safety controller.
