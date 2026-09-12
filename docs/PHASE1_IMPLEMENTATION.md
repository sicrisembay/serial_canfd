# Phase 1 Implementation: FDCAN Hardware Receive Filtering

This phase is based on the actual capabilities of the STM32G431C8 MCU. The correct approach is to use the built-in FDCAN filter RAM rather than implementing a generic software-only routing layer.

The goal is to let the host configure a set of receive filters in the MCU hardware so only selected CAN/CAN-FD traffic is forwarded over USB.

---

## 1. Hardware capability to exploit

The STM32G431C8 has one FDCAN peripheral with hardware filter memory. The practical limits are:

- up to 128 filter elements for standard 11-bit IDs
- up to 64 filter elements for extended 29-bit IDs

This means the device should treat FDCAN filtering as the primary mechanism for RX filtering, with a small software mirror for configuration tracking and validation.

---

## 2. Design objective

The host should be able to configure filters for:

- exact standard ID match
- exact extended ID match
- mask-based filtering
- range/group filtering where supported by the hardware mode
- filter enable/disable
- filter reset / clear-all

The default behavior remains unchanged: if no filters are configured, the device forwards all receive traffic upstream.

---

## 3. Protocol design

### New command IDs

Add the following command IDs to `frameParser.h`:

```c
#define CMD_SET_RX_FILTER       (0x15)
#define CMD_CLEAR_RX_FILTER     (0x16)
#define CMD_GET_RX_FILTER       (0x17)
```

These fit the existing command space and keep the protocol consistent with the current design.

### Filter configuration model

The host command should configure a hardware filter entry, not an arbitrary software match object.

Payload fields for `CMD_SET_RX_FILTER` should include:

```text
Payload[0] = CMD_SET_RX_FILTER
Payload[1] = filterIndex
Payload[2] = enabled
Payload[3] = idType         // 0=standard, 1=extended
Payload[4] = filterMode     // 0=disable, 1=mask, 2=range/list
Payload[5] = fifoTarget     // optional, usually RX FIFO 0
Payload[6..9] = idValue     // 32-bit little-endian
Payload[10..13] = maskValue // 32-bit little-endian
Payload[14..17] = rangeMin  // optional for range mode
Payload[18..21] = rangeMax  // optional for range mode
```

For the first implementation, the simplest supported modes should be:

- exact standard ID match
- exact extended ID match
- mask-based standard ID match
- mask-based extended ID match

Range mode can be added later if the HAL capability and filter structure require it.

---

## 4. Firmware data model

Add a compact configuration mirror in `canParser.h`.

```c
#define RX_FILTER_MAX_STANDARD  (128U)
#define RX_FILTER_MAX_EXTENDED  (64U)

typedef enum {
    RX_FILTER_ID_STD = 0,
    RX_FILTER_ID_EXT = 1
} RxFilterIdType_t;

typedef enum {
    RX_FILTER_MODE_DISABLE = 0,
    RX_FILTER_MODE_ID = 1,
    RX_FILTER_MODE_MASK = 2
} RxFilterMode_t;

typedef struct {
    uint8_t enabled;
    uint8_t idType;
    uint8_t mode;
    uint32_t id;
    uint32_t mask;
} RxFilterConfig_t;
```

Then declare:

```c
static RxFilterConfig_t stdRxFilters[RX_FILTER_MAX_STANDARD];
static RxFilterConfig_t extRxFilters[RX_FILTER_MAX_EXTENDED];
```

This mirror is not the actual filtering engine. It is used for:

- validation
- reporting status to the host
- reconstructing the hardware filter list
- tracking what was successfully programmed

---

## 5. FDCAN hardware configuration strategy

The actual filtering should be configured through the STM32 HAL FDCAN filter API, for example in the style of:

```c
HAL_FDCAN_ConfigFilter(&hfdcan1, &filterConfig);
```

### Hardware filter setup rules

- Standard IDs share a single standard filter bank up to 128 entries
- Extended IDs share a single extended filter bank up to 64 entries
- Each filter entry must be validated before programming
- An invalid filter index or unsupported mode must return an error response
- The host must not exceed the allowed number of filters for the selected ID type

### Recommended validation logic

```c
if (idType == RX_FILTER_ID_STD && filterIndex >= RX_FILTER_MAX_STANDARD) {
    return HAL_ERROR;
}

if (idType == RX_FILTER_ID_EXT && filterIndex >= RX_FILTER_MAX_EXTENDED) {
    return HAL_ERROR;
}
```

