# Phase 1 Implementation Checklist

## Feature

Configure the STM32G431C8 FDCAN hardware filter RAM for upstream receive filtering.

## Goal

Allow the host to program a set of FDCAN receive filters so that only selected CAN/CAN-FD messages are forwarded over USB. This should use the MCU hardware filter block instead of a generic software-only filtering loop.

---

## 1. Protocol and specification updates

- [ ] Review [firmware/FRAME_SPECIFICATION.md](../firmware/FRAME_SPECIFICATION.md)
- [ ] Define new filter command IDs and payload layout
- [ ] Document the supported filter modes and ID-type limits
- [ ] Add notes for the 128 standard-ID and 64 extended-ID hardware limits
- [ ] Update the command list in [README.md](../README.md) if user-visible behavior changes
- [ ] Confirm backward compatibility for existing firmware and host tools

### Proposed command set

- [ ] `CMD_SET_RX_FILTER`
- [ ] `CMD_CLEAR_RX_FILTER`
- [ ] `CMD_GET_RX_FILTER`

### Firmware constraints to document

- [ ] Standard filters: max 128 entries
- [ ] Extended filters: max 64 entries
- [ ] No unsupported mode is allowed by the host command
- [ ] Default behavior when no filters are configured remains pass-through

---

## 2. Hardware filter data model

- [ ] Add hardware filter configuration definitions in [firmware/Core/Inc/canParser.h](../firmware/Core/Inc/canParser.h)
- [ ] Define `idType` enum: standard vs extended
- [ ] Define `filterMode` enum: disable, exact ID, mask
- [ ] Define a software mirror structure for validation/reporting
- [ ] Define the filter count limit per ID type

### Suggested mirror fields

- [ ] enabled flag
- [ ] idType
- [ ] mode
- [ ] id value
- [ ] mask value
- [ ] filter index

---

## 3. Parser and command handling

- [ ] Update [firmware/Core/Inc/frameParser.h](../firmware/Core/Inc/frameParser.h) with the new command constants
- [ ] Add parser cases in [firmware/Core/Src/frameParser.c](../firmware/Core/Src/frameParser.c)
- [ ] Validate host payload length and field validity
- [ ] Reject unsupported modes cleanly
- [ ] Return explicit error codes for invalid index and invalid limit
- [ ] Add a clear-all filter command path
- [ ] Add a get-filter status path for host introspection

### Parser expectations

- [ ] Invalid index is rejected
- [ ] Invalid ID type is rejected
- [ ] Unsupported filter mode returns an error
- [ ] Filter count exceeds hardware capacity is rejected
- [ ] Clear-all resets hardware configuration and mirror state

---

## 4. FDCAN filter programming

- [ ] Add hardware filter programming helper functions in [firmware/Core/Src/canParser.c](../firmware/Core/Src/canParser.c)
- [ ] Validate filter index against the standard and extended hardware limits
- [ ] Convert host request fields to HAL FDCAN filter configuration
- [ ] Program the filter into the FDCAN filter RAM
- [ ] Keep a mirror copy of what was successfully configured
- [ ] Clear or reconfigure filters without corrupting CAN startup state

### Suggested helper functions

- [ ] `CAN_SetHardwareFilter()`
- [ ] `CAN_ClearHardwareFilter()`
- [ ] `CAN_ClearAllHardwareFilters()`
- [ ] `CAN_ValidateFilterConfig()`

---

## 5. CAN startup and receive flow integration

- [ ] Review [firmware/Core/Src/main.c](../firmware/Core/Src/main.c) for CAN initialization timing
- [ ] Ensure filter configuration is applied after FDCAN init and before normal traffic is processed
- [ ] Confirm that filter configuration can be updated without requiring a full controller reset
- [ ] Ensure `CANRX_Process()` remains focused on accepted hardware messages only
- [ ] Preserve default pass-through when filters are empty

### Required behavior

- [ ] `FDCAN` accepts only configured IDs when filters are active
- [ ] `CANRX_Process()` does not need a large software matching loop for the primary filter decision
- [ ] Standard and extended ID filters are both supported
- [ ] CAN-FD and classic CAN filtering remain consistent with filter mode selection

---

## 6. Validation and testing

- [ ] Validate a default no-filter case: all messages forwarded
- [ ] Validate one standard exact-ID filter: only matching ID passes
- [ ] Validate one extended exact-ID filter: only matching extended ID passes
- [ ] Validate one standard mask filter: only matching subset passes
- [ ] Validate one extended mask filter: only matching subset passes
- [ ] Validate invalid index handling
- [ ] Validate invalid mode handling
- [ ] Validate clear-all operation restores default pass-through behavior

### Real hardware checks

- [ ] Filter config survives `CAN_START` and `CAN_STOP`
- [ ] No filter corruption occurs after repeated updates
- [ ] USB traffic remains stable under filtered receive traffic
- [ ] No unexpected packet loss happens during filter reconfiguration

---

## 7. Memory and performance validation

- [ ] Confirm the mirror structures remain small and fixed-sized
- [ ] Confirm no dynamic allocation is introduced
- [ ] Confirm the implementation does not increase the receive hot path unnecessarily
- [ ] Confirm Flash growth stays within available headroom
- [ ] Confirm the code still fits within the current runtime memory profile

---

## 8. Error handling and edge cases

- [ ] Reject filter index beyond hardware maximum
- [ ] Reject unsupported `idType` values
- [ ] Reject unsupported `mode` values
- [ ] Handle partial invalid payloads safely
- [ ] Preserve CAN operation if a filter update fails
- [ ] Return a defined status code for all failure paths

---

## 9. Documentation updates

- [ ] Update [firmware/FRAME_SPECIFICATION.md](../firmware/FRAME_SPECIFICATION.md)
- [ ] Add command descriptions and example payloads
- [ ] Document the hardware limits and supported modes
- [ ] Update [README.md](../README.md) with a short note on hardware filtering capability
- [ ] Add an example of enabling a standard ID filter
- [ ] Add an example of clearing all filters

---

## 10. Definition of done

Phase 1 is complete only when all of the following are true:

- [ ] The device can configure and clear FDCAN receive filters through the USB protocol
- [ ] Standard and extended ID filters work on real hardware
- [ ] The firmware respects the MCU limits of 128 standard filters and 64 extended filters
- [ ] Default pass-through behavior remains unchanged when no filters are configured
- [ ] Invalid configuration requests are rejected cleanly
- [ ] Documentation and command protocol match the actual implementation
- [ ] Hardware validation is completed with the board connected to a CAN bus

---

## 11. Suggested implementation order

1. [ ] Add protocol constants and data structures
2. [ ] Add filter validation and programming helpers
3. [ ] Implement parser command handlers
4. [ ] Apply filter configuration at CAN startup/update time
5. [ ] Validate standard and extended ID filtering on hardware
6. [ ] Update protocol docs and host examples
7. [ ] Final review of limits, errors, and stability

## Notes

This phase should be implemented around the FDCAN hardware capabilities, not as a generic software filter abstraction. That is the correct embedded design for the STM32G431C8 and aligns with the project’s existing architecture and runtime constraints.
