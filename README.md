# Factorio Mod List Manager

Desktop tool for managing named Factorio mod configurations inside a Factorio `mods` folder.

## What It Does

- Selects and remembers the Factorio `mods` folder.
- Scans root `.zip` mods and reads `info.json` metadata when available.
- Detects managed mod-list folders containing both `mod-list.json` and `mod-settings.dat`.
- Creates, edits, renames, deletes, and activates managed mod lists.
- Backs up root `mod-list.json` and `mod-settings.dat` before activation.
- Treats `mod-settings.dat` as binary data and never parses or rewrites it.

## Run

```powershell
dotnet run --project src\FactorioModManager.App\FactorioModManager.App.csproj
```

## Test

```powershell
dotnet test FactorioModManager.sln
```

## Publish Example

```powershell
dotnet publish src\FactorioModManager.App\FactorioModManager.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```
