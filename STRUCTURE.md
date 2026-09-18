# Codebase Structure

## Directory Layout

```
tShockLoader/
├── src/
│   ├── TShockLoader/                 # TML Mod host, hooks, relinker, plugin loader
│   ├── TShockLoader.Abstractions/    # Optional TML contract (net6.0)
│   ├── TerrariaApi.Server/           # TSAPI 2.1 surface + OTAPI compatibility
│   └── TShockAPI/                    # TShock 5.2.3 plugin
├── samples/                          # Example plugins / content mods
├── docs/                             # User-facing install, config, compatibility
├── scripts/                          # Release verification
├── tModLoader.targets                # Locate tML install; pack only tShockLoader
├── TShockLoader.sln                  # Solution
├── TShockLoader.slnx                 # Solution (slnx)
├── dev-docs/                         # Internal architecture notes (not operator docs)
└── README.md
```

## Directory Purposes

**`src/TShockLoader/`:**
- Purpose: Dedicated-server Mod entry, session host, hook attach/bridge, relinker, plugin load
- Contains: C# host code; packed vendor DLLs copied at pack time; `build.txt` / `description.txt`
- Key files: `TShockLoader.cs`, `Runtime/PluginHost.cs`, `Plugins/PluginLoader.cs`, `tShockLoader.csproj`

**`src/TShockLoader/HookAttach/`:**
- Purpose: Subscribe tML `On_*` and related methods; raise OTAPI-shaped events
- Contains: Per-domain attach/detach classes
- Key files: `HookManager.cs`, `NPCHooks.cs`, `WorldGenHooks.cs`, `NetMessageHooks.cs`, `WiringHooks.cs`, `MessageBufferHooks.cs`, `ItemHooks.cs`, `ChestHooks.cs`

**`src/TShockLoader/HookBridge/`:**
- Purpose: Subscribe OTAPI `Hooks.*` and tML events; forward into `ServerApi.Hooks`
- Contains: Namespace `TerrariaApi.Server.Hooking`
- Key files: `ApiHooks.cs`, `GameHooks.cs`, `NetHooks.cs`, `NpcHooks.cs`, `ItemHooks.cs`, `ProjectileHooks.cs`, `ServerHooks.cs`, `WiringHooks.cs`, `WorldHooks.cs`, `HookMethods.cs`

**`src/TShockLoader/Compatibility/Relinker/`:**
- Purpose: Cecil rewrite of third-party plugin PE; synthetic `OTAPI.Runtime`
- Key files: `PluginRelinker.cs`, `OtapiRuntime.cs`, `OtapiRuntimeBinder.cs`

**`src/TShockLoader/Runtime/`:**
- Purpose: Paths, host context, lifecycle logging, contract probes
- Key files: `LoaderPaths.cs`, `HostTmlContext.cs`, `NetContractProbe.cs`, `ContentContractProbe.cs`, `LifecycleLog.cs`

**`src/TShockLoader.Abstractions/`:**
- Purpose: Plugin-safe host identity (`TmlBridge.TryGet`)
- Key files: `TmlBridge.cs`, `ITmlContext.cs`, `ITmlHostInfo.cs`

**`src/TerrariaApi.Server/`:**
- Purpose: Plugin API, hook manager, event args, OTAPI/ITile compatibility shims
- Key files: `ServerApi.cs`, `TerrariaPlugin.cs`, `HookManager.cs`, `Compatibility/OTAPI/Hooks.cs`, `Compatibility/Terraria/ITile.cs`, `TileRef.cs`, `TileHeaders.cs`

**`src/TShockAPI/`:**
- Purpose: TShock core plugin
- Contains: `DB/`, `Rest/`, `Handlers/`, `Hooks/`, `Configuration/`, `lib/` (BCrypt, SQLite, HttpServer, Npgsql, MySqlConnector, GetText, ReflectionMagic)
- Key files: `TShock.cs`, `Commands.cs`, `GetDataHandlers.cs`, `Bouncer.cs`, `TSPlayer.cs`

**`samples/`:**
- Purpose: Reference plugins and a content-contract mod
- Contains: `SamplePlugin/`, `PortablePlugin/` (Abstractions), `FaultPlugin/`, `P3Content/` (used by `ContentContractProbe`)

