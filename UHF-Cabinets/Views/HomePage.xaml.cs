using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UHFCabinets.Views;

public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
    }

    private void OnTouchStart(object sender, RoutedEventArgs e)
    {
        App.Nav.NavigateTo("Login");
    }
}
