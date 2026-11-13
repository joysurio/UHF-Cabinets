using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Services;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class DispenseRestockOperationPage : Page
{
    public DispenseRestockOperationPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!AppSession.IsAuthenticated)
        {
            App.Nav.NavigateTo("Home");
            return;
        }

        TestPanel.Visibility = App.Settings.TestMode ? Visibility.Visible : Visibility.Collapsed;
        await StartOperationAsync();
    }

    private async Task StartOperationAsync()
    {
        OperationContext.ResetForNewOperation();
        OperationContext.IdMov = $"MOV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";
        foreach (var r in App.Local.GetLastScanRfids())
            OperationContext.BeforeSnapshot.Add(r);

        PhaseTitle.Text = "Opening doors";
        PhaseDetail.Text = App.Settings.TestMode
            ? "Door open requested. When ready, tap \"Simulate: Door closed\"."
            : "Requesting door open from external module.";

        try
        {
            await App.Door.RequestOpenDoorAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Door module", MessageBoxButton.OK, MessageBoxImage.Warning);
            App.Nav.NavigateTo("MainMenu");
            return;
        }

        try
        {
            await App.Door.WaitForDoorClosedAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Door module", MessageBoxButton.OK, MessageBoxImage.Warning);
            App.Nav.NavigateTo("MainMenu");
            return;
        }

        if (App.Settings.TestMode)
        {
            PhaseTitle.Text = "Reading RFID";
            PhaseDetail.Text = "Enter detected tag IDs (one per line), then tap Simulate RFID read.";
            return;
        }

        PhaseTitle.Text = "Reading RFID";
        PhaseDetail.Text = "Reading tags from the configured reader (ClsLectorUHF / hardware bridge).";
        await CompleteAfterRfidReadAsync();
    }

    private void OnSimulateDoorOpened(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Door opening event simulated (test mode).", "Test mode", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void OnSimulateDoorClosed(object sender, RoutedEventArgs e)
    {
        App.TestDoor?.SimulateDoorClosed();
    }

    private async void OnSimulateRfidRead(object sender, RoutedEventArgs e)
    {
        if (App.TestRfid == null) return;
        var lines = RfidLinesBox.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        App.TestRfid.SetSimulatedTags(lines);
        await CompleteAfterRfidReadAsync();
    }

    private async Task CompleteAfterRfidReadAsync()
    {
        try
        {
            var tags = await App.Rfid.ReadTagsAsync();
            OperationContext.AfterSnapshot.Clear();
            foreach (var t in tags)
                OperationContext.AfterSnapshot.Add(t);
            await ProcessComparisonAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "RFID read", MessageBoxButton.OK, MessageBoxImage.Warning);
            App.Nav.NavigateTo("MainMenu");
        }
    }

    private async Task ProcessComparisonAsync()
    {
        PhaseTitle.Text = "Processing";
        PhaseDetail.Text = "Comparing before/after RFID lists.";
        OperationContext.ComputeDiffFromSnapshots();
        OperationContext.RebuildUnknownList(App.Local.HasItemRfid);

        if (OperationContext.UnknownRfidsPending.Count > 0)
        {
            App.Nav.NavigateTo("UnknownRfid");
            return;
        }

        try
        {
            await OperationFinalize.CompleteAsync(App.Local, App.Gateway);
            await App.OutboundSync.FlushPendingAsync();
            App.Nav.NavigateTo("OperationSummary");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Processing", MessageBoxButton.OK, MessageBoxImage.Error);
            App.Nav.NavigateTo("MainMenu");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        OperationContext.ResetForNewOperation();
        App.Nav.NavigateTo("MainMenu");
    }
}
