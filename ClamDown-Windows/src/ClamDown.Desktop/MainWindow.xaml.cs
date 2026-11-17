using ClamDown.Core.IPC;
using ClamDown.Desktop.Hardware;
using ClamDown.Desktop.IPC;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClamDown.Desktop;

public sealed partial class MainWindow : Window
{
    private readonly LibreHardwareMonitorAdapter _monitor = new();
    private readonly ServiceClient _service = new();
    private readonly DispatcherQueueTimer _timer;
    private bool _sessionActive;
    private bool _serviceRequestInFlight;

    public MainWindow()
    {
        InitializeComponent();
        _timer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(2);
        _timer.IsRepeating = true;
        _timer.Tick += async (_, _) =>
        {
            RefreshTelemetry();
            await RefreshSessionStatusAsync();
        };
        _timer.Start();
        RefreshTelemetry();
        _ = RefreshSessionStatusAsync();
        Closed += (_, _) => _monitor.Dispose();
    }

    private void RefreshTelemetry()
    {
        try
        {
            var t = _monitor.Read();
            CpuValue.Text = t.CpuTemperatureC is { } cpu ? $"{cpu:F0} °C" : "N/A";
            GpuValue.Text = t.GpuTemperatureC is { } gpu ? $"{gpu:F0} °C" : "N/A";
            BatteryValue.Text = t.BatteryPercent is { } battery ? $"{battery}%" : "N/A";
            PowerValue.Text = t.IsOnAcPower switch
            {
                true => "AC power",
                false => "On battery",
                _ => "Power state unavailable"
            };
            ProviderValue.Text = t.HardwareProvider ?? "No provider";
            SensorList.Children.Clear();

            foreach (var fan in t.Fans)
            {
                SensorList.Children.Add(new TextBlock
                {
                    Text = $"{fan.Name}     {fan.Value:F0} RPM",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 255, 255, 255))
                });
            }

            if (t.Fans.Count == 0)
                SensorList.Children.Add(new TextBlock
                {
                    Text = "No fan tachometer readings exposed by this device.",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                        Windows.UI.Color.FromArgb(255, 169, 184, 200))
                });

            FooterStatus.Text = t.Warnings.Count == 0
                ? "Read-only sensor monitoring active. Fan control is disabled."
                : string.Join("  •  ", t.Warnings);
        }
        catch (Exception ex)
        {
            FooterStatus.Text = $"Sensor provider error: {ex.Message}";
        }
    }

    private async Task RefreshSessionStatusAsync()
    {
        if (_serviceRequestInFlight) return;
        _serviceRequestInFlight = true;
        try
        {
            var response = await _service.SendAsync(new ControlRequest("status"));
            ApplySessionResponse(response);
        }
        catch
        {
            SessionStatus.Text = "SERVICE OFFLINE · Install/start ClamDown Service";
            SessionButton.Content = "Service unavailable";
            SessionButton.IsEnabled = false;
        }
        finally { _serviceRequestInFlight = false; }
    }

    private void ApplySessionResponse(ControlResponse response)
    {
        _sessionActive = response.Success && response.State == "active";
        SessionStatus.Text = _sessionActive
            ? $"ACTIVE · Ends {response.EndsAt?.ToLocalTime():t} · Windows lid policy unchanged"
            : "OFF · No keep-awake request is active";
        SessionButton.Content = _sessionActive ? "Stop session" : "Start 60-minute session";
        SessionButton.IsEnabled = true;
    }

    private async void SessionButton_Click(object sender, RoutedEventArgs e)
    {
        SessionButton.IsEnabled = false;
        try
        {
            var request = _sessionActive
                ? new ControlRequest("stop")
                : new ControlRequest("start", 60 * 60);
            var response = await _service.SendAsync(request);
            ApplySessionResponse(response);

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = response.Success ? "ClamDown session" : "Could not change session",
                Content = response.Message +
                    (request.Command == "start"
                        ? "\n\nThis only requests that Windows remain awake. It does not override lid-close settings or provide thermal cutoffs. Monitor temperatures and use OEM protections."
                        : string.Empty),
                CloseButtonText = "OK"
            };
            _ = await dialog.ShowAsync();
        }
        catch (Exception ex)
        {
            SessionStatus.Text = "SERVICE OFFLINE · Session state unavailable";
            SessionButton.Content = "Service unavailable";
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "ClamDown service unavailable",
                Content = $"Could not contact the Windows service. Make sure ClamDown Service is installed and running.\n\n{ex.Message}",
                CloseButtonText = "OK"
            };
            _ = await dialog.ShowAsync();
        }
        finally
        {
            if (SessionButton.Content?.ToString() != "Service unavailable")
                SessionButton.IsEnabled = true;
        }
    }
}
