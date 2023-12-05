namespace Frontend.Services;

public class LiveUpdateFrontEndMessenger
{
    public event Action<Status>? OnStatusChanged;

    public void Notify(Status status)
    {
        OnStatusChanged?.Invoke(status);
    }
}
