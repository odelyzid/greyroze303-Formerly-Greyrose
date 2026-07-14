# Greyrose — Wizard101 Private Server Emulator

> Quick-and-dirty login/patch/game server emulator for Wizard101 client revision **r667549.Wizard_1_390**

## Quick Start

1. Edit hosts (`C:\Windows\System32\drivers\etc\hosts`):
   ```
   127.0.0.1 login.us.wizard101.com
   127.0.0.1 patch.us.wizard101.com
   ```
   Run `ipconfig /flushdns`

2. Console (all platforms) — starts all three servers immediately:
   ```bash
   dotnet run --project wizard101/Greyrose/Greyrose.csproj -- --console
   ```

3. Windows GUI — launch Greyrose (servers auto-start):
   ```bash
   dotnet run --project wizard101/Greyrose/Greyrose.csproj
   ```

## Core Commands

- `dotnet build wizard101/WizPS.sln` — build single C# project
- `--db <path>` — use custom database path

## Build-time Flags

- `--build-patch-only` — rebuild patch list metadata files
- `--build-patch-minimal` — minimal patch list (error 16 fixes)
- `--validate-patch-bin` — validate LatestFileList.bin
- `--validate-login-blob [--char-id]` — test login-blob building
- `--inspect-login-blob --char-id` — dump blob structure
- `--dump-zone-login-blob --char-id` — hex dump blob
- `--import-zone-login-blob <file>` — import zone capture
- `--resanitize-player-blobs` — rebuild stored player login blobs
- `--console` — force console mode (GUI otherwise)

## Architecture

Single `.cs` project with partial `Server` class:
- `Servers/LoginServer.cs` — TCP 12000 (KIP auth, char mgmt)
- `Servers/PatchServer.cs` — TCP 12500 (KIP metadata)
- `Servers/PatchFileServer.cs` — HTTP 12501 (file serving)
- `Servers/GameServer.cs` — TCP 12170 (player packets)
- `ServerLifecycle` — starts PatchFileServer first (HTTP 12501)
- `ClientSession` — per-connection state (AccountUserGid, SelectedCharacterId)

Player data pipeline:
- Database (`greyrose.db`) → `PlayerData` ↔ `PlayerStruct`
- `LoginBlobBuilder` creates blobs (creation/zone-capture/default)
- `LoginBlobInspector` / `ZoneLoginBlobImporter` — blob tools
- Static zone-state blobs bundled in project

## Key Quirks

- Static `KIPacket` backing buffer (4096 bytes, not thread-safe)
- Hardcoded encryption key: `"but most of all, 11a10318 is my hero"`
- Windows-only GUI (TargetFramework `net8.0-windows`), console mode required on Linux
- No tests, linting, or CI in repository
- ImplicitUsings and Nullable disabled in csproj
- No UDP listener despite char-select referencing port 12171
- Session key is static (no real crypto)
- Auth admits all via first account (UserGID `4295088136144`)