### Clear-all behavior

Implement `CMD_CLEAR_RX_FILTER` by clearing the software mirror and reinitializing the FDCAN filter RAM to the default accept-all configuration, or by disabling all programmed filters in the hardware.

---

## 6. Runtime flow

The receive path should remain simple. The real filtering decision happens in hardware, so we do not insert a large software match loop in `CANRX_Process()`.

The flow becomes:

1. host updates filter configuration via USB command
2. firmware validates and programs hardware FDCAN filters
3. FDCAN accepts or rejects incoming frames before software copies them
4. `CANRX_Process()` only handles messages that are already accepted by hardware
5. the host sees only the filtered stream

This is the correct usage pattern for the MCU and is much lighter than a software filter table.

---

## 7. New command handlers

Add the new command cases in `frameParser.c`.

### `CMD_SET_RX_FILTER`

- parse the filter payload
- validate `filterIndex`
- validate `idType`
- validate `mode`
- validate index range against standard/extended limits
- call the filter programming routine
- reply with status code

### `CMD_CLEAR_RX_FILTER`

- clear the hardware filter configuration for the selected entry or all entries
- clear the software mirror state
- reply with success/error status

### `CMD_GET_RX_FILTER`

- return the currently configured filter count and/or the selected filter definition
- used for host-side introspection and debugging

---

## 8. Suggested implementation structure

### Add new functions in `canParser.c`

```c
static HAL_StatusTypeDef CAN_SetHardwareFilter(uint8_t filterIndex,
                                              uint8_t idType,
                                              uint8_t mode,
                                              uint32_t id,
                                              uint32_t mask);

static HAL_StatusTypeDef CAN_ClearHardwareFilter(uint8_t filterIndex,
                                                uint8_t idType);

static HAL_StatusTypeDef CAN_ClearAllHardwareFilters(void);
static void CAN_ApplyFilterMirror(void);
```

These functions keep the hardware programming logic isolated and easier to test.

---

## 9. Implementation sequence

### Step 1: Define hardware filter limits

Update `canParser.h` with constants and enumerations.

### Step 2: Declare filter mirror state

Add the `stdRxFilters` and `extRxFilters` arrays in `canParser.c`.

### Step 3: Add FDCAN filter programming helpers

Implement helper functions that convert the host request into the HAL filter configuration.

### Step 4: Add parser command handlers

Add `CMD_SET_RX_FILTER`, `CMD_CLEAR_RX_FILTER`, and `CMD_GET_RX_FILTER` to `frameParser.c`.

### Step 5: Initialize filter state on startup

When the CAN controller starts, initialize the filter RAM to a pass-through default or leave the filter list empty.

### Step 6: Validate hardware behavior

Test with actual traffic on the CAN bus and confirm that only configured IDs are forwarded.

### Step 7: Update docs

Update the protocol docs and usage docs to reflect the hardware filter model and the limits.

---

## 10. Validation scenarios

### Required tests

- [ ] no filters configured -> all CAN traffic is forwarded
- [ ] one standard ID filter -> only matching standard ID is received
- [ ] one extended ID filter -> only matching extended ID is received
- [ ] mask-based standard filter -> expected IDs pass
- [ ] invalid filter index -> explicit error response
- [ ] clear-all -> device returns to default pass-through mode
- [ ] filter count above limit -> explicit error response

### Real hardware checks

- [ ] filter configuration survives CAN start/stop cycles
- [ ] the filter RAM does not corrupt other FDCAN settings
- [ ] USB throughput remains stable under filtered traffic
- [ ] no message loss occurs when filters are updated at runtime

---

## 11. Acceptance criteria for Phase 1

Phase 1 is complete only when:

- the host can configure standard and extended ID filters through USB
- the device programs the FDCAN hardware filter RAM rather than using a software-only filter loop
- invalid requests are rejected cleanly
- the default behavior is unchanged when no filters are configured
- the implementation respects the MCU limits of 128 standard filters and 64 extended filters
- protocol docs and host usage examples match the implemented commands

---

## 12. Final design note

This Phase 1 feature is not a generic software router. It is a hardware-filter feature for the STM32G431C8 FDCAN peripheral. That is the correct fit for the MCU and the most efficient design for the product.

The software mirror should be minimal and support configuration validation, not duplicate the actual filtering decision.
