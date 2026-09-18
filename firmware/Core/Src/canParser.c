/*
 * canParser.c
 *
 *  Created on: Feb 26, 2026
 *      Author: Sicris
 */

#include "string.h"
#include "main.h"
#include "canParser.h"
#include "frameParser.h"

#define CANTX_Q_SIZE    (8)

volatile uint32_t canTxRdPtr = 0;
volatile uint32_t canTxWrPtr = 0;
static CanTx_t canTxSto[CANTX_Q_SIZE];

extern FDCAN_HandleTypeDef hfdcan1;

static CanStat_t canStat = {0};
static uint32_t can_tx_loss_packet_count = 0;
static RxFilterConfig_t stdRxFilters[RX_FILTER_MAX_STANDARD];
static RxFilterConfig_t extRxFilters[RX_FILTER_MAX_EXTENDED];

static uint8_t CAN_CountEnabledFilters(const RxFilterConfig_t *filterArray, uint32_t maxFilterCount)
{
    uint32_t i;
    uint8_t count = 0U;

    for(i = 0; i < maxFilterCount; i++) {
        if(filterArray[i].enabled != 0U) {
            count++;
        }
    }

    return count;
}

static HAL_StatusTypeDef CAN_ApplyGlobalFilter(void)
{
    HAL_StatusTypeDef sts;

    if((CAN_CountEnabledFilters(stdRxFilters, RX_FILTER_MAX_STANDARD) == 0U) &&
       (CAN_CountEnabledFilters(extRxFilters, RX_FILTER_MAX_EXTENDED) == 0U)) {
        sts = HAL_FDCAN_ConfigGlobalFilter(&hfdcan1,
                                          FDCAN_ACCEPT_IN_RX_FIFO0,
                                          FDCAN_ACCEPT_IN_RX_FIFO0,
                                          FDCAN_FILTER_REMOTE,
                                          FDCAN_FILTER_REMOTE);
        return sts;
    }

    sts = HAL_FDCAN_ConfigGlobalFilter(&hfdcan1,
                                      FDCAN_REJECT,
                                      FDCAN_REJECT,
                                      FDCAN_FILTER_REMOTE,
                                      FDCAN_FILTER_REMOTE);
    return sts;
}

static HAL_StatusTypeDef CAN_ProgramFilterElement(const RxFilterConfig_t *cfg)
{
    FDCAN_FilterTypeDef filterConfig;
    uint32_t standardMask = 0x7FFU;
    uint32_t extendedMask = 0x1FFFFFFFU;

    if((cfg == (const RxFilterConfig_t *)0) || (cfg->enabled == 0U)) {
        return HAL_OK;
    }

    memset(&filterConfig, 0, sizeof(filterConfig));
    filterConfig.IdType = (cfg->idType == RX_FILTER_ID_EXTENDED) ? FDCAN_EXTENDED_ID : FDCAN_STANDARD_ID;
    filterConfig.FilterIndex = cfg->filterIndex;
    filterConfig.FilterConfig = ((cfg->mode == RX_FILTER_MODE_DISABLE) ? FDCAN_FILTER_DISABLE : FDCAN_FILTER_TO_RXFIFO0);
    filterConfig.FilterType = FDCAN_FILTER_MASK;

    if(cfg->mode == RX_FILTER_MODE_ID) {
        filterConfig.FilterID1 = cfg->id;
        filterConfig.FilterID2 = (cfg->idType == RX_FILTER_ID_EXTENDED) ? extendedMask : standardMask;
    } else if(cfg->mode == RX_FILTER_MODE_MASK) {
        filterConfig.FilterID1 = cfg->id;
        filterConfig.FilterID2 = cfg->mask;
    } else {
        return HAL_ERROR;
    }

    if(filterConfig.IdType == FDCAN_STANDARD_ID) {
        filterConfig.FilterID1 &= 0x7FFU;
        filterConfig.FilterID2 &= 0x7FFU;
    } else {
        filterConfig.FilterID1 &= 0x1FFFFFFFU;
        filterConfig.FilterID2 &= 0x1FFFFFFFU;
    }

    return HAL_FDCAN_ConfigFilter(&hfdcan1, &filterConfig);
}


