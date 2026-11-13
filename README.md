# UHF Cabinets – Software Manager iTRACE®

Windows desktop application for **cabinet devices**: user identification via central API, inventory from last RFID scan, **Dispense / Restock** with before/after RFID comparison, per-tag movements (`movimientos`), mandatory unknown-tag registration, and outbound sync when the API is unavailable.

## Requirements (from client)

- **Platform:** Windows desktop app  
- **Database:** Local SQLite (`items`, `movimientos`, plus `app_meta` for last scan & product cache, `pending_api_sync` for retries)  
- **Gateway API:** `UHF-Cabinets.Api` (see solution)

## Features implemented (CTO cabinet scope)

1. **Login** – Credentials validated against **UHF-Cabinets.Api** (`POST /api/auth/login`). Session stores `userId`, display name, role. No login → no menu.
2. **Main menu** – Only **Inventory**, **Dispense / Restock**, **Logout**.
3. **Inventory** – **Strict UI:** product name and quantity only, aggregated from **last valid RFID scan** + `items` mapping.
4. **Dispense / Restock** – `idMov`, `beforeSnapshot` from local inventory scan state, door open/close via **`IDoorModule`**, RFID read via **`IRfidModule`** (no real hardware in this codebase).
5. **Comparison** – `SALIDA` = before − after, `ENTRADA` = after − before; one `movimientos` row per tag.
6. **Unknown RFID** – Tags in **after** scan not in `items` must be assigned a **product** before the operation can finish; optional API `POST /api/items`; failures queued locally.
7. **API sync** – Movements and RFID registrations retried from **`pending_api_sync`**; background timer in the WPF app.
8. **Test mode** – `testMode` in `UHF-Cabinets\appsettings.json`: `TestDoorModule` / `TestRfidModule` with UI to simulate door close and paste RFID lines. With `testMode: false`, use **`hardware.mode` / `doorMode` / `rfidMode`**: `httpBridge` (local HTTP service), **`lectorUhf`** (ClsLectorUHF → ClsLectorChainway), or **`stub`** (see **Hardware modes** below).

## How to run

- **Prerequisites:** .NET 8 SDK, Windows.
- **Build:** `dotnet build UHF-Cabinets.sln`
- **Gateway (for login/products/sync):** `dotnet run --project UHF-Cabinets.Api\UHF-Cabinets.Api.csproj`
- **Cabinet app:** `dotnet run --project UHF-Cabinets\UHF-Cabinets.csproj`  
  Copy `appsettings.json` is emitted to the output folder; edit **apiBaseUrl**, **cabinetId**, **cabinetApiKey** as needed.

**Demo login (API):** username `admin`, password `Admin123!`, with seeded cabinet headers `CAB-01` / `dev-cabinet-api-key`.

**Local DB path:** `%LocalAppData%\UHF-Cabinets\uhfcabinets_device.db` (new schema; older `uhfcabinets.db` from previous prototypes is not used).

---

## How to use (cabinet UI)

1. Start the **API** (optional for fully offline login is *not* supported—login goes through the gateway).
2. Run the **WPF** app. Home → **Touch to start** → login with API credentials.
3. **Inventory** shows product name and quantity from the last stored RFID scan.
4. **Dispense / Restock:** with `testMode: true`, after the door step use **Simulate: Door closed**, then enter RFIDs (one per line) and **Simulate RFID read**. Unknown tags force the **Unknown RFID** screen until each has a product. You then see a short **summary** (exit / entry counts).
5. **Logout** clears the session and returns to Home.

## Solution structure

- **UHF-Cabinets** – WPF cabinet UI: API client, `IDoorModule` / `IRfidModule` (test, stub, HTTP bridge, **ClsLectorUHF** native stack), navigation, SQLite via `LocalCabinetService`.
- **ClsLectorUHF** – Brand-agnostic UHF façade (`UhfReaderFacade`); references **ClsLectorChainway** for Chainway. Merge client `clsLectorUHF.rar` sources here when available.
- **ClsLectorChainway** – Chainway-specific `IChainwayTagSource`; replace `ChainwayPlaceholderTagSource` with SDK/USB code from `clsLectorChainway.rar`.
- **UHF-Cabinets.Data** – SQLite (EF Core): `items`, `movimientos`, `app_meta`, `pending_api_sync`.
- **UHF-Cabinets.Api** – ASP.NET Core gateway API between cabinets and back-office systems (auth, products, RFID items, movements, async outbound sync). SQLite database under `UHF-Cabinets.Api\Data\uhf_gateway.db` (created on first run).

### Cabinet gateway API (CTO contract)

Run locally:

```bash
dotnet run --project UHF-Cabinets.Api\UHF-Cabinets.Api.csproj
```

Swagger UI (Development): `http://localhost:5288/swagger`.

**Cabinet authentication (all `/api/*` routes):** send headers `X-Cabinet-Id` and `X-Api-Key`. First-run seed registers cabinet `CAB-01` with API key `dev-cabinet-api-key` (change for production).

**Demo user login** (`POST /api/auth/login`): username `admin`, password `Admin123!`. Response user id is `U123` (maps to `idUsuario` in the contract).

