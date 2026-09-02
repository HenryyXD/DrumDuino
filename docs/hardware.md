# Hardware setup

## Arduino Mega + Moco for LUFA

Two chips on the board:

| Chip | Role |
|------|------|
| ATmega2560 | Runs `firmware/microdrum.ino` — pads, triggers, SysEx config |
| ATmega16U2 | Runs **Moco for LUFA** — USB MIDI to the PC |

## Playing (MIDI mode)

```
Pads → 2560 (MIDI mode) → 16U2 (Moco) → USB MIDI → Melodics / DAW
```

No Hairless or loopMIDI required.

## Configuring (Tool mode)

```
DrumDuino app → COM port (115200) → 2560 (Tool mode) → SysEx 0x77
```

MIDI USB and serial config use different paths; both can coexist.

## Jumper

A jumper on the USB area is needed to flash the **16U2** (DFU / Moco). Normal sketch uploads target the **2560** and usually do not need that jumper.

## Firmware flags (microdrum.ino)

- `SERIALSPEED = 1` → 115200 baud for config tool
- `MEGA = 1` → 16 inputs, no multiplexer
- `NPin = 16`
