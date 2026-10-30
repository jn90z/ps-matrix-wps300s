# MATRIX WPS300S-150-5 USB Remote Control

Windows WPF (.NET 8) desktop GUI for the **MATRIX WPS300S-150-5** programmable DC power supply. A separate implementation inspired by the layout of [ps-silabs-CP210](https://github.com/jn90z/ps-silabs-CP210), without reusing that instrument's proprietary serial frames.

## Features

- Dark, bench-instrument-style front panel with live **voltage, current and calculated power**.
- USB virtual COM-port selection, refresh, connect/disconnect and device identification.
- Set current first, then Tab to voltage; input fields are blank on startup.
- Independent voltage/current set commands and combined Apply.
- Output ON (confirmation required), OFF, and status query.
- Remote/local mode and continuous readback approximately every 500 ms.
- Optional output-OFF request on normal disconnect/exit (default enabled).
- Timestamped diagnostic log.
- Limits: **0–150 V, 0–5 A, 300 W** (the combined Apply command enforces the power limit).

## Build and run

Requires **Windows 10/11**, the **.NET 8 SDK**, and a USB interface that appears as a Windows COM port.

```powershell
git clone https://github.com/jn90z/ps-matrix-wps300s.git
cd ps-matrix-wps300s
dotnet restore
dotnet run --project MatrixWps300s.csproj
```

To publish a self-contained Windows executable:

```powershell
dotnet publish MatrixWps300s.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

Output: `bin/Release/net8.0-windows/win-x64/publish/`.

## USB and protocol

The manufacturer's WPS300S series documents describe **optional** USB / RS-232 / RS-485 interfaces and **SCPI** communication. This first implementation assumes the installed USB interface presents a **virtual COM port**, rather than USBTMC or HID. If Windows does not enumerate a COM port, identify the USB device's VID/PID and interface class before changing the transport. Do not assume that every model has an installed USB communication option.

Communication: **9600 baud, 8 data bits, no parity, 1 stop bit, no flow control**, ASCII lines terminated with **CR LF**.

| Operation | SCPI |
|---|---|
| Identity | `*IDN?` |
| Remote / local | `SYST:REM` / `SYST:LOC` |
| Set voltage/current | `VOLT 12.000` / `CURR 0.1000` |
| Apply both | `APPL 12.000,0.1000` |
| Read voltage/current | `MEAS:VOLT?` / `MEAS:CURR?` |
| Read power | `MEAS:POW?` |
| Output enable/disable/query | `OUTP ON` / `OUTP OFF` / `OUTP?` |

Sources:
- [MATRIX WPS-300S product page](https://www.szmatrix.com/product/wps-300s-series-wide-range-programmable-dc-power-supply/)
- [MATRIX MPS-200/WPS300S SCPI programming manual (PDF)](https://www.szmatrix.com/wp-content/uploads/2025/09/MPS-200WPS300S-Series-SCPI-Programming-Protocol-1.pdf)

## First hardware test

1. With the output disconnected from your device under test, attach the supply's USB interface and check **Device Manager → Ports (COM & LPT)**.
2. Select the COM port and Connect. Confirm the log shows an identification response to `*IDN?`.
3. Verify a voltage and current measurement against the front-panel display.
4. Set low current and low voltage, check the front panel, then test output enable/disable into a suitable dummy load.
5. Validate that disconnect/exit sends OFF and confirm the output physically turns off. Cable removal, USB faults and host power loss cannot guarantee a shutdown command.

**Safety:** 150 V DC and up to 300 W can be hazardous. Software controls and timed readback are not substitutes for hardware protection or a physical emergency disconnect. An OFF request is not equivalent to a confirmed OFF state. Validate the actual unit's response before connecting valuable loads.

## Scope

USB communication and GUI only. No scheduling, cloud, network service, Plex integration, test sequencing, or manufacturing automation. Hardware transport and command compatibility must be verified against the actual unit.

## License

MIT — see [LICENSE](LICENSE).