**LFSProduction test integration:** optional outbound HTTP sync is configured under `LfsProduction` in `UHF-Cabinets.Api\appsettings.json` (`BaseUrl`, `ApiKey`, `Enabled`, push paths). When `Enabled` is `false` or `BaseUrl` is empty, movements and RFID registrations stay stored locally and are marked synced without calling LFS. Align `MovementsPushPath` / `RfidItemsPushPath` with whatever LFSProduction exposes; the payloads are documented in `LfsOutboundSyncService`.

Endpoints: `POST /api/auth/login`, `GET /api/products`, `GET /api/products/{idProducto}`, `GET /api/items/{rfid}`, `POST /api/items`, `POST /api/movements`, `POST /api/movements/bulk`. `GET /health` is unauthenticated.

## Configuration

- **Cabinet app:** `UHF-Cabinets\appsettings.json` maps to `UHFCabinets.Data.Configuration.AppSettings`.
- **Secrets:** optional `appsettings.secrets.json` next to the exe (same JSON shape for sensitive keys). Ignored by Git; merge overrides `cabinetApiKey`, `hardware`, etc.
- **Environment (optional):** `UHF_API_BASE_URL`, `UHF_CABINET_ID`, `UHF_CABINET_API_KEY`, `UHF_HARDWARE_BRIDGE_URL` (sets bridge URL and `hardware.mode` to `httpBridge`), `UHF_ALLOW_INSECURE_TLS` (`1` / `true` — **dev only**).
- **`allowInsecureTls`:** when `true`, TLS certificate validation is skipped for the **gateway** and **hardware bridge** HTTP clients. **Never** enable in production.
- **Gateway API:** `UHF-Cabinets.Api\appsettings.json` and Swagger auth headers (see gateway section above).

### Hardware modes (`testMode: false`)

`hardware.mode` is the default for **both** door and RFID. Override per channel with **`doorMode`** and **`rfidMode`** (values: `stub`, `httpBridge`, `lectorUhf`). If `mode` is `lectorUhf`, the **door** channel defaults to `stub` unless you set `doorMode` (e.g. to `httpBridge` when the door is still driven by a local HTTP service).

**Native RFID stack (client architecture):** `rfidMode` / `mode` = `lectorUhf` uses **`ClsLectorUHF`** → **`ClsLectorChainway`** (`ClsLectorUhfRfidModule` implements `IRfidModule`). Replace `ChainwayPlaceholderTagSource` with real reader code from the client archives.

#### HTTP bridge (door and/or RFID without vendor DLLs in-process)

When `doorMode` and/or `rfidMode` is `httpBridge`, set `bridgeBaseUrl`. One shared `HttpClient` is used for both channels when both are `httpBridge`.

```json
"hardware": {
  "mode": "httpBridge",
  "bridgeBaseUrl": "https://127.0.0.1:17700",
  "doorClosedPollMilliseconds": 250
}
```

Implement a small local service that maps your PLC / reader to this HTTP contract:

| Method | Path | Response |
|--------|------|----------|
| `POST` | `hardware/door/open` | `204` / `200` on success |
| `GET` | `hardware/door/status` | JSON `{"closed": true}` or `{"closed": false}` (poll until `true`) |
| `GET` | `hardware/rfid/tags` | JSON `{"tags": ["EPC1", "EPC2", ...]}` |

**Example:** RFID via native stack, door via HTTP bridge:

```json
"hardware": {
  "mode": "lectorUhf",
  "doorMode": "httpBridge",
  "rfidMode": "lectorUhf",
  "bridgeBaseUrl": "https://127.0.0.1:17700"
}
```

If both resolved channels are `stub`, `StubDoorModule` / `StubRfidModule` run.

### Deployment seed (optional)

Set `deploymentSeed.importOnStartup` to `true` and `deploymentSeed.jsonPath` to a file like `deployment-seed.sample.json`. On startup the app **upserts** `items` and can set `lastScanRfids`. Sample: `UHF-Cabinets\deployment-seed.sample.json`.

## Next steps (production)

1. **Chainway / UHF libraries:** Merge the client’s `clsLectorChainway.rar` into [ClsLectorChainway](ClsLectorChainway/ClsLectorChainway.csproj) (implement `IChainwayTagSource`) and `clsLectorUHF.rar` into [ClsLectorUHF](ClsLectorUHF/ClsLectorUHF.csproj) (extend `UhfReaderFacade` for additional brands). Rebuild; keep `hardware.rfidMode` as `lectorUhf` (or `mode` only).
2. **Door:** Use `doorMode: httpBridge` with a local bridge, or add a real `IDoorModule` for your actuator and register it in [App.xaml.cs](UHF-Cabinets/App.xaml.cs) / [HardwareModuleFactory](UHF-Cabinets/Hardware/HardwareModuleFactory.cs).
3. **TLS / secrets:** Use HTTPS for the gateway; keep `allowInsecureTls` false; store `cabinetApiKey` in `appsettings.secrets.json` or `UHF_CABINET_API_KEY`.
4. **Deploy:** Use `deploymentSeed` or your own tooling to preload `items` and optionally `lastScanRfids`.
