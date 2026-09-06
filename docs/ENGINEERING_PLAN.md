# Engineering Implementation Plan

## Objective

Turn the current USB-to-CAN/CAN-FD bridge into a more capable and production-friendly tool without compromising the existing protocol or exceeding the current MCU resource envelope.

## Current resource budget

Measured build footprint:

- Flash: 41,204 bytes (`.text`) + 388 bytes (`.data`) = 41,592 bytes total
- RAM: 9,340 bytes (`.bss`)

Available capacity on STM32G431C8:

- Flash: 128 KB total = 131,072 bytes
- RAM: 32 KB total = 32,768 bytes

Remaining headroom:

- Flash: approximately 89,480 bytes
- RAM: approximately 23,428 bytes

This is sufficient for incremental feature growth, but any capture buffer, large diagnostic payloads, or OTA staging logic should be explicitly bounded and tuned against RAM availability.

---

## Design principles

1. Maintain backward compatibility with the current USB frame protocol.
2. Keep changes localized to the bridge stack: parser, CAN logic, and USB handling.
3. Prefer ring buffers and bounded queues over large dynamic allocations.
4. Add features in phases so each one can be validated independently.
5. Keep host-side protocol changes documented in the frame specification before implementation.

---

## Phase 1: Receive filtering and routing

### Goal

Reduce upstream traffic and make the bridge act as a smarter device instead of a raw pass-through.

### Scope

Add host-configurable filters for:

- standard vs extended IDs
- single-ID or range filters
- CAN vs CAN-FD filtering
- optional message-class filtering
- optional data-pattern filters

### Proposed commands

- `CMD_SET_RX_FILTER` (new)
- `CMD_CLEAR_RX_FILTER` (new)
- `CMD_GET_RX_FILTER` (new)

### Data model

Use a small fixed-size filter table in RAM, for example:

- up to 8 or 16 filter entries
- each entry includes:
  - match mode
  - ID value
  - mask or range
  - direction
  - frame type

### Files likely involved

- `firmware/Core/Inc/frameParser.h`
- `firmware/Core/Src/frameParser.c`
- `firmware/Core/Inc/canParser.h`
- `firmware/Core/Src/canParser.c`
- `firmware/FRAME_SPECIFICATION.md`
- `README.md`

### Expected impact

- Flash: roughly +2-5 KB
- RAM: roughly +0.5-2 KB depending on filter table size

### Exit criteria

- host can configure filters and receive only matching messages
- default pass-through mode remains unchanged
- no regression in normal CAN traffic flow

---

## Phase 2: Diagnostics and health reporting

### Goal

Expose more useful bus-state and health information to the host without changing the board hardware.

### Scope

Expand the existing stats output to include:

- RX/TX throughput counters
- bus-off transitions
- error passive transitions
- last error code history
- time since last reset
- runtime counters for receive/transmit events

### Proposed commands

- `CMD_GET_CAN_STATS` (extend existing payload format)
- `CMD_GET_PROTOCOL_STATUS` (already present; extend meaningfully)
- `CMD_RESET_CAN_STATS` (already present; keep supported)

### Data model

Track counters in RAM with a compact structure, for example:

- tx_count
- rx_count
- tx_error_count
- rx_error_count
- bus_off_count
- passive_count
- last_error_code
- last_timestamp

### Files likely involved

- `firmware/Core/Src/canParser.c`
- `firmware/Core/Inc/canParser.h`
- `firmware/Core/Src/frameParser.c`
- `firmware/FRAME_SPECIFICATION.md`

### Expected impact

- Flash: roughly +1-3 KB
- RAM: roughly +0.5-1 KB

### Exit criteria

- host can request current diagnostics payload
- counters update continuously while the bridge is active
- status reports are stable and low-noise

---

## Phase 3: Scheduled transmit and replay engine

### Goal

Turn the bridge into a useful test instrument by supporting timed transmissions.

### Scope

Add a command-driven scheduler for:

- periodic transmit of CAN/CAN-FD frames
- delayed message sends
- replay of a previously captured or host-supplied list
- repeating sequences with configurable intervals

### Proposed commands

- `CMD_TX_SCHEDULE_ADD`
- `CMD_TX_SCHEDULE_CLEAR`
- `CMD_TX_SCHEDULE_START`
- `CMD_TX_SCHEDULE_STOP`
- `CMD_TX_SCHEDULE_STATUS`

### Data model