HAL_StatusTypeDef CAN_ApplyAllFilter(void)
{
    HAL_StatusTypeDef sts = HAL_OK;

    if(hfdcan1.State != HAL_FDCAN_STATE_READY) {
        return HAL_ERROR;
    }

    for(uint32_t i = 0; i < RX_FILTER_MAX_STANDARD; i++) {
        if(stdRxFilters[i].enabled == 0U) {
            continue;
        }
        sts = CAN_ProgramFilterElement(&stdRxFilters[i]);
        if(sts != HAL_OK) {
            return sts;
        }
    }

    for(uint32_t i = 0; i < RX_FILTER_MAX_EXTENDED; i++) {
        if(extRxFilters[i].enabled == 0U) {
            continue;
        }
        sts = CAN_ProgramFilterElement(&extRxFilters[i]);
        if(sts != HAL_OK) {
            return sts;
        }
    }

    return CAN_ApplyGlobalFilter();
}


HAL_StatusTypeDef CAN_SetRxFilter(uint8_t filterIndex,
                                 uint8_t idType,
                                 uint8_t mode,
                                 uint32_t id,
                                 uint32_t mask)
{
    uint32_t maxFilterCount = 0;
    RxFilterConfig_t *filterArray = (RxFilterConfig_t *)0;

    if(hfdcan1.State != HAL_FDCAN_STATE_READY) {
        return HAL_ERROR;
    }

    if((idType != RX_FILTER_ID_STANDARD) && (idType != RX_FILTER_ID_EXTENDED)) {
        return HAL_ERROR;
    }

    if((mode != RX_FILTER_MODE_ID) && (mode != RX_FILTER_MODE_MASK)) {
        return HAL_ERROR;
    }

    if(idType == RX_FILTER_ID_STANDARD) {
        maxFilterCount = RX_FILTER_MAX_STANDARD;
        filterArray = stdRxFilters;
    } else {
        maxFilterCount = RX_FILTER_MAX_EXTENDED;
        filterArray = extRxFilters;
    }

    if(filterIndex >= maxFilterCount) {
        return HAL_ERROR;
    }

    filterArray[filterIndex].enabled = 1U;
    filterArray[filterIndex].idType = idType;
    filterArray[filterIndex].mode = mode;
    filterArray[filterIndex].filterIndex = filterIndex;
    filterArray[filterIndex].id = id;
    filterArray[filterIndex].mask = mask;

    return HAL_OK;
}

HAL_StatusTypeDef CAN_ClearRxFilter(uint8_t filterIndex, uint8_t idType)
{
    uint32_t maxFilterCount = 0;
    RxFilterConfig_t *filterArray = (RxFilterConfig_t *)0;

    if(hfdcan1.State != HAL_FDCAN_STATE_READY) {
        return HAL_ERROR;
    }

    if((idType != RX_FILTER_ID_STANDARD) && (idType != RX_FILTER_ID_EXTENDED)) {
        return HAL_ERROR;
    }

    if(idType == RX_FILTER_ID_STANDARD) {
        maxFilterCount = RX_FILTER_MAX_STANDARD;
        filterArray = stdRxFilters;
    } else {
        maxFilterCount = RX_FILTER_MAX_EXTENDED;
        filterArray = extRxFilters;
    }

    if(filterIndex >= maxFilterCount) {
        return HAL_ERROR;
    }

    memset(&filterArray[filterIndex], 0, sizeof(RxFilterConfig_t));

    return HAL_OK;
}

HAL_StatusTypeDef CAN_ClearAllRxFilters(void)
{
    if(hfdcan1.State != HAL_FDCAN_STATE_READY) {
        return HAL_ERROR;
    }

    memset(stdRxFilters, 0, sizeof(stdRxFilters));
    memset(extRxFilters, 0, sizeof(extRxFilters));

    return HAL_OK;
}

uint8_t CAN_GetRxFilterCount(uint8_t idType)
{
    if(idType == RX_FILTER_ID_STANDARD) {
        return CAN_CountEnabledFilters(stdRxFilters, RX_FILTER_MAX_STANDARD);
    }
    if(idType == RX_FILTER_ID_EXTENDED) {
        return CAN_CountEnabledFilters(extRxFilters, RX_FILTER_MAX_EXTENDED);
    }
    return 0U;
}

