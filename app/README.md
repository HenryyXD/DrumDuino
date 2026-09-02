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

## Features (v2 — redesign P0/P1)

- **Layout:** StatusBar + tabs Config | Analytics (1180×720)
- **Config:** lista de pads, editor agrupado, monitor dock, rail de ações
- **Analytics:** timeline, heatmap 16 pads, intensidade, single-pad mode, crosstalk
- **Diff:** badges RAM/EEPROM vs app
- **Profiles** por pad (`%AppData%/DrumDuino/profiles`)
- **Assistente** threshold (wizard 10 golpes)
- **Lote:** multi-select + threshold/curva; duplicar pad; reordenar ▲▼
- **Undo/redo** local; test note MIDI OUT; preview de curva

Ver `ROADMAP.md` — **P2 pendente** (editor gráfico curva, auto-COM, log SysEx, temas, tooltips).

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
