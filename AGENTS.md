# DrumDuino — agent guide

Monorepo for an Arduino Mega 16-pad e-drum module and its PC config app.

**Local path:** `Z:\Workdir\DrumDuino`  
**GitHub:** https://github.com/HenryyXD/DrumDuino

## Structure

```
DrumDuino/
├── firmware/          microdrum.ino (Mega 16, 115200) — do not rewrite for v1 app
├── app/               Avalonia + C# (.NET 8) config tool
├── presets/           JSON kits (+ legacy pins.ini import)
└── docs/              protocol.md, hardware.md
```

## Run the app

```bash
cd Z:\Workdir\DrumDuino\app
dotnet run --project DrumDuino.App
```

Release build:

```bash
cd app
dotnet publish DrumDuino.App -c Release -r win-x64 --self-contained false
```

## Stack

- **UI:** Avalonia 11 + CommunityToolkit.Mvvm
- **Core:** `DrumDuino.Core` — SysEx protocol, serial client, presets
- **Serial:** 115200 baud, `System.IO.Ports`
- **Firmware protocol:** SysEx `F0 77 [cmd] [d1] [d2] [d3] F7` — see `docs/protocol.md`

## Hardware assumptions

- Arduino Mega 2560 = drum firmware (`MEGA=1`, `NPin=16`, `SERIALSPEED=1`)
- ATmega16U2 with Moco for LUFA = native USB MIDI (no Hairless/loopMIDI)
- **Play:** USB MIDI → Melodics/DAW (MIDI mode, config app closed)
- **Configure:** COM @ 115200 → Tool mode → edit → Save EEPROM → Return MIDI

## App workflow

1. Connect COM → enters **Tool** mode automatically
2. Edit 16 pads offline or **Ler do módulo**
3. **Aplicar (RAM)** or **Salvar EEPROM**
4. **Voltar MIDI** or close window (returns to MIDI on exit)
5. Import legacy `pins.ini` or JSON presets

## What NOT to do

- Do **not** fork or copy UI/code from old microDrum ConfigTool (C#, Python, .exe)
- Do **not** change firmware protocol without updating `docs/protocol.md` and `DrumDuino.Core`
- Do **not** target TeensyDrum / 48-pad microDrum in v1 — Mega 16 only
- Do **not** commit secrets, `.env`, or personal presets if sensitive

## Key source files

| Area | Files |
|------|-------|
| Protocol | `app/DrumDuino.Core/Protocol/SysExCodec.cs` |
| Serial | `app/DrumDuino.Core/Serial/MicroDrumClient.cs` |
| Firmware truth | `firmware/d_setting.ino`, `firmware/a_midi.ino` |
| UI | `app/DrumDuino.App/Views/MainWindow.axaml` |

## Firmware changes (optional, later)

- Return to MIDI mode when config app disconnects (partially done in app on close)
- LED indicator for current mode