**`docs/`:**
- Purpose: Operator and plugin-author documentation
- Key files: `install.md`, `configuration.md`, `compatibility.md`, `plugin-development.md`, `faq.md`, `versions.md`

**`scripts/`:**
- Purpose: Release checks and subtree sync
- Key files: `verify-release.ps1`, `sync-subtrees.ps1` (used by `.githooks/pre-push` and `.github/workflows/sync-subtrees.yml`)

**`dev-docs/`:**
- Purpose: Internal architecture notes (not operator-facing)
- Key files: `ARCHITECTURE.md`, `runtime-contracts.md`, `hook-matrix.md`, `tml-extension-sdk.md`

## Key File Locations

**Entry Points:** `src/TShockLoader/TShockLoader.cs`: TML `Mod` / `ModSystem` / `GlobalTile`
**Host session:** `src/TShockLoader/Runtime/PluginHost.cs`: Start/Stop orchestration
**Plugin load:** `src/TShockLoader/Plugins/PluginLoader.cs`: Core TShock + ServerPlugins
**TSAPI bind:** `src/TerrariaApi.Server/ServerApi.cs`
**TShock plugin:** `src/TShockAPI/TShock.cs`
**Configuration (TShock):** `src/TShockAPI/Configuration/TShockConfig.cs`, `ConfigFile.cs`
**Mod metadata:** `src/TShockLoader/build.txt` (`side = Server`, `dllReferences`)
**Packaging:** `tModLoader.targets`, `src/TShockLoader/tShockLoader.csproj` (`StagePackedLibraries`)
**Tests:** no project test suite; probes in `Runtime/*Probe.cs`; samples under `samples/`

## Naming Conventions

**Files:** PascalCase types matching class name (`PluginHost.cs`, `GetDataHandlers.cs`)
**Host namespace:** `tShockLoader` / `tShockLoader.Runtime` / `tShockLoader.Plugins` / `tShockLoader.Relinker` / `tShockLoader.HookAttach`
**Bridge namespace:** `TerrariaApi.Server.Hooking` even though files live under `src/TShockLoader/HookBridge/`
**Abstractions namespace:** `TShockLoader.Abstractions`
**Assembly names:** `tShockLoader` (Mod), `TShockAPI`, `TerrariaApi.Server`, `TShockLoader.Abstractions`
**Directories:** PascalCase feature folders (`HookAttach`, `DB`, `Rest`)

## Where to Add New Code

**New OTAPI-shaped game hook (tML → OTAPI):** `src/TShockLoader/HookAttach/` — add attach/detach pair and call from `HookManager.AttachAll` / `DetachAll` (reverse order on detach).

**New TSAPI hook (OTAPI → ServerApi.Hooks):** `src/TShockLoader/HookBridge/` — attach in `ApiHooks.Attach`; add handler collection on `src/TerrariaApi.Server/HookManager.cs` and EventArgs under `src/TerrariaApi.Server/EventArgs/` if needed.

**New OTAPI type shim:** `src/TerrariaApi.Server/Compatibility/OTAPI/` (keep `Hooks.cs` as the event surface plugins expect).

**New TShock command / packet handler:** `src/TShockAPI/Commands.cs` or `src/TShockAPI/Handlers/` (register from `GetDataHandlers`).

**New DB store:** `src/TShockAPI/DB/` plus query builder under `src/TShockAPI/DB/Queries/` if SQL dialect differs.

**New optional TML contract for plugins:** extend `src/TShockLoader.Abstractions/` and implement on `HostTmlContext`; do not leak host types into Abstractions.

**New relink mapping:** `src/TShockLoader/Compatibility/Relinker/PluginRelinker.cs` (`Map` / `NeedsRelink`); synthetic runtime types via `OtapiRuntime`.

**New sample plugin:** `samples/<Name>/` — TSAPI plugins reference TerrariaApi.Server + TShockAPI; portable TML-aware plugins may reference Abstractions only.

**Shared TShock utilities:** `src/TShockAPI/Utils.cs`, `Extensions/`

**Do not** add `TShockAPI.dll` / `TerrariaApi.Server.dll` / `tShockLoader.dll` into `ServerPlugins/`; the loader rejects those copies.