HAL_StatusTypeDef CAN_GetRxFilterInfo(uint8_t filterIndex, uint8_t idType, RxFilterConfig_t * filterInfo)
{
    uint32_t maxFilterCount = 0;
    RxFilterConfig_t *filterArray = (RxFilterConfig_t *)0;

    if(filterInfo == (RxFilterConfig_t *)0) {
        return HAL_ERROR;
    }

    if((idType != RX_FILTER_ID_STANDARD) && (idType != RX_FILTER_ID_EXTENDED)) {
        return HAL_ERROR;
    }

    if(idType == RX_FILTER_ID_STANDARD) {
        maxFilterCount = RX_FILTER_MAX_STANDARD;
        filterArray = stdRxFilters;
    } else {
        maxFilterCount = RX_FILTER_MAX_EXTENDED;
        filterArray = extRxFilters;
    }

    if(filterIndex >= maxFilterCount) {
        return HAL_ERROR;
    }

    memcpy(filterInfo, &filterArray[filterIndex], sizeof(RxFilterConfig_t));

    return HAL_OK;
}

static bool CAN_txQ_full()
{
    return (((canTxWrPtr + 1) % CANTX_Q_SIZE) == canTxRdPtr);
}

static bool CAN_txQ_empty()
{
    return (canTxWrPtr == canTxRdPtr);
}

bool CAN_Send(CanTx_t * pCanTx)
{
	bool isOK = true;

    if(pCanTx == (CanTx_t *)0) {
        return false;
    }

    if(CAN_txQ_full()) {
        return false;
    }

    uint32_t isrContext = __get_IPSR() & 0x3F;
    uint32_t primask_bit = __get_PRIMASK();

    /* Enter Critical Section */
    if(isrContext == 0) {
        __disable_irq();
    }

    if(((canTxWrPtr + 1) % CANTX_Q_SIZE) == canTxRdPtr) {
        // Full
        isOK = false;
    } else {
        memcpy(&canTxSto[canTxWrPtr], pCanTx, sizeof(CanTx_t));
        canTxWrPtr = (canTxWrPtr + 1) % CANTX_Q_SIZE;
    }
    /* Exit Critical Section */
    if(isrContext == 0) {
        if(primask_bit == 0) {
            __enable_irq();
        }
    }

    return isOK;
}


void CANTX_Process(void)
{
    // Note: To avoid data race condition, this function is only
    // allowed to be called in Thread mode
    if ((__get_IPSR() & 0x3F) != 0) {
        // Not in thread mode
        Error_Handler();
    }

    if(hfdcan1.State == HAL_FDCAN_STATE_BUSY) {
        if(HAL_FDCAN_GetTxFifoFreeLevel(&hfdcan1) > 0) {
            if(!CAN_txQ_empty()) {
                CanTx_t canTx = canTxSto[canTxRdPtr];

                // Try to send to FDCAN hardware
                if(HAL_OK == HAL_FDCAN_AddMessageToTxFifoQ(&hfdcan1, &(canTx.header), canTx.data)) {
                    /* Enter Critical Section */
                    uint32_t primask_bit = __get_PRIMASK();
                    __disable_irq();

                    // Success - remove from queue
                    canTxRdPtr = (canTxRdPtr + 1) % CANTX_Q_SIZE;

                    /* Exit Critical Section */
                    if(primask_bit == 0) {
                        __enable_irq();
                    }
                } else {
                    // Failed - keep packet in queue for retry next time
                    can_tx_loss_packet_count++;
                }

            }
        }
    }
}


