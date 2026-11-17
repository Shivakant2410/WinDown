namespace ClamDown.Core.Power;

public interface IPowerRequest : IDisposable
{
    void KeepSystemAwake(string reason);
    void Release();
}
