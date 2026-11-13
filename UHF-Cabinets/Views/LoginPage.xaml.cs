using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class LoginPage : Page
{
    private System.Timers.Timer? _timeoutTimer;
    private bool _passwordVisible;

    public LoginPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _timeoutTimer = new System.Timers.Timer(App.Settings.LoginTimeoutSeconds * 1000);
        _timeoutTimer.Elapsed += (_, _) => Dispatcher.Invoke(() => App.Nav.NavigateTo("Home"));
        _timeoutTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _timeoutTimer?.Stop();
        _timeoutTimer?.Dispose();
        PasswordPlainBox.Clear();
        _passwordVisible = false;
    }

    private void OnTogglePasswordVisibility(object sender, RoutedEventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        if (_passwordVisible)
        {
            PasswordPlainBox.Text = PasswordBox.Password;
            PasswordPlainBox.Visibility = Visibility.Visible;
            PasswordBox.Visibility = Visibility.Collapsed;
            EyeRevealGlyph.Visibility = Visibility.Collapsed;
            EyeHideGlyph.Visibility = Visibility.Visible;
            TogglePasswordVisibilityButton.ToolTip = "Hide password";
            System.Windows.Automation.AutomationProperties.SetName(TogglePasswordVisibilityButton, "Hide password");
            PasswordPlainBox.Focus();
            PasswordPlainBox.CaretIndex = PasswordPlainBox.Text.Length;
        }
        else
        {
            PasswordBox.Password = PasswordPlainBox.Text;
            PasswordBox.Visibility = Visibility.Visible;
            PasswordPlainBox.Visibility = Visibility.Collapsed;
            EyeRevealGlyph.Visibility = Visibility.Visible;
            EyeHideGlyph.Visibility = Visibility.Collapsed;
            TogglePasswordVisibilityButton.ToolTip = "Show password";
            System.Windows.Automation.AutomationProperties.SetName(TogglePasswordVisibilityButton, "Show password");
            PasswordPlainBox.Clear();
            PasswordBox.Focus();
        }
    }

    private string GetPassword() =>
        _passwordVisible ? PasswordPlainBox.Text : PasswordBox.Password;

    private async void OnSignIn(object sender, RoutedEventArgs e)
    {
        _timeoutTimer?.Stop();
        var user = UserNameBox.Text.Trim();
        var pass = GetPassword();
        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            StatusText.Text = "Enter username and password.";
            StatusText.Visibility = Visibility.Visible;
            return;
        }

        try
        {
            var (ok, userId, name, role, err) = await App.Gateway.LoginAsync(user, pass);
            if (!ok || userId == null)
            {
                StatusText.Text = err ?? "Invalid login.";
                StatusText.Visibility = Visibility.Visible;
                return;
            }

            AppSession.Set(userId, name ?? userId, role);
            try
            {
                await App.Gateway.RefreshProductsCatalogCacheAsync();
            }
            catch
            {
                // catalog optional; unknown-RFID flow can use cache or retry
            }

            await App.OutboundSync.FlushPendingAsync();
            App.Nav.NavigateTo("MainMenu");
        }
        catch (Exception ex)
        {
            StatusText.Text = "API unavailable: " + ex.Message;
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