Keep the scheduler small and fixed-size, for example:

- up to 16 schedule entries
- each entry includes:
  - message ID
  - DLC/data
  - delay or period
  - repeat count
  - frame type and BRS settings

### Files likely involved

- `firmware/Core/Src/frameParser.c`
- `firmware/Core/Src/canParser.c`
- `firmware/Core/Inc/canParser.h`
- `firmware/Core/Inc/frameParser.h`
- `firmware/FRAME_SPECIFICATION.md`

### Expected impact

- Flash: roughly +3-6 KB
- RAM: roughly +1-3 KB depending on queue depth

### Exit criteria

- scheduled messages are sent at the requested rate
- replayed frames remain correctly timestamped and sequenced
- scheduler does not disrupt normal receive traffic handling

---

## Phase 4: Capture buffer and host-side logging export

### Goal

Add lightweight trace/logging capabilities without requiring extra hardware or GPIO triggers.

### Scope

Implement a capture mode that stores recent CAN traffic in RAM or a bounded flash-backed ring, with export to the host over USB.

### Features

- timestamped RX capture
- optional filtering by ID or direction
- trigger on message count, specific ID, or error state
- export in CSV, JSON, or a structured binary trace format

### Data model

Use a fixed-size ring buffer, for example:

- 2 KB, 4 KB, or 8 KB capture buffer
- each entry stores a timestamp + CAN header + data length + payload

Important: this phase should be designed around a configurable buffer size so memory remains controlled.

### Files likely involved

- `firmware/Core/Src/canParser.c`
- `firmware/Core/Inc/canParser.h`
- `firmware/Core/Src/frameParser.c`
- `firmware/FRAME_SPECIFICATION.md`
- `README.md`

### Expected impact

- Flash: roughly +2-5 KB
- RAM: roughly +2-8 KB depending on capture depth

### Exit criteria

- host can enable capture and read a bounded trace log over USB
- log export is deterministic and easy to parse
- buffer overflow is counted and reported

---

## Phase 5: Secure OTA updates over USB

### Goal

Create a safer and more operationally friendly firmware update path.

### Scope

Extend the current DFU capability with:

- version validation
- signed firmware images or secure hash verification
- rollback support
- update status reporting
- staged application image handling

### Design note

The current project already includes a DFU trigger path in `main.c` and the docs describe the ROM bootloader flow. This feature should build on that path rather than replacing it.

### Files likely involved

- `firmware/Core/Src/main.c`
- `docs/DFU_IMPLEMENTATION.md`
- `firmware/FRAME_SPECIFICATION.md`
- `firmware/Core/Src/frameParser.c`
- linker script and startup-related files if the update model is expanded

### Expected impact

- Flash: +2-8 KB depending on verification logic and staging logic
- RAM: +0.5-2 KB for staged update metadata and buffers

### Exit criteria

- firmware image can be validated before activation
- version mismatch and corrupt-image cases are handled safely
- update flow remains recoverable if the process is interrupted

---

## Recommended execution order

1. Phase 1 — receive filtering and routing
2. Phase 2 — diagnostics and health reporting
3. Phase 3 — scheduled transmit and replay
4. Phase 4 — capture buffer and logging export
5. Phase 5 — secure OTA update support

This sequence maximizes product value while keeping the design aligned to the board’s real hardware and the current resource profile.

---

## Risk management

### Memory pressure

The key risk is RAM growth. Keep these features bounded with:

- fixed-size tables
- configurable buffer sizes
- ring buffers instead of large linear buffers
- compile-time constants for queue depth and capture depth

### Protocol compatibility

Any change to messages or command IDs must be reflected in the frame specification and host-side tools at the same time.

### Reliability

Because this is firmware running on a CAN bridge, every new feature should be tested against:

- normal traffic without filtering
- error states and bus-off conditions
- USB throughput under load
- large packet bursts and queue state transitions

---

## Success metrics

At the end of each phase, the feature should satisfy:

- it is usable through the USB protocol
- it does not break default bridge behavior
- it is documented in the protocol spec
- it is bounded by explicit memory limits
- it has a clear validation procedure on real hardware

---

## Summary

The current firmware has healthy remaining headroom, so incremental feature development is feasible. The best path is to strengthen the bridge itself first, then add diagnostics, scheduling, capture, and finally OTA support. This sequence keeps the project practical, testable, and aligned with the board’s real hardware capabilities.
