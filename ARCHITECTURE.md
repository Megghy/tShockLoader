# Architecture

## Pattern Overview

**Overall:** tModLoader Mod host that embeds TSAPI 2.1 and TShock 5.2.3, then bridges game events into the plugin API and relinks third-party plugin assemblies at load time.

**Key Characteristics:**
- Dedicated-server only: `tShockLoader.Load` returns immediately unless `Main.dedServ`.
- Single packaged Mod (`tShockLoader.tmod`) ships TerrariaApi.Server, TShockAPI, Abstractions, and native SQLite interop.
- Two hook layers: `HookAttach` (tML `On_*` / MonoMod into OTAPI-shaped events) then `HookBridge` (OTAPI events into `ServerApi.Hooks`).
- Fail-fast session: `PluginHost.Start` is single-shot; any exception runs `Stop` and rethrows.
- Plugin isolation: reject core DLL copies in `ServerPlugins/`; optional SHA-256 match for a disk copy of `TShockLoader.Abstractions.dll`.

## Layers

**Host / tModLoader Mod:**
- Purpose: Own the dedicated-server session, paths, native SQLite, and lifecycle.
- Location: `src/TShockLoader/`
- Contains: `tShockLoader` Mod, `tShockLoaderSystem`, `GlobalTileHandler`, `PluginHost`, `LoaderPaths`, contract probes
- Depends on: tModLoader (`tMLMod.targets`), TerrariaApi.Server, TShockAPI, TShockLoader.Abstractions
- Used by: tModLoader dedicated server via `Mods/tShockLoader.tmod`

**TSAPI (plugin surface):**
- Purpose: Bind `Main`, expose `ServerApi.Hooks`, plugin list, log/profiler, assembly resolve aliases (`OTAPI`, `TerrariaServer`).
- Location: `src/TerrariaApi.Server/`
- Contains: `ServerApi`, `TerrariaPlugin`, `HookManager`, `EventArgs/`, OTAPI compatibility types under `Compatibility/OTAPI/`
- Depends on: tModLoader / Terraria
- Used by: TShockAPI, third-party plugins, host `PluginLoader`

**TShockAPI:**
- Purpose: Accounts, groups, commands, bouncer/anti-cheat, REST, SQLite/MySQL/Postgres stores.
- Location: `src/TShockAPI/`
- Contains: `TShock` plugin (`[ApiVersion(2, 1)]`, version `5.2.3`), `Commands`, `GetDataHandlers`, `DB/`, `Rest/`, `Bouncer`
- Depends on: TerrariaApi.Server and vendor DLLs in `src/TShockAPI/lib/`
- Used by: operators and third-party plugins

**Abstractions:**
- Purpose: Optional TML host contract for plugins that want loader identity without referencing the host assembly.
- Location: `src/TShockLoader.Abstractions/`
- Contains: `TmlBridge`, `ITmlContext`, `ITmlHostInfo`
- Depends on: net6.0 only (no Terraria)
- Used by: portable plugins; bound by `PluginHost` to `HostTmlContext`

**Relinker:**
- Purpose: Rewrite plugin PE so OTAPI/TerrariaServer/XNA references resolve to tModLoader, FNA, TerrariaApi.Server, TShockAPI; emit a synthetic `OTAPI.Runtime` when needed.
- Location: `src/TShockLoader/Compatibility/Relinker/`
- Contains: `PluginRelinker`, `OtapiRuntime`, `OtapiRuntimeBinder`
- Depends on: Mono.Cecil
- Used by: `PluginLoader` before `AssemblyLoadContext.LoadFromStream`

## Data Flow

**Dedicated-server start:** (`PluginHost.Start`)

1. `tShockLoader.Load` — `src/TShockLoader/TShockLoader.cs` (skip if not `Main.dedServ`)
2. Resolve `-instancepath` / `-tmlsavedirectory` / `-configpath` / `-logpath` — `src/TShockLoader/Runtime/LoaderPaths.cs`
3. Extract `native/SQLite.Interop.dll` into instance cache and bind `DllImportResolver` — `PluginHost.ExtractNativeLibraries`
4. `ServerApi.Bind(Main.instance, …/ServerLog.txt)` — `src/TerrariaApi.Server/ServerApi.cs`
5. `HookAttach.HookManager.AttachAll` then `ApiHooks.Attach(ServerApi.Hooks)` — `src/TShockLoader/HookAttach/`, `src/TShockLoader/HookBridge/`
6. Bind `TmlBridge` to `HostTmlContext` — `src/TShockLoader.Abstractions/TmlBridge.cs`
7. `PluginLoader.Load`: core `TShock` first, then `*.dll` / `*.dll-plugin` from `ServerPlugins/` — `src/TShockLoader/Plugins/PluginLoader.cs`
8. Initialize plugins ordered by `TerrariaPlugin.Order`, then `ServerApi.Hooks.InvokeGameInitialize`
9. `NetContractProbe.Verify` — TML packets 249–253 must not be cancelled by GetData

**Content post-setup:** (`tShockLoaderSystem`)

1. `PostSetupContent` rebuilds English content names and `TShock.Utils.ComputeMaxStyles` — `src/TShockLoader/TShockLoader.cs`
2. `OnWorldLoad` runs `ContentContractProbe.Verify` when sample mod `P3Content` is present

**Unload:** (`PluginHost.Stop`)

1. Dispose third-party plugins reverse of init, then core TShock — `PluginLoader.Unload`
2. `TmlBridge.Unbind`, detach HookBridge then HookAttach, `ServerApi.Unbind`, free SQLite native handle
3. First cleanup exception is printed; later ones are still attempted

**Packet path:**

