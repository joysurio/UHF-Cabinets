using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using UHFCabinets.Session;

namespace UHFCabinets.Views;

public partial class InventoryPage : Page
{
    public sealed class InventoryLineDto
    {
        public string Producto { get; set; } = "";
        public int Quantity { get; set; }
    }

    public InventoryPage()
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

        var rows = App.Local.GetInventoryAggregatedByProduct();
        var list = new List<InventoryLineDto>();
        foreach (var (producto, qty) in rows)
            list.Add(new InventoryLineDto { Producto = producto, Quantity = qty });
        InventoryGrid.ItemsSource = list;
    }

    private void OnBack(object sender, RoutedEventArgs e) => App.Nav.NavigateTo("MainMenu");
}
