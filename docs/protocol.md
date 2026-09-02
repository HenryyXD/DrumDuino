# Serial protocol (SysEx)

Manufacturer ID: **0x77**

## Packet format

```
F0 77 [cmd] [data1] [data2] [data3] F7
```

## Commands

| Cmd | Name | Description |
|-----|------|-------------|
| `0x00` | AskMode | Returns current mode |
| `0x01` | SetMode | `data1`: 0=Off, 1=Standby, 2=MIDI, 3=Tool |
| `0x02` | AskSetting | Read parameter; `data1`=pin, `data2`=param id |
| `0x03` | SetSetting | Write parameter (RAM only) |
| `0x04` | SaveSetting | Write parameter to EEPROM |
| `0x6F` | Diagnostic | Monitor pad hits in Tool mode |
| `0x7F` | Reset | Soft reset |

## Pin parameters (`data2`)

| Id | Parameter |
|----|-----------|
| `0x00` | Note |
| `0x01` | Threshold |
| `0x02` | ScanTime |
| `0x03` | MaskTime |
| `0x04` | Retrigger |
| `0x05` | Curve |
| `0x06` | XTalk |
| `0x07` | XTalk Group |
| `0x08` | CurveForm |
| `0x09` | ChokeNote / Gain |
| `0x0D` | Type (Piezo, HHC, Disabled…) |
| `0x0E` | MIDI Channel |

## Special `data1` values

| Value | Meaning |
|-------|---------|
| `0x7E` | General settings (delay, NSensor, xtalk) |
| `0x4C` | Hi-hat settings |
| `0x7F` | End of transmission marker (response) |

Source of truth: `firmware/d_setting.ino`, `firmware/a_midi.ino`.
