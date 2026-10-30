# MATRIX WPS300S-150-5 USB Remote Control

Windows WPF (.NET 8) desktop GUI for the **MATRIX WPS300S-150-5** programmable DC power supply. A separate implementation inspired by the layout of [ps-silabs-CP210](https://github.com/jn90z/ps-silabs-CP210), without reusing that instrument's proprietary serial frames.

## Features

- Dark, bench-instrument-style front panel with live **voltage, current and calculated power**.
- Windows CH340/CH341 USB-serial adapter discovery (filters unrelated COM ports), refresh, connect/disconnect, and instrument identification using `*IDN?`.
- Before connecting, the dropdown shows the detected CH340 adapter; after identification, it shows the MATRIX model and underlying COM port.
- Set current first, then Tab to voltage; input fields are blank on startup.
- Independent voltage/current set commands and combined Apply.
- Output ON (confirmation required), OFF, and status query.
- Remote/local mode and continuous readback approximately every 500 ms using `MEAS:VCM?` (voltage and current in one response).
- Readback TX/RX logging and automatic pause after three consecutive query failures; reconnect to retry.
- Optional output-OFF request on normal disconnect/exit (default enabled).
- Timestamped diagnostic log.
- Limits: **0–150 V, 0–5 A, 300 W** (the combined Apply command enforces the power limit).

## Build and run

Requires **Windows 10/11**, the **.NET 8 SDK**, and a **CH340/CH341 USB-serial interface** with its Windows driver installed. The device picker currently filters to CH340/CH341 adapters and does not list arbitrary serial ports.

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

The manufacturer's WPS300S series documents describe **optional** USB / RS-232 / RS-485 interfaces and **SCPI** communication. This implementation uses the CH340 USB-to-serial adapter observed on the tested WPS300S-150-5. Windows Device Manager displayed `USB-SERIAL CH340 (COM8)`; `*IDN?` returned `MATRIX,WPS300S-15005,1.1`. The COM port number can change across computers and USB connections. CH340 hardware detection alone does not prove that the connected instrument is a Matrix supply; the model is confirmed after identification. Other USB interfaces are not currently listed by the GUI.

Communication: **9600 baud, 8 data bits, no parity, 1 stop bit, no flow control**, ASCII lines terminated with **CR LF**.

| Operation | SCPI |
|---|---|
| Identity | `*IDN?` |
| Remote / local | `SYST:REM` / `SYST:LOC` |
| Set voltage/current | `VOLT 12.000` / `CURR 0.1000` |
| Apply both | `APPL 12.000,0.1000` |
| Live voltage/current polling | `MEAS:VCM?` (expects `voltage,current`) |
| Read power (defined, not currently used for GUI polling) | `MEAS:POW?` |
| Query applied setpoints (defined, not yet shown in GUI) | `APPL?` |
| Error, temperature and protection queries (defined, not yet shown in GUI) | `SYST:ERR?`, `SYST:TEMP?`, `VOLT:PROT?`, `CURR:PROT?` |
| Output enable/disable/query | `OUTP ON` / `OUTP OFF` / `OUTP?` |

Sources:
- [MATRIX WPS-300S product page](https://www.szmatrix.com/product/wps-300s-series-wide-range-programmable-dc-power-supply/)
- [MATRIX MPS-200/WPS300S SCPI programming manual (PDF)](https://www.szmatrix.com/wp-content/uploads/2025/09/MPS-200WPS300S-Series-SCPI-Programming-Protocol-1.pdf)

## First hardware test

1. With the output disconnected from your device under test, attach the supply's USB interface and check **Device Manager → Ports (COM & LPT)**.
2. The dropdown should list only detected CH340/CH341 ports. Select the Matrix adapter (COM8 on the tested machine) and Connect. Confirm the log shows an identification response to `*IDN?`.
3. Verify `MEAS:VCM?` returns a parseable voltage/current pair and compare it against the front-panel display. This measurement query is not yet hardware-verified on firmware 1.1; if it times out three times, polling pauses automatically.
4. Set low current and low voltage, check the front panel, then test output enable/disable into a suitable dummy load.
5. Validate that disconnect/exit sends OFF and confirm the output physically turns off. Cable removal, USB faults and host power loss cannot guarantee a shutdown command.

**Safety:** 150 V DC and up to 300 W can be hazardous. Software controls and timed readback are not substitutes for hardware protection or a physical emergency disconnect. An OFF request is not equivalent to a confirmed OFF state. Validate the actual unit's response before connecting valuable loads.

## Scope

USB communication and GUI only. No scheduling, cloud, network service, Plex integration, test sequencing, or manufacturing automation. Hardware transport and command compatibility must be verified against the actual unit.

## License

MIT — see [LICENSE](LICENSE).


## Hardware verification status (2026-10-30)

**Confirmed:** Windows enumerates the adapter as USB-SERIAL CH340 (COM8), the application opens COM8, and the Matrix replies to `*IDN?` with `MATRIX,WPS300S-15005,1.1`.

**Still to verify:** `MEAS:VCM?` measurement responses, remote/local control, output-state queries, setpoint changes, and output-off behavior. The initial separate measurement queries timed out; the combined query is a subsequent code change and has not yet been verified on the instrument. A successful identity query does not prove the other commands work. Do not rely on a software OFF request as a hardware safety interlock.
