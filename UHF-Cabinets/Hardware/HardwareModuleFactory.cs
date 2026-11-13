using System.Net.Http;
using ClsLectorUHF;
using UHFCabinets.Data.Configuration;

namespace UHFCabinets.Hardware;

public static class HardwareModuleFactory
{
    /// <summary>
    /// Resolves door and RFID modules for production (<see cref="AppSettings.TestMode"/> false).
    /// <paramref name="bridgeHttp"/> is non-null when either channel uses httpBridge (single shared client).
    /// </summary>
    public static void ResolveProductionHardware(
        AppSettings settings,
        Func<string, HttpClient> createBridgeHttp,
        out IDoorModule door,
        out IRfidModule rfid,
        out HttpClient? bridgeHttp)
    {
        bridgeHttp = null;
        var dMode = settings.Hardware.GetEffectiveDoorMode();
        var rMode = settings.Hardware.GetEffectiveRfidMode();
        var bridgeUrl = settings.Hardware.BridgeBaseUrl;

        var doorBridge = dMode.Equals("httpBridge", StringComparison.OrdinalIgnoreCase);
        var rfidBridge = rMode.Equals("httpBridge", StringComparison.OrdinalIgnoreCase);

        if (doorBridge || rfidBridge)
        {
            if (string.IsNullOrWhiteSpace(bridgeUrl))
            {
                throw new InvalidOperationException(
                    "hardware.bridgeBaseUrl is required when door or RFID mode is httpBridge.");
            }

            bridgeHttp = createBridgeHttp(bridgeUrl.Trim());
        }

        door = doorBridge && bridgeHttp != null
            ? new HttpBridgeDoorModule(bridgeHttp, settings.Hardware)
            : new StubDoorModule();

        if (rfidBridge && bridgeHttp != null)
            rfid = new HttpBridgeRfidModule(bridgeHttp);
        else if (rMode.Equals("lectorUhf", StringComparison.OrdinalIgnoreCase))
            rfid = new ClsLectorUhfRfidModule(new UhfReaderFacade(UhfHardwareBrand.Chainway));
        else
            rfid = new StubRfidModule();
    }
}
