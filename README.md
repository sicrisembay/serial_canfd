# WebSerial CAN-FD Bridge

This repository contains the firmware for an STM32G431C8TX-based USB-to-CAN/CAN-FD bridge. The device presents a USB CDC virtual serial port and relays CAN traffic between a host application and the CAN bus using a compact binary frame protocol.

## Overview

The firmware is designed for host-driven CAN monitoring, diagnostics, and testing. It exposes a simple serial interface over USB and supports both classic CAN and CAN-FD messages, including standard and extended identifiers, bus error reporting, hardware receive filtering, and a reset-to-DFU flow for firmware updates.

The code is intentionally focused on a small embedded runtime loop that:

- accepts framed USB commands
- validates and dispatches them
- transmits CAN/CAN-FD messages
- forwards received messages upstream over USB
- reports protocol and error state back to the host

## Features

- USB CDC virtual COM port interface
- CAN classic and CAN-FD support
- Standard and extended CAN IDs
- Bit Rate Switch (BRS) support for CAN-FD
- Hardware RX filter configuration and query support
- CAN error statistics and protocol status notifications
- Ring-buffered USB/CAN handling
- DFU entry via reset into the STM32 ROM bootloader

## Hardware target

- MCU: STM32G431C8TX
- Core: ARM Cortex-M4 with FPU
- Flash: 128 KB
- SRAM: 32 KB
- CAN controller: FDCAN1
- USB: USB 2.0 Full-Speed CDC device
- Timestamp source: TIM2

## Repository layout

```text
webserial_canfd/
├── AGENT.md                     # Agent operating guidance for this repo
├── README.md                    # High-level overview and usage
├── firmware/
│   ├── FRAME_SPECIFICATION.md   # Authoritative binary protocol definition
│   ├── webserial_canfd.ioc      # STM32CubeMX configuration
│   ├── Core/
│   │   ├── Inc/
│   │   │   ├── canParser.h
│   │   │   ├── frameParser.h
│   │   │   ├── main.h
│   │   │   └── UTIL_ringbuf.h
│   │   └── Src/
│   │       ├── canParser.c
│   │       ├── frameParser.c
│   │       ├── main.c
│   │       ├── system_stm32g4xx.c
│   │       └── UTIL_ringbuf.c
│   ├── USB_Device/
│   │   ├── App/
│   │   └── Target/
│   ├── Drivers/
│   ├── Middlewares/
│   └── Debug/
├── docs/
│   ├── DFU_IMPLEMENTATION.md
│   ├── ENGINEERING_PLAN.md
│   ├── FEATURE_ROADMAP.md
│   └── PHASE1_CHECKLIST.md
└── ...
```

## Frame protocol summary

The device uses a framed binary protocol over USB. The protocol definition is authoritative in [firmware/FRAME_SPECIFICATION.md](firmware/FRAME_SPECIFICATION.md), and the implementation is in [firmware/Core/Src/frameParser.c](firmware/Core/Src/frameParser.c).

Each frame includes:

| Field | Offset | Size | Description |
|---|---:|---:|---|
| TAG | 0 | 1 byte | Start-of-frame marker (`0xFF`) |
| Length | 1 | 2 bytes | Total frame length, little-endian |
| Timestamp | 3 | 4 bytes | 10 µs timer value |
| Packet Seq | 7 | 2 bytes | Frame sequence number |
| Payload | 9 | N bytes | Command and payload data |
| Checksum | 9 + N | 1 byte | Two's complement checksum |

Key limits:

- minimum valid frame length: 10 bytes
- maximum frame length: 1023 bytes
- maximum CAN-FD payload: 64 bytes

The parser expects little-endian encoding and validates the length and checksum before processing a command.

## Supported commands

The firmware currently exposes the following command IDs, as defined in [firmware/Core/Inc/frameParser.h](firmware/Core/Inc/frameParser.h):

| Command | ID | Purpose |
|---|---:|---|
| `CMD_GET_DEVICE_ID` | `0x00` | Read device identity and firmware version |
| `CMD_CAN_START` | `0x01` | Start FDCAN with the selected arbitration/data bitrates |
| `CMD_CAN_STOP` | `0x02` | Stop FDCAN |
| `CMD_DEVICE_RESET` | `0x03` | Reset the MCU |
| `CMD_SEND_DOWNSTREAM` | `0x10` | Transmit a CAN/CAN-FD frame to the bus |
| `CMD_SEND_UPSTREAM` | `0x11` | Device-to-host notification for received bus traffic |
| `CMD_PROTOCOL_STATUS` | `0x12` | Unsolicited protocol status notification |
| `CMD_GET_CAN_STATS` | `0x13` | Request or report CAN error statistics |
| `CMD_RESET_CAN_STATS` | `0x14` | Reset CAN error counters |
| `CMD_SET_RX_FILTER` | `0x15` | Configure one hardware receive filter |
| `CMD_CLEAR_RX_FILTER` | `0x16` | Disable a specific filter or clear all filters |
| `CMD_GET_RX_FILTER_COUNT` | `0x17` | Query count of active filters |
| `CMD_GET_RX_FILTER_INFO` | `0x18` | Query detailed filter state |
| `CMD_ENTER_DFU` | `0xF0` | Reset into the STM32 ROM DFU bootloader |

