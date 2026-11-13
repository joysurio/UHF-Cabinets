namespace UHFCabinets.Navigation;

public interface INavigationService
{
    void NavigateTo(string pageKey, object? parameter = null);
    void GoBack();
}