1. tML `MessageBuffer` / `On_*` — `src/TShockLoader/HookAttach/MessageBufferHooks.cs`, `NetMessageHooks.cs`
2. OTAPI `Hooks.*` — `src/TerrariaApi.Server/Compatibility/OTAPI/Hooks.cs`
3. `NetHooks` / `ServerApi.Hooks.NetGetData` — `src/TShockLoader/HookBridge/NetHooks.cs`
4. TShock `GetDataHandlers` / `Bouncer` — `src/TShockAPI/GetDataHandlers.cs`, `src/TShockAPI/Bouncer.cs`

## Key Abstractions

**PluginHost session flags:**
- Purpose: `bound` / `hooksAttached` / `tmlBound` / `pluginsLoaded` drive reverse cleanup; `Start` throws if already started.
- Location: `src/TShockLoader/Runtime/PluginHost.cs`

**ServerApi:**
- Purpose: Single bound `Main`, plugin list, `Hooks`, log writer. Assembly resolve maps `OTAPI` / `OTAPI.Upcoming` / `TerrariaServer` / `TerrariaApi.Server` to the TSAPI assembly.
- Location: `src/TerrariaApi.Server/ServerApi.cs`
- Pattern: Static session + `InternalsVisibleTo` tShockLoader

**TerrariaPlugin + PluginContainer:**
- Purpose: Plugin contract (`Initialize` / `Dispose`, `Order`, `[ApiVersion]`); containers track `Source` (`Core` vs `Plugin`).
- Location: `src/TerrariaApi.Server/TerrariaPlugin.cs`, `PluginContainer.cs`

**TmlBridge:**
- Purpose: Session-scoped `ITmlContext`; `Bind` once; plugins use `TryGet`.
- Location: `src/TShockLoader.Abstractions/TmlBridge.cs`

**PluginRelinker:**
- Purpose: If assembly refs known vanilla/OTAPI names, rewrite type scopes; unsupported `OTAPI.Runtime` IL hook types throw `RelinkUnsupportedException` (plugin skipped).
- Location: `src/TShockLoader/Compatibility/Relinker/PluginRelinker.cs`

**LoaderPaths:**
- Purpose: Instance root defaults to `-instancepath` else `-tmlsavedirectory` else install dir; plugins at `{instance}/ServerPlugins`; TShock data at `{instance}/tshock` unless `-configpath`.
- Location: `src/TShockLoader/Runtime/LoaderPaths.cs`

## Entry Points

**tShockLoader Mod:**
- Location: `src/TShockLoader/TShockLoader.cs`
- Triggers: tML `Mod.Load` / `Unload`; `ModSystem.PostSetupContent` / `OnWorldLoad`; `GlobalTile.PreHitWire` → `OTAPI.Hooks.Wiring.InvokeAnnouncementBox`
- Responsibilities: Start/stop host; refresh content name tables; optional P3 content contract

**PluginHost.Start / Stop:**
- Location: `src/TShockLoader/Runtime/PluginHost.cs`
- Triggers: Mod Load/Unload
- Responsibilities: Paths, native libs, bind API, attach hooks, load plugins, probes

**TShock plugin:**
- Location: `src/TShockAPI/TShock.cs`
- Triggers: Instantiated by `PluginLoader` as the Core plugin
- Responsibilities: Config, DB, commands, REST, player/region/ban managers

**Third-party plugins:**
- Location: `{instance}/ServerPlugins/*.dll` (runtime, not in repo)
- Triggers: `PluginLoader` after relink; skip names in `ignoredplugins.txt`
- Responsibilities: Subclass `TerrariaPlugin` with matching ApiVersion 2.1

## Error Handling

**Strategy:** Fail-fast on host/session errors; skip or log individual plugin load failures only where explicitly coded.

- `PluginHost.Start` catch-all: `Stop()` then rethrow.
- Duplicate `Start` / duplicate `ServerApi.Bind` / duplicate `TmlBridge.Bind` throw `InvalidOperationException`.
- Missing packed `TShockLoader.Abstractions.dll` or `native/SQLite.Interop.dll` throw `FileNotFoundException`.
- Core assembly copies in `ServerPlugins/` throw (do not load).
- Relink unsupported OTAPI.Runtime hooks: log and skip that plugin.
- `BadImageFormatException` on a file: skip.
- Plugin ctor / `Initialize` failure: throw wrapped `InvalidOperationException` (aborts remaining init).
- `Unload` / `Stop` collect first exception, continue remaining cleanup, then throw or print.

**Contract probes:** `NetContractProbe` always runs after ready; `ContentContractProbe` runs on world load only if `P3Content` is loaded.

## Cross-Cutting Concerns

**Logging:** Console + `Mod.Logger` + `ServerApi.LogWriter` (`ServerLog.txt` under `LoaderPaths.LogRoot`). TShock also writes under `tshock/logs`.

**Caching:** Relinked assemblies load from memory streams (no disk rewrite). SQLite interop extracted to `{instance}/cache/tshockloader/SQLite.Interop.dll`.

**Storage:** TShock `SavePath` = `LoaderPaths.TShockDataRoot` (`config.json`, sqlite/mysql/postgres via `src/TShockAPI/DB/`). Plugins directory is `ServerApi.ServerPluginsDirectoryPath`.

**Native:** `SQLite.Interop.dll` packed from `src/TShockAPI/lib/` into the tmod via `StagePackedLibraries` in `src/TShockLoader/tShockLoader.csproj`.

**Packaging:** Only assembly `tShockLoader` packs as a TML mod (`tModLoader.targets` + `build.txt` `side = Server`). Other projects set `BuildMod=false`.
