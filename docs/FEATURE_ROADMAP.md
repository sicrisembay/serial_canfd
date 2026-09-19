# Feature Roadmap

This document captures five strong feature ideas for the WebSerial CAN-FD bridge project, ordered by practical value and alignment with the current architecture.

## 1. Configurable CAN receive filtering and routing

Add support for host-defined receive filters based on:

- standard vs extended ID
- ID range masks
- selected message classes
- optional data-byte matching
- routing rules for forwarding only selected traffic

### Why this is valuable

- reduces unnecessary USB traffic
- allows the device to behave as a smarter bridge instead of a raw pass-through
- useful in automotive, industrial, bench, and diagnostics workloads

### Potential impact

This feature would improve efficiency and make the device more usable in real-world CAN networks where not every message is relevant.

---

## 2. Scheduled transmit and replay mode

Allow the device to queue and send CAN messages on a schedule, including:

- periodic message transmission
- delayed send timing
- replay of a recorded sequence
- configurable intervals and repetition counts

### Why this is valuable

- supports automated bus testing
- helps with ECU simulation and validation
- enables regression-style playback for development and calibration workflows

### Potential impact

This would turn the bridge into a more complete test instrument, not just a transport layer.

---

## 3. Rich bus health and diagnostics reporting

Expand the existing CAN statistics with:

- RX/TX throughput counters
- bus-off and error-passive transitions
- last error code history
- per-error type counts
- uptime and reset-cause reporting

### Why this is valuable

- makes troubleshooting easier
- gives host tools a real-time health dashboard
- improves field diagnostics and debug visibility

### Potential impact

This would make the project significantly more useful in production and lab environments where reliability matters.

---

## 4. Secure OTA firmware updates over USB

Improve the current DFU flow by adding:

- firmware signature verification
- version compatibility checks
- rollback support
- progress and status reporting during flashing

### Why this is valuable

- better security for firmware upgrades
- easier deployment and maintenance
- more reliable production usage

### Potential impact

This would make firmware updates safer and more operationally manageable for deployed devices.

---

## 5. Message capture buffer and host-side logging export

Add a persistent or ring-buffered capture mode that stores selected CAN traffic and supports export over USB, including:

- timestamped capture of RX frames
- optional filtering by ID, frame type, or direction
- trigger on message count, ID match, or error condition
- export to a file-friendly format such as CSV, JSON, or a binary trace log

### Why this is valuable

- simplifies debugging and post-analysis
- helps with root-cause investigations on noisy CAN networks
- gives host tools a structured capture workflow without needing extra hardware

### Potential impact

This expands the project from a live bridge into a practical CAN analyzer and logging tool while staying fully compatible with the existing board design.

---

## Recommended priority order

Given the hardware constraints of the current board, the most practical development order is:

1. Configurable CAN receive filtering and routing
2. Rich bus health and diagnostics reporting
3. Scheduled transmit and replay mode
4. Message capture buffer and host-side logging export
5. Secure OTA firmware updates over USB

This order prioritizes features that improve the bridge’s usability, observability, and test value while avoiding dependency on unavailable board-level I/O. The capture and OTA features are still valuable, but they build better after the core live-bridge and diagnostics capabilities are stronger.

## Summary

The strongest feature additions are the ones that keep the device useful as a CAN bridge while adding intelligence, observability, and automation. Receive filtering, diagnostics, and replay support are the most compelling next steps for this board, while message capture and OTA updates are excellent follow-on enhancements.
