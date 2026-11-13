using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Data.Services;
using UHFCabinets.Services;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class UnknownRfidPage : Page
{
    private sealed class ProductOption
    {
        public string IdProducto { get; init; } = "";
        public string Nombre { get; init; } = "";
        public string Display => $"{Nombre} ({IdProducto})";
    }

    private readonly Dictionary<string, ComboBox> _combos = new(StringComparer.Ordinal);
    private List<ProductOption> _options = new();

    public UnknownRfidPage()
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

        if (OperationContext.UnknownRfidsPending.Count == 0)
        {
            await TryFinishAsync();
            return;
        }

        StatusText.Text = "";
        await LoadProductOptionsAsync();
        BuildRows();
    }

    private async Task LoadProductOptionsAsync()
    {
        try
        {
            await App.Gateway.RefreshProductsCatalogCacheAsync();
        }
        catch
        {
            /* use cache */
        }

        var json = App.Local.GetProductsCatalogJson();
        _options = ParseCatalog(json);
        if (_options.Count == 0)
        {
            foreach (var p in App.Local.GetDistinctProductoFromItems())
                _options.Add(new ProductOption { IdProducto = p, Nombre = p });
        }
    }

    private static List<ProductOption> ParseCatalog(string? json)
    {
        var list = new List<ProductOption>();
        if (string.IsNullOrWhiteSpace(json)) return list;
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("items", out var items)) return list;
            foreach (var el in items.EnumerateArray())
            {
                var id = el.TryGetProperty("idProducto", out var idEl) ? idEl.GetString() : null;
                var name = el.TryGetProperty("nombre", out var nEl) ? nEl.GetString() : id;
                if (!string.IsNullOrEmpty(id))
                    list.Add(new ProductOption { IdProducto = id!, Nombre = name ?? id! });
            }
        }
        catch
        {
            /* ignore */
        }

        return list;
    }

    private void BuildRows()
    {
        RowsPanel.Children.Clear();
        _combos.Clear();

        foreach (var rfid in OperationContext.UnknownRfidsPending.Distinct(StringComparer.Ordinal))
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 120 });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 160 });

            var tagLabel = new TextBlock
            {
                Text = rfid,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 12, 0)
            };
            Grid.SetColumn(tagLabel, 0);

            var cb = new ComboBox
            {
                DisplayMemberPath = nameof(ProductOption.Display),
                SelectedValuePath = nameof(ProductOption.IdProducto),
                ItemsSource = _options,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(cb, 1);

            grid.Children.Add(tagLabel);
            grid.Children.Add(cb);
            _combos[rfid] = cb;
            RowsPanel.Children.Add(grid);
        }
    }

    private async void OnSave(object sender, RoutedEventArgs e)
    {
        StatusText.Text = "";
        foreach (var kv in _combos)
        {
            if (kv.Value.SelectedValue is not string id || string.IsNullOrWhiteSpace(id))
            {
                StatusText.Text = "Select a product for every RFID.";
                return;
            }

            var opt = kv.Value.SelectedItem as ProductOption;
            var nombre = opt?.Nombre ?? id;
            App.Local.UpsertItem(kv.Key, nombre);
            var ok = await App.Gateway.TryRegisterRfidAsync(kv.Key, id);
            if (!ok)
            {
                App.Local.EnqueueSync(LocalCabinetService.SyncKindRfid, new { rfid = kv.Key, idProducto = id });
            }
        }

        OperationContext.RebuildUnknownList(App.Local.HasItemRfid);
        if (OperationContext.UnknownRfidsPending.Count > 0)
        {
            StatusText.Text = "Some tags are still unknown. Save again after assigning products.";
            BuildRows();
            return;
        }

        await TryFinishAsync();
    }

    private async Task TryFinishAsync()
    {
        try
        {
            await OperationFinalize.CompleteAsync(App.Local, App.Gateway);
            await App.OutboundSync.FlushPendingAsync();
            App.Nav.NavigateTo("OperationSummary");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Finish operation", MessageBoxButton.OK, MessageBoxImage.Error);
            App.Nav.NavigateTo("MainMenu");
        }
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        App.Nav.NavigateTo("MainMenu");
    }
}
