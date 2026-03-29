namespace OrderSphere.UI.Services;

public sealed class CartStateService
{
    public int ItemCount { get; private set; }
    public bool IsOpen { get; private set; }

    public event Action? OnChange;

    public void Toggle()
    {
        IsOpen = !IsOpen;
        OnChange?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        OnChange?.Invoke();
    }

    public void SetItemCount(int count)
    {
        ItemCount = count;
        OnChange?.Invoke();
    }
}