> The auto-generated `SEND_UPSTREAM` and `PROTOCOL_STATUS` frames are not host requests; they are device notifications sent when incoming traffic or bus state changes are detected.

## Build and flash

### Prerequisites

- STM32CubeIDE or an ARM GCC toolchain
- ST-LINK debugger/programmer
- Optional: STM32CubeMX for configuration changes

### Build with STM32CubeIDE

1. Open STM32CubeIDE.
2. Import the project by selecting the `firmware` directory.
3. Build the project with `Project -> Build All`.
4. The resulting ELF/binary is generated under the `Debug` output folder.

### Build from the command line

```bash
cd firmware/Debug
make all
```

### Flashing

1. Connect the STM32 board to the ST-LINK.
2. Use STM32CubeProgrammer or STM32CubeIDE to flash the generated firmware image.
3. Reset or run the target.

## Quick start

1. Program the board with the firmware.
2. Connect the USB cable; the device enumerates as a CDC virtual serial port.
3. Connect the CAN or CAN-FD bus to FDCAN1.
4. Open a serial client or browser-based serial app.
5. Send valid framed commands using the command IDs described above.

A common workflow is:

- issue `CMD_CAN_START` with arbitration/data bitrate selections
- send `CMD_SEND_DOWNSTREAM` frames to transmit onto the bus
- receive `CMD_SEND_UPSTREAM` notifications for incoming traffic
- query `CMD_GET_CAN_STATS` when error state needs checking

## CAN configuration notes

The runtime configuration is generated from the FDCAN setup in [firmware/Core/Src/main.c](firmware/Core/Src/main.c) and the hardware project file [firmware/webserial_canfd.ioc](firmware/webserial_canfd.ioc).

The project currently supports:

- nominal bitrate selection for CAN arbitration phase
- data bitrate selection for CAN-FD data phase
- standard or extended hardware receive filters
- filtered receive logic at the FDCAN level

The implementation keeps filter state in RAM and re-applies it when the CAN peripheral is restarted.

## USB DFU bootloader support

The firmware includes a DFU entry path that sets a magic word in `.noinit` RAM and performs a reset. On the next startup, the bootloader check in [firmware/Core/Src/main.c](firmware/Core/Src/main.c) detects the magic value and jumps into the STM32 ROM bootloader.

This flow is documented in [docs/DFU_IMPLEMENTATION.md](docs/DFU_IMPLEMENTATION.md).

## Development notes

The runtime loop is intentionally simple and polling-oriented:

```text
main loop:
  CDC_ProcessTx()
  PARSER_Process()
  CANTX_Process()
  CANRX_Process()
  CANErr_Process()
```

The main implementation points are:

- [firmware/Core/Src/main.c](firmware/Core/Src/main.c) — startup, clock config, USB init, runtime loop, DFU entry
- [firmware/Core/Src/frameParser.c](firmware/Core/Src/frameParser.c) — frame receive parsing, command dispatch, response generation
- [firmware/Core/Src/canParser.c](firmware/Core/Src/canParser.c) — CAN transmission, reception, error handling, and filter logic
- [firmware/Core/Inc/frameParser.h](firmware/Core/Inc/frameParser.h) — core protocol constants and command IDs
- [firmware/Core/Inc/canParser.h](firmware/Core/Inc/canParser.h) — CAN and RX filter API definitions

## Documentation

Relevant documents in this repository:

- [firmware/FRAME_SPECIFICATION.md](firmware/FRAME_SPECIFICATION.md)
- [docs/DFU_IMPLEMENTATION.md](docs/DFU_IMPLEMENTATION.md)
- [docs/ENGINEERING_PLAN.md](docs/ENGINEERING_PLAN.md)
- [docs/FEATURE_ROADMAP.md](docs/FEATURE_ROADMAP.md)

## Licensing and contribution

This project includes STMicroelectronics software components under their licenses. See the source files and vendor directories for their respective notices.

Contributions are welcome, but protocol changes should be documented in [firmware/FRAME_SPECIFICATION.md](firmware/FRAME_SPECIFICATION.md) and kept consistent with the implementation in the firmware code.

## Note on BOOT configuration

On some boards, the BOOT0 pin shares routing with a CAN signal. In those cases, the board may require the flash option bits to be configured so that SWD/JTAG debugging remains usable while the application boots normally. This is a board-specific hardware setup concern rather than a firmware feature.