void CANRX_Process(void)
{
    // Note: To avoid data race condition, this function is only
    // allowed to be called in Thread mode

    if ((__get_IPSR() & 0x3F) != 0) {
        // Not in thread mode
        Error_Handler();
    }

    if(hfdcan1.State == HAL_FDCAN_STATE_BUSY) {
        while(HAL_FDCAN_GetRxFifoFillLevel(&hfdcan1, FDCAN_RX_FIFO0) > 0) {
            FDCAN_RxHeaderTypeDef rxHeader;
            uint8_t rxData[CONFIG_CANFD_DATA_SIZE];

            if(HAL_FDCAN_GetRxMessage(&hfdcan1, FDCAN_RX_FIFO0, &rxHeader, rxData) == HAL_OK) {
                // Process received message
                /*
                 * RX_TYPE
                 *  bit0: 0 - CAN-CC
                 *        1 - CAN-FD
                 *
                 *  bit1: 0 - BRS_ON
                 *        1 - BRS_OFF
                 *
                 *  bit2: 0 - FDCAN_STANDARD_ID (11-bit identifier)
                 *        1 - FDCAN_EXTENDED_ID (29-bit identifier)
                 */
                uint8_t type = 0;
                if(rxHeader.FDFormat == FDCAN_FD_CAN) {
                    type |= 0x1;
                }
                if(rxHeader.BitRateSwitch == FDCAN_BRS_OFF) {
                    type |= 0x2;
                }
                if(rxHeader.IdType == FDCAN_EXTENDED_ID) {
                    type |= 0x4;
                }

                uint8_t dlc = 0;
                switch(rxHeader.DataLength) {
                    case FDCAN_DLC_BYTES_0:
                    case FDCAN_DLC_BYTES_1:
                    case FDCAN_DLC_BYTES_2:
                    case FDCAN_DLC_BYTES_3:
                    case FDCAN_DLC_BYTES_4:
                    case FDCAN_DLC_BYTES_5:
                    case FDCAN_DLC_BYTES_6:
                    case FDCAN_DLC_BYTES_7:
                    case FDCAN_DLC_BYTES_8:
                        dlc = (uint8_t)rxHeader.DataLength;
                        break;
                    case FDCAN_DLC_BYTES_12:
                        dlc = 12;
                        break;
                    case FDCAN_DLC_BYTES_16:
                        dlc = 16;
                        break;
                    case FDCAN_DLC_BYTES_20:
                        dlc = 20;
                        break;
                    case FDCAN_DLC_BYTES_24:
                        dlc = 24;
                        break;
                    case FDCAN_DLC_BYTES_32:
                        dlc = 32;
                        break;
                    case FDCAN_DLC_BYTES_48:
                        dlc = 48;
                        break;
                    case FDCAN_DLC_BYTES_64:
                        dlc = 64;
                        break;
                    default: {
                        dlc = 0;
                        break;
                    }
                }
                // Send upstream via CMD_SEND_UPSTREAM (0x11)
                const uint32_t FRAME_CMD_OFFSET = PAYLOAD_OFFSET;
                const uint32_t FRAME_TYPE_OFFSET = PAYLOAD_OFFSET + 1;
                const uint32_t FRAME_MSGID_OFFSET = PAYLOAD_OFFSET + 2;
                const uint32_t FRAME_DLC_OFFSET = PAYLOAD_OFFSET + 6;
                const uint32_t FRAME_DATA_OFFSET = PAYLOAD_OFFSET + 7;

                uint8_t sendBuffer[128];
                uint32_t length = 0;
                sendBuffer[FRAME_CMD_OFFSET] = CMD_SEND_UPSTREAM;
                length += 1;

                sendBuffer[FRAME_TYPE_OFFSET] = type;
                length += 1;

                sendBuffer[FRAME_MSGID_OFFSET] = (uint8_t)(rxHeader.Identifier & 0xFF);
                sendBuffer[FRAME_MSGID_OFFSET + 1] = (uint8_t)((rxHeader.Identifier >> 8) & 0xFF);
                sendBuffer[FRAME_MSGID_OFFSET + 2] = (uint8_t)((rxHeader.Identifier >> 16) & 0xFF);
                sendBuffer[FRAME_MSGID_OFFSET + 3] = (uint8_t)((rxHeader.Identifier >> 24) & 0xFF);
                length += 4;

                sendBuffer[FRAME_DLC_OFFSET] = dlc;
                length += 1;

                if(dlc > 0) {
                    memcpy(&sendBuffer[FRAME_DATA_OFFSET], rxData, dlc);
                    length += dlc;
                }

                length += FRAME_OVERHEAD;

                PARSER_SendFrame(sendBuffer, length);
            }
        }
    }
}


