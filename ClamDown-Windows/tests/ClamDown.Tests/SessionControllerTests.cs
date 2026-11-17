using ClamDown.Core.Power;
using ClamDown.Service;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClamDown.Tests;

public sealed class SessionControllerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(59)]
    [InlineData(14401)]
    public void Start_RejectsDurationsOutsideBounds(int seconds)
    {
        var power = new FakePowerRequest();
        var controller = new SessionController(power, NullLogger<SessionController>.Instance);

        var started = controller.Start(seconds, out var endsAt, out _);

        Assert.False(started);
        Assert.Null(endsAt);
        Assert.False(power.IsActive);
    }

    [Fact]
    public void StartAndStop_ManagePowerRequest()
    {
        var power = new FakePowerRequest();
        var controller = new SessionController(power, NullLogger<SessionController>.Instance);

        var started = controller.Start(60, out var endsAt, out _);

        Assert.True(started);
        Assert.True(power.IsActive);
        Assert.NotNull(endsAt);
        Assert.True(controller.Snapshot().Active);

        controller.Stop();

        Assert.False(power.IsActive);
        Assert.False(controller.Snapshot().Active);
    }

    private sealed class FakePowerRequest : IPowerRequest
    {
        public bool IsActive { get; private set; }
        public void KeepSystemAwake(string reason) => IsActive = true;
        public void Release() => IsActive = false;
        public void Dispose() => Release();
    }
}
