# DrumDuino Config App

Avalonia desktop app for configuring the DrumDuino Mega 16-pad module.

## Run

```bash
cd Z:\Workdir\DrumDuino\app
dotnet run --project DrumDuino.App
```

## Projects

| Project | Role |
|---------|------|
| `DrumDuino.App` | Avalonia UI (MVVM) |
| `DrumDuino.Core` | SysEx protocol, serial client, preset import/export |
| `tools/PresetExport` | One-shot `pins.ini` → JSON converter |

## Features (v1)

- Edit 16 pads offline (names are local-only; firmware stores trigger params)
- Connect COM @ 115200 → Tool mode
- Read / apply / save EEPROM
- Return to MIDI on exit
- Import `pins.ini`, load/save JSON presets

## Dependencies

- .NET 8 SDK
- Windows (serial + desktop; cross-platform possible later)

## Note on Avalonia

Uses **Avalonia 11.2** — Avalonia 12 source generators require a newer Roslyn than .NET SDK 9 ships with in this environment.
