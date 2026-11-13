using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;
using UHFCabinets.Configuration;
using UHFCabinets.Data;
using UHFCabinets.Data.Configuration;
using UHFCabinets.Data.Services;
using UHFCabinets.Hardware;
using UHFCabinets.Navigation;
using UHFCabinets.Services;

namespace UHFCabinets;

public partial class App : Application
{
    public static INavigationService Nav { get; private set; } = null!;
    public static AppDbContext Db { get; private set; } = null!;
    public static AppSettings Settings { get; private set; } = null!;
    public static LocalCabinetService Local { get; private set; } = null!;
    public static CabinetGatewayClient Gateway { get; private set; } = null!;
    public static OutboundSyncService OutboundSync { get; private set; } = null!;
    public static HttpClient Http { get; private set; } = null!;
    /// <summary>HTTP client for optional local hardware bridge; separate from <see cref="Http"/>.</summary>
    public static HttpClient? HardwareHttp { get; private set; }
    public static IDoorModule Door { get; private set; } = null!;
    public static IRfidModule Rfid { get; private set; } = null!;
    public static TestDoorModule? TestDoor => Door as TestDoorModule;
    public static TestRfidModule? TestRfid => Rfid as TestRfidModule;

    private DispatcherTimer? _syncTimer;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UHF-Cabinets", "uhfcabinets_device.db");
        Db = new AppDbContext(dbPath);
        Db.EnsureCreatedAndSeed();

        Settings = AppSettingsLoader.LoadFromAppDirectory();
        Http = SecureHttpClientFactory.CreateApiClient(Settings);
        Local = new LocalCabinetService(Db);
        DeploymentSeedImporter.TryImport(Local, Settings.DeploymentSeed?.JsonPath, Settings.DeploymentSeed?.ImportOnStartup ?? false);
        Gateway = new CabinetGatewayClient(Http, Settings, Local);
        OutboundSync = new OutboundSyncService(Local, Gateway);

        if (Settings.TestMode)
        {
            Door = new TestDoorModule();
            Rfid = new TestRfidModule();
        }
        else
        {
            try
            {
                HardwareHttp?.Dispose();
                HardwareHttp = null;
                HardwareModuleFactory.ResolveProductionHardware(
                    Settings,
                    url =>
                    {
                        HardwareHttp?.Dispose();
                        HardwareHttp = SecureHttpClientFactory.CreateHardwareBridgeClient(Settings, url);
                        return HardwareHttp;
                    },
                    out var doorModule,
                    out var rfidModule,
                    out _);
                Door = doorModule;
                Rfid = rfidModule;
            }
            catch (Exception ex)
            {
                HardwareHttp?.Dispose();
                HardwareHttp = null;
                Door = new StubDoorModule();
                Rfid = new StubRfidModule();
                MessageBox.Show(
                    "Hardware configuration error; using stub door/RFID modules.\n\n" + ex.Message,
                    "Hardware",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        Nav = new NavigationService();

        _syncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Clamp(Settings.SyncIntervalSeconds, 5, 600))
        };
        _syncTimer.Tick += async (_, _) => await OutboundSync.FlushPendingAsync();
        _syncTimer.Start();

        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show(args.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _syncTimer?.Stop();
        HardwareHttp?.Dispose();
        Http.Dispose();
        Db.Dispose();
        base.OnExit(e);
    }
}
