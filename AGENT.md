# AGENT.md

## Purpose

This repository contains an STM32G431-based USB-to-CAN/CAN-FD bridge firmware. The device presents a USB CDC virtual serial port and exchanges a custom framed protocol with a host application while relaying CAN and CAN-FD traffic.

The goal of future changes is to preserve the bridge behavior, protocol compatibility, and hardware safety while keeping edits focused and easy to review.

## Repository map

- `README.md` – project overview and high-level usage
- `firmware/FRAME_SPECIFICATION.md` – authoritative protocol format and command layout
- `docs/DFU_IMPLEMENTATION.md` – DFU/bootloader design notes and procedures
- `firmware/Core/Inc/frameParser.h` – frame protocol constants and exported parser functions
- `firmware/Core/Inc/canParser.h` – CAN message and stats definitions
- `firmware/Core/Src/main.c` – startup, clock config, init, main loop, DFU jump logic
- `firmware/Core/Src/frameParser.c` – frame receive parsing, bitrate config, command dispatch
- `firmware/Core/Src/canParser.c` – CAN queueing, RX/TX processing, error logic
- `firmware/USB_Device/App/usbd_cdc_if.c` – USB CDC implementation
- `firmware/Drivers/` and `firmware/Middlewares/` – ST HAL and USB middleware, usually generated or vendor code

## Architecture summary

### Main runtime loop
`main.c` runs a polling loop that repeatedly calls:

- `CDC_ProcessTx()`
- `PARSER_Process()`
- `CANTX_Process()`
- `CANRX_Process()`
- `CANErr_Process()`

This is a simple embedded application pattern: parse USB frames, dispatch commands, queue CAN transmit packets, drain CAN RX FIFO, and emit status/notification frames.

### Frame protocol
The protocol is a framed binary format with:

- TAG (`0xFF`)
- 16-bit length, little-endian
- 32-bit timestamp
- 16-bit packet sequence
- payload
- 1-byte checksum (two's complement across all preceding bytes)

The protocol constants and payload offsets are defined in `frameParser.h` and are expected to stay consistent with the spec in `firmware/FRAME_SPECIFICATION.md`.

### CAN flow
- Host commands are parsed in `frameParser.c`
- Downstream CAN writes are queued via `CAN_Send()` and sent by `CANTX_Process()`
- Incoming CAN traffic is read in `CANRX_Process()` and forwarded upstream as `CMD_SEND_UPSTREAM`
- Error and protocol status changes are emitted from `CANErr_Process()`

## Project conventions

### 1. Keep protocol changes intentional and documented
If a command, payload field, or frame layout changes:

- update `firmware/FRAME_SPECIFICATION.md`
- update `frameParser.h` and any command definitions
- review affected logic in `frameParser.c` and `canParser.c`
- update `README.md` if the interface or usage changes

### 2. Prefer small, domain-focused edits
This codebase is compact but hardware-sensitive. Avoid broad refactors, AI-generated “cleanup” patches, or unrelated reformatting. Keep the change tightly scoped to the bug or feature being implemented.

### 3. Respect STM32 HAL conventions
This is embedded C for STM32. When editing low-level behavior:

- prefer the existing HAL patterns and naming
- keep interrupt/critical-section logic correct
- avoid unrealistic assumptions about thread mode
- do not silently bypass safety checks around FDCAN state or USB state transitions

### 4. Keep endianness and packet layout correct
The protocol is little-endian. Any change to offset constants, length logic, or payload encoding must preserve compatibility with the existing serializer/deserializer expectations.

### 5. Preserve DFU behavior
The DFU flow is intentionally implemented via a magic word in `.noinit` RAM and a reset into the ROM bootloader. Changes here must remain safe and must not interfere with normal startup.

## Operating rules for agents

- Read the protocol spec before changing frame semantics.
- Check `main.c`, `frameParser.c`, and `canParser.c` before making CAN/USB protocol edits.
- Do not rewrite generated STM32 code unless the change is required for the actual feature.
- Prefer minimal modifications with clear comments if a non-obvious hardware-specific change is introduced.
- If the change affects host compatibility, ensure the docs and examples stay aligned.

## Validation expectations

This repo is an STM32 firmware project, so validation should normally be done with the embedded toolchain used by the project (STM32CubeIDE, Make with ARM GCC, or a similar STM32 build flow). If a full hardware build is not possible in the current environment, still validate the code path as much as possible and explain any limitations.

## Suggested first reads for new work

1. `README.md`
2. `firmware/FRAME_SPECIFICATION.md`
3. `firmware/Core/Src/main.c`
4. `firmware/Core/Src/frameParser.c`
5. `firmware/Core/Src/canParser.c`

## Quick sanity checklist

Before finishing a patch, confirm:

- the wiring of USB payloads and CAN payloads still matches the protocol
- lengths, offsets, and checksum logic are still valid
- CAN state transitions remain safe
- any command added or modified is reflected in the docs
- the patch stays within the firmware project scope and does not disturb unrelated files

## Summary

This repository is a focused STM32 firmware bridge. The most important rule is: preserve the USB/CAN protocol contract while keeping the runtime behavior deterministic and hardware-safe.
