namespace HealthCheck.Web.Components.Shared.Feedback;

public sealed class LoadingState
{
    private int activeOperations;

    public bool IsLoading { get; private set; }
    public string Message { get; private set; } = "Carregando...";

    public event Action? OnChange;

    public void Show(string message = "Carregando...")
    {
        activeOperations++;
        IsLoading = true;
        Message = message;
        OnChange?.Invoke();
    }

    public void Hide()
    {
        if (activeOperations > 0)
            activeOperations--;

        IsLoading = activeOperations > 0;

        if (!IsLoading)
            Message = "Carregando...";

        OnChange?.Invoke();
    }
}
