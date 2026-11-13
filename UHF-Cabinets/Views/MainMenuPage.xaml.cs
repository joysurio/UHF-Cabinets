using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class MainMenuPage : Page
{
    public MainMenuPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAuthenticated)
        {
            App.Nav.NavigateTo("Home");
            return;
        }

        WelcomeText.Text = string.IsNullOrEmpty(AppSession.Role)
            ? $"Welcome, {AppSession.DisplayName}"
            : $"Welcome, {AppSession.DisplayName} ({AppSession.Role})";
    }

    private void OnInventory(object sender, RoutedEventArgs e) => App.Nav.NavigateTo("Inventory");

    private void OnDispense(object sender, RoutedEventArgs e) => App.Nav.NavigateTo("DispenseRestock");

    private void OnLogout(object sender, RoutedEventArgs e)
    {
        AppSession.Clear();
        App.Nav.NavigateTo("Home");
    }
}
