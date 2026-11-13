using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Services;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class OperationSummaryPage : Page
{
    public OperationSummaryPage()
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

        SalidasText.Text = $"Exits (SALIDA): {OperationContext.SummarySalidas}";
        EntradasText.Text = $"Entries (ENTRADA): {OperationContext.SummaryEntradas}";
    }

    private void OnOk(object sender, RoutedEventArgs e)
    {
        OperationContext.ResetForNewOperation();
        App.Nav.NavigateTo("MainMenu");
    }
}
