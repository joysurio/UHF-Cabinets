namespace UHFCabinets.Navigation;

public class NavigationService : INavigationService
{
    private readonly Dictionary<string, Type> _pages = new();
    private readonly Stack<(string Key, object?)> _history = new();
    private Action<object?>? _navigate;

    public void Register(string key, Type pageType) => _pages[key] = pageType;
    public void SetNavigator(Action<object?> navigate) => _navigate = navigate;

    public void NavigateTo(string pageKey, object? parameter = null)
    {
        if (!_pages.TryGetValue(pageKey, out var type))
            return;
        _navigate?.Invoke(new NavigatePayload { PageType = type, Parameter = parameter });
    }

    public void GoBack()
    {
        if (_history.Count > 0)
        {
            var (key, param) = _history.Pop();
            NavigateTo(key, param);
        }
    }

    public void PushHistory(string key, object? parameter) => _history.Push((key, parameter));
}

public class NavigatePayload
{
    public Type PageType { get; set; } = null!;
    public object? Parameter { get; set; }
}
