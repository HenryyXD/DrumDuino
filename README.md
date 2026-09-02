# DrumDuino

Electronic drum module for Arduino Mega (16 analog inputs) with a modern PC configuration app.

## Structure

```
DrumDuino/
├── firmware/     Arduino sketch (MicroDrum/MegaDrum adaptation)
├── app/          PC config tool (in development)
├── presets/      Drum kit presets
└── docs/         Protocol and hardware notes
```

## Hardware

- **MCU:** Arduino Mega 2560
- **Inputs:** 16 analog pads (+ digital choke/aux)
- **USB MIDI:** Moco for LUFA on ATmega16U2 (native USB MIDI, no Hairless)
- **Serial config:** 115200 baud, SysEx manufacturer ID `0x77`

## Daily use

1. Power on the Mega (boots in MIDI mode).
2. Open Melodics / DAW — device appears as USB MIDI.
3. Play. Config app is **not** required.

## Configuration

1. Open DrumDuino app → connect COM port.
2. Edit pads (threshold, note, scan time, etc.).
3. Save to EEPROM on the board.
4. Return to MIDI mode and disconnect.

## Firmware origin

Firmware in `firmware/` is adapted from [MicroDrum](https://github.com/massimobernava/md-firmware) / MicroMegaDrum for Arduino Mega without multiplexer boards.

## License

Firmware: GPL-3.0 (see `firmware/LICENSE`). App: TBD.