void CANErr_Process(void)
{
    // Note: To avoid data race condition, this function is only
    // allowed to be called in Thread mode

    if ((__get_IPSR() & 0x3F) != 0) {
        // Not in thread mode
        Error_Handler();
    }

    if(hfdcan1.State != HAL_FDCAN_STATE_BUSY) {
        return;
    }

    static FDCAN_ProtocolStatusTypeDef prevProtocolStatus = {0};
    static FDCAN_ErrorCountersTypeDef prevErrorCounters = {0};
    static bool isInitialized = false;
    FDCAN_ProtocolStatusTypeDef protocolStatus;
    FDCAN_ErrorCountersTypeDef errorCounters;
    HAL_StatusTypeDef sts;

    sts = HAL_FDCAN_GetProtocolStatus(&hfdcan1, &protocolStatus);
    if(sts != HAL_OK) {
        return;
    }

    sts = HAL_FDCAN_GetErrorCounters(&hfdcan1, &errorCounters);
    if(sts != HAL_OK) {
        return;
    }

    // Check if any status field has changed
    bool statusChanged = false;
    bool maxValChanged = false;

    if(isInitialized) {
        // Check LastErrorCode - only report if changed and not NONE/NO_CHANGE
        if((prevProtocolStatus.LastErrorCode != protocolStatus.LastErrorCode) &&
           (protocolStatus.LastErrorCode != FDCAN_PROTOCOL_ERROR_NONE) &&
           (protocolStatus.LastErrorCode != FDCAN_PROTOCOL_ERROR_NO_CHANGE)) {
            statusChanged = true;
        }
        // Check DataLastErrorCode - only report if changed and not NONE/NO_CHANGE
        if((prevProtocolStatus.DataLastErrorCode != protocolStatus.DataLastErrorCode) &&
           (protocolStatus.DataLastErrorCode != FDCAN_PROTOCOL_ERROR_NONE) &&
           (protocolStatus.DataLastErrorCode != FDCAN_PROTOCOL_ERROR_NO_CHANGE)) {
            statusChanged = true;
        }
        // Check other status fields
        if((prevProtocolStatus.ErrorPassive != protocolStatus.ErrorPassive) ||
           (prevProtocolStatus.Warning != protocolStatus.Warning) ||
           (prevProtocolStatus.BusOff != protocolStatus.BusOff) ||
           (prevProtocolStatus.RxESIflag != protocolStatus.RxESIflag) ||
           (prevProtocolStatus.ProtocolException != protocolStatus.ProtocolException)) {
            statusChanged = true;
        }
    } else {
        isInitialized = true;
        statusChanged = true;  // Send initial state
    }

    canStat.RxErrorCnt = errorCounters.RxErrorCnt;
    canStat.TxErrorCnt = errorCounters.TxErrorCnt;
    if(canStat.RxErrorCnt > canStat.RxErrorCntMax) {
        canStat.RxErrorCntMax = canStat.RxErrorCnt;
        maxValChanged = true;
    }
    if(canStat.TxErrorCnt > canStat.TxErrorCntMax) {
        canStat.TxErrorCntMax = canStat.TxErrorCnt;
        maxValChanged = true;
    }
    if((errorCounters.RxErrorPassive == 1) && (prevErrorCounters.RxErrorPassive == 0)) {
        canStat.PassiveErrorCnt++;
    }
    prevErrorCounters = errorCounters;

    if(statusChanged) {
        /*
         * Protocol Status Format:
         * Payload[0]: CMD_PROTOCOL_STATUS (0x12)
         * Payload[1]: LastErrorCode
         * Payload[2]: DataLastErrorCode
         * Payload[3]: Activity
         * Payload[4]: Flags byte:
         *   bit0: ErrorPassive
         *   bit1: Warning
         *   bit2: BusOff
         *   bit3: RxESIflag
         *   bit4: RxBRSflag
         *   bit5: RxFDFflag
         *   bit6: ProtocolException
         *   bit7: Reserved
         * Payload[5]: TDCvalue
         */
        uint8_t sendBuffer[32];
        uint32_t length = 0;
        
        sendBuffer[PAYLOAD_OFFSET + length++] = CMD_PROTOCOL_STATUS;
        sendBuffer[PAYLOAD_OFFSET + length++] = protocolStatus.LastErrorCode;
        sendBuffer[PAYLOAD_OFFSET + length++] = protocolStatus.DataLastErrorCode;
        sendBuffer[PAYLOAD_OFFSET + length++] = protocolStatus.Activity;
        
        // Pack flags into a single byte
        uint8_t flags = 0;
        if(protocolStatus.ErrorPassive) flags |= 0x01;
        if(protocolStatus.Warning) flags |= 0x02;
        if(protocolStatus.BusOff) flags |= 0x04;
        if(protocolStatus.RxESIflag) flags |= 0x08;
        if(protocolStatus.RxBRSflag) flags |= 0x10;
        if(protocolStatus.RxFDFflag) flags |= 0x20;
        if(protocolStatus.ProtocolException) flags |= 0x40;
        sendBuffer[PAYLOAD_OFFSET + length++] = flags;
        
        sendBuffer[PAYLOAD_OFFSET + length++] = protocolStatus.TDCvalue;
        
        length += FRAME_OVERHEAD;
        
        PARSER_SendFrame(sendBuffer, length);
        
        // Update previous status
        prevProtocolStatus = protocolStatus;
    }

    if(statusChanged || maxValChanged) {
        CAN_stat_send();
    }
}


