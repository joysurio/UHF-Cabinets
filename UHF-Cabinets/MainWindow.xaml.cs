using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UHFCabinets.Navigation;

namespace UHFCabinets;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) => SyncMaximizeRestoreChrome();
        Loaded += (_, _) => SyncMaximizeRestoreChrome();
        if (App.Nav is NavigationService ns)
        {
            ns.SetNavigator(OnNavigate);
            RegisterPages(ns);
        }
        App.Nav.NavigateTo("Home");
    }

    private void RegisterPages(NavigationService nav)
    {
        nav.Register("Home", typeof(Views.HomePage));
        nav.Register("Login", typeof(Views.LoginPage));
        nav.Register("MainMenu", typeof(Views.MainMenuPage));
        nav.Register("Inventory", typeof(Views.InventoryPage));
        nav.Register("DispenseRestock", typeof(Views.DispenseRestockOperationPage));
        nav.Register("UnknownRfid", typeof(Views.UnknownRfidPage));
        nav.Register("OperationSummary", typeof(Views.OperationSummaryPage));
    }

    private void OnNavigate(object? payload)
    {
        if (payload is not NavigatePayload p)
            return;
        var page = (Page)Activator.CreateInstance(p.PageType)!;
        if (page is IRequireParameter r)
            r.SetParameter(p.Parameter);
        MainFrame.Navigate(page);
    }

    private void ChromeDragArea_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            e.Handled = true;
            return;
        }

        if (e.ClickCount != 1)
            return;

        try
        {
            DragMove();
        }
        catch
        {
            /* maximized or no drag surface */
        }
    }

    private void WindowChrome_Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void WindowChrome_MaximizeRestore_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void WindowChrome_Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    }

    private void SyncMaximizeRestoreChrome()
    {
        var maximized = WindowState == WindowState.Maximized;
        if (MaximizeGlyph != null && RestoreGlyph != null)
        {
            MaximizeGlyph.Visibility = maximized ? Visibility.Collapsed : Visibility.Visible;
            RestoreGlyph.Visibility = maximized ? Visibility.Visible : Visibility.Collapsed;
        }

        if (MaximizeRestoreButton != null)
            MaximizeRestoreButton.ToolTip = maximized ? "Restore down" : "Maximize";
    }
}

public interface IRequireParameter
{
    void SetParameter(object? parameter);
}