void CAN_stat_send(void)
{
    uint8_t buffer[128];
    uint32_t len = 0;
    extern uint16_t stat_downstream_packet_loss_cnt;
    extern uint16_t stat_upstream_packet_loss_cnt;
    extern uint16_t stat_rx_buffer_overflow_cnt;

    /*
     * CAN Stats Response Format:
     * Payload[0]: CMD_GET_CAN_STATS (0x13)
     * Payload[1-2]: TxErrorCnt (uint16_t, little-endian)
     * Payload[3-4]: TxErrorCntMax (uint16_t, little-endian)
     * Payload[5-6]: RxErrorCnt (uint16_t, little-endian)
     * Payload[7-8]: RxErrorCntMax (uint16_t, little-endian)
     * Payload[9-10]: PassiveErrorCnt (uint16_t, little-endian)
     * Payload[11-12]: stat_downstream_packet_loss_cnt (uint16_t, little-endian)
     * Payload[13-14]: stat_upstream_packet_loss_cnt (uint16_t, little-endian)
     * Payload[15-16]: stat_rx_buffer_overflow_cnt (uint16_t, little-endian)
     * Payload[17]: Status (0 = success)
     */

    len = 0;
    buffer[PAYLOAD_OFFSET + len++] = CMD_GET_CAN_STATS;

    // TxErrorCnt
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(canStat.TxErrorCnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((canStat.TxErrorCnt >> 8) & 0xFF);

    // TxErrorCntMax
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(canStat.TxErrorCntMax & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((canStat.TxErrorCntMax >> 8) & 0xFF);

    // RxErrorCnt
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(canStat.RxErrorCnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((canStat.RxErrorCnt >> 8) & 0xFF);

    // RxErrorCntMax
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(canStat.RxErrorCntMax & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((canStat.RxErrorCntMax >> 8) & 0xFF);

    // PassiveErrorCnt
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(canStat.PassiveErrorCnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((canStat.PassiveErrorCnt >> 8) & 0xFF);

    // Downstream packet loss count
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(stat_downstream_packet_loss_cnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((stat_downstream_packet_loss_cnt >> 8) & 0xFF);

    // Upstream packet loss count
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(stat_upstream_packet_loss_cnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((stat_upstream_packet_loss_cnt >> 8) & 0xFF);

    // RX Buffer overflow count
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)(stat_rx_buffer_overflow_cnt & 0xFF);
    buffer[PAYLOAD_OFFSET + len++] = (uint8_t)((stat_rx_buffer_overflow_cnt >> 8) & 0xFF);

    // Success status
    buffer[PAYLOAD_OFFSET + len++] = 0;
    len += FRAME_OVERHEAD;
    PARSER_SendFrame(buffer, len);
}


CanStat_t CAN_get_stats(void)
{
    return canStat;
}


void CAN_reset_stats(void)
{
    memset(&canStat, 0, sizeof(canStat));
}
