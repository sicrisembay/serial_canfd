#include "frameParser.h"
#include "main.h"
#include "UTIL_ringbuf.h"
#include "canParser.h"

#define FRAME_RX_SIZE       (1024)
#define FRAME_TX_SIZE       (512)

typedef struct {
    uint32_t prescaler;
    uint32_t syncJumpWidth;
    uint32_t timeSeg1;
    uint32_t timeSeg2;
} CAN_BITRATE_CONFIG_T;

typedef enum {
    ARB_BITRATE_1000K = 0,
    ARB_BITRATE_800K,
    ARB_BITRATE_500K,
    ARB_BITRATE_250K,
    ARB_BITRATE_125K,
    ARB_BITRATE_100K,
    ARB_BITRATE_50K,
    ARB_BITRATE_20K,
    ARB_BITRATE_10K,
    N_ARB_BITRATE
} ARB_BITRATE_E;

static CAN_BITRATE_CONFIG_T const arbBitrateConfig[N_ARB_BITRATE] = {
    // Note1: 80MHz clock; Time quantum = 1/80MHz = 12.5ns; {prescaler, sjw, timeSeg1, timeSeg2}
    // Note2: sample point = (1+timeSeg1)/(1+timeSeg1+timeSeg2)
    // Note3: All entries target 87.5% SP: (1+69)/(1+69+10) = 70/80 = 87.5%
    //        except 800K which uses 100 TQ/bit (not divisible by 8): 87/100 = 87.0%
    {1,   10, 69, 10},  // 1000K: prescaler=1,  80 TQ/bit, SP=87.5%
    {1,   13, 86, 13},  // 800K:  prescaler=1, 100 TQ/bit, SP=87.0% (closest achievable)
    {2,   10, 69, 10},  // 500K:  prescaler=2,  80 TQ/bit, SP=87.5%
    {4,   10, 69, 10},  // 250K:  prescaler=4,  80 TQ/bit, SP=87.5%
    {8,   10, 69, 10},  // 125K:  prescaler=8,  80 TQ/bit, SP=87.5%
    {10,  10, 69, 10},  // 100K:  prescaler=10, 80 TQ/bit, SP=87.5%
    {20,  10, 69, 10},  // 50K:   prescaler=20, 80 TQ/bit, SP=87.5%
    {50,  10, 69, 10},  // 20K:   prescaler=50, 80 TQ/bit, SP=87.5%
    {100, 10, 69, 10}   // 10K:   prescaler=100, 80 TQ/bit, SP=87.5%
};

typedef enum {
    DATA_BITRATE_5000K = 0,
    DATA_BITRATE_2000K,
    DATA_BITRATE_1000K,
    DATA_BITRATE_800K,
    DATA_BITRATE_500K,
    DATA_BITRATE_250K,
    DATA_BITRATE_125K,
    DATA_BITRATE_100K,
    N_DATA_BITRATE
} DATA_BITRATE_E;

static CAN_BITRATE_CONFIG_T const dataBitrateConfig[N_DATA_BITRATE] = {
    // Note1: 80MHz clock; Time quantum = 1/80MHz = 12.5ns; {prescaler, sjw, timeSeg1, timeSeg2}
    // Note2: sample point = (1+timeSeg1)/(1+timeSeg1+timeSeg2)
    // Note3: All entries target 87.5% SP where achievable (total TQ divisible by 8)
    //        except 800K which uses 25 TQ/bit (100 total, not div by 8): 22/25 = 88.0%
    // Note4: DataPrescaler max=32, DataTimeSeg1 max=32, DataTimeSeg2 max=16
    {1,  2, 13,  2},  // 5000K: prescaler=1,  16 TQ/bit, SP=87.5%
    {5,  1,  6,  1},  // 2000K: prescaler=5,   8 TQ/bit, SP=87.5%
    {5,  2, 13,  2},  // 1000K: prescaler=5,  16 TQ/bit, SP=87.5%
    {4,  3, 21,  3},  // 800K:  prescaler=4,  25 TQ/bit, SP=88.0% (closest achievable)
    {10, 2, 13,  2},  // 500K:  prescaler=10, 16 TQ/bit, SP=87.5%
    {20, 2, 13,  2},  // 250K:  prescaler=20, 16 TQ/bit, SP=87.5%
    {20, 4, 27,  4},  // 125K:  prescaler=20, 32 TQ/bit, SP=87.5%
    {25, 4, 27,  4}   // 100K:  prescaler=25, 32 TQ/bit, SP=87.5%
};

extern FDCAN_HandleTypeDef hfdcan1;

volatile uint32_t rdPtr = 0;
volatile uint32_t wrPtr = 0;
static uint8_t rxFrameBuffer[FRAME_RX_SIZE];

extern tRingBufObject usbTxRb;
static uint16_t packetSeq = 0;

uint16_t stat_downstream_packet_loss_cnt = 0;
uint16_t stat_upstream_packet_loss_cnt = 0;
uint16_t stat_rx_buffer_overflow_cnt = 0;

static void _ProcessValidFrame(const uint32_t index, uint32_t len)
{
    uint8_t responseBuffer[128];
    uint32_t respLen = 0;
    uint8_t cmd;

    /* Command */
    cmd = rxFrameBuffer[(index + PAYLOAD_OFFSET) % FRAME_RX_SIZE];

    switch(cmd) {
        case CMD_GET_DEVICE_ID: {
            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_GET_DEVICE_ID;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = 0xAC;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = VERSION_MAJOR;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = VERSION_MINOR;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = VERSION_PATCH;
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }

        case CMD_CAN_START: {
            HAL_StatusTypeDef sts = HAL_OK;
            const uint8_t arbBitrate = rxFrameBuffer[(index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE];
            const uint8_t dataBitRate = rxFrameBuffer[(index + PAYLOAD_OFFSET + 2) % FRAME_RX_SIZE];
            if((arbBitrate >= N_ARB_BITRATE) || (dataBitRate >= N_DATA_BITRATE)) {
                sts = HAL_ERROR;
            }

            if(HAL_OK == sts) {
                if(HAL_FDCAN_GetState(&hfdcan1) == HAL_FDCAN_STATE_BUSY) {
                    sts = HAL_FDCAN_Stop(&hfdcan1);
                }
            }

            if(HAL_OK == sts) {
                hfdcan1.Init.ClockDivider = FDCAN_CLOCK_DIV1;
                hfdcan1.Init.FrameFormat = FDCAN_FRAME_FD_BRS;
                hfdcan1.Init.Mode = FDCAN_MODE_NORMAL;
                hfdcan1.Init.AutoRetransmission = ENABLE;
                hfdcan1.Init.TransmitPause = DISABLE;
                hfdcan1.Init.ProtocolException = DISABLE;
                hfdcan1.Init.NominalPrescaler = arbBitrateConfig[arbBitrate].prescaler;
                hfdcan1.Init.NominalSyncJumpWidth = arbBitrateConfig[arbBitrate].syncJumpWidth;
                hfdcan1.Init.NominalTimeSeg1 = arbBitrateConfig[arbBitrate].timeSeg1;
                hfdcan1.Init.NominalTimeSeg2 = arbBitrateConfig[arbBitrate].timeSeg2;
                hfdcan1.Init.DataPrescaler = dataBitrateConfig[dataBitRate].prescaler;
                hfdcan1.Init.DataSyncJumpWidth = dataBitrateConfig[dataBitRate].syncJumpWidth;
                hfdcan1.Init.DataTimeSeg1 = dataBitrateConfig[dataBitRate].timeSeg1;
                hfdcan1.Init.DataTimeSeg2 = dataBitrateConfig[dataBitRate].timeSeg2;
                hfdcan1.Init.StdFiltersNbr = RX_FILTER_MAX_STANDARD;
                hfdcan1.Init.ExtFiltersNbr = RX_FILTER_MAX_EXTENDED;
                hfdcan1.Init.TxFifoQueueMode = FDCAN_TX_FIFO_OPERATION;
                sts = HAL_FDCAN_Init(&hfdcan1);
            }

            if(HAL_OK == sts) {
                FDCAN_FilterTypeDef filterConfig = {
                    .IdType = FDCAN_STANDARD_ID,
                    .FilterIndex = 0,
                    .FilterType = FDCAN_FILTER_MASK,
                    .FilterConfig = FDCAN_FILTER_DISABLE,
                    .FilterID1 = 0,
                    .FilterID2 = 0
                };
                /* 
                 * Setup filters: 
                 *   1. Clear all filters first
                 *   2. Re-apply the existing ones 
                 */
                if(HAL_OK == sts) {
                    // Clear all standard filters
                    filterConfig.IdType = FDCAN_STANDARD_ID;
					for(uint8_t i = 0; i < RX_FILTER_MAX_STANDARD; i++) {
                        filterConfig.FilterIndex = i;
						sts = HAL_FDCAN_ConfigFilter(&hfdcan1, &filterConfig);
						if(sts != HAL_OK) {
							break;
						}
					}
                }
                if(HAL_OK == sts) {
                    // Clear all extended filters
                	filterConfig.IdType = FDCAN_EXTENDED_ID;
                    for(uint8_t i = 0; i < RX_FILTER_MAX_EXTENDED; i++) {
                        filterConfig.FilterIndex = i;
                        sts = HAL_FDCAN_ConfigFilter(&hfdcan1, &filterConfig);
                        if(sts != HAL_OK) {
                            break;
                        }
                    }
                }

                sts = CAN_ApplyAllFilter();
            }

            if(HAL_OK == sts) {
                sts = HAL_FDCAN_Start(&hfdcan1);
            }

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_CAN_START;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }

        case CMD_CAN_STOP: {
            HAL_StatusTypeDef sts = HAL_OK;
            if(HAL_FDCAN_GetState(&hfdcan1) == HAL_FDCAN_STATE_BUSY) {
                sts = HAL_FDCAN_Stop(&hfdcan1);
            }

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_CAN_STOP;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = sts;
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }

        case CMD_DEVICE_RESET: {
            NVIC_SystemReset();
            break;
        }

        case CMD_ENTER_DFU: {
            /* Set the magic word then reset — JumpToBootloader() runs before main() */
            dfu_flag = DFU_MAGIC_WORD;
            NVIC_SystemReset();
            break;
        }

        case CMD_SEND_DOWNSTREAM: {
            /*
             * TX_TYPE
             *  bit0: 0 - CAN-CC
             *        1 - CAN-FD
             *
             *  bit1: 0 - BRS_ON (valid if bit0 is 1)
             *        1 - BRS_OFF
             *
             *  bit2: 0 - FDCAN_STANDARD_ID (11-bit identifier)
             *        1 - FDCAN_EXTENDED_ID (29-bit identifier)
             */
            const uint32_t FRAME_TX_TYPE_OFFSET = (index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE;
            const uint32_t FRAME_TX_MSGID_OFFSET = (index + PAYLOAD_OFFSET + 2) % FRAME_RX_SIZE;
            const uint32_t FRAME_TX_DLC_OFFSET = (index + PAYLOAD_OFFSET + 6) % FRAME_RX_SIZE;
            const uint32_t FRAME_TX_DATA_OFFSET = (index + PAYLOAD_OFFSET + 7) % FRAME_RX_SIZE;

            CanTx_t canTx = {0};
            bool hasError = false;
            const uint8_t type = rxFrameBuffer[FRAME_TX_TYPE_OFFSET];
            const uint8_t dlc = rxFrameBuffer[FRAME_TX_DLC_OFFSET];
            uint32_t identifier = rxFrameBuffer[FRAME_TX_MSGID_OFFSET];
            identifier |= ((uint32_t)rxFrameBuffer[(FRAME_TX_MSGID_OFFSET + 1) % FRAME_RX_SIZE] << 8);
            identifier |= ((uint32_t)rxFrameBuffer[(FRAME_TX_MSGID_OFFSET + 2) % FRAME_RX_SIZE] << 16);
            identifier |= ((uint32_t)rxFrameBuffer[(FRAME_TX_MSGID_OFFSET + 3) % FRAME_RX_SIZE] << 24);

            canTx.header.Identifier = identifier;
            if((type & 0x4) == 0) {
                canTx.header.IdType = FDCAN_STANDARD_ID;  // 11-bit identifier
            } else {
                canTx.header.IdType = FDCAN_EXTENDED_ID;  // 29-bit identifier
            }
            canTx.header.TxFrameType = FDCAN_DATA_FRAME;
            canTx.header.ErrorStateIndicator = FDCAN_ESI_ACTIVE;
            if((type & 0x1) == 0) {
                // CAN Classic
                canTx.header.FDFormat = FDCAN_CLASSIC_CAN;
                if((type & 0x2) == 0) {
                    hasError = true;  // CAN-CC doesn't support BRS
                } else {
                    canTx.header.BitRateSwitch = FDCAN_BRS_OFF;
                }

                if(dlc > 8) {
                    hasError = true;  // CAN-CC max DLC is 8
                } else {
                    canTx.header.DataLength = dlc;
                }
            } else {
                // FD
                canTx.header.FDFormat = FDCAN_FD_CAN;
                if((type & 0x2) == 0) {
                    canTx.header.BitRateSwitch = FDCAN_BRS_ON;
                } else {
                    canTx.header.BitRateSwitch = FDCAN_BRS_OFF;
                }

                if(dlc > 64) {
                    hasError = true; // CAN-FD max DLC is 64
                } else {
                    if(dlc <= 8) {
                        canTx.header.DataLength = dlc;
                    } else if(dlc <= 12) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_12;
                    } else if(dlc <= 16) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_16;
                    } else if(dlc <= 20) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_20;
                    } else if(dlc <= 24) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_24;
                    } else if(dlc <= 32) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_32;
                    } else if(dlc <= 48) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_48;
                    } else if(dlc <= 64) {
                        canTx.header.DataLength = FDCAN_DLC_BYTES_64;
                    }
                }
            }
            canTx.header.TxEventFifoControl = FDCAN_NO_TX_EVENTS;
            canTx.header.MessageMarker = 0;
            if(hasError != true) {
                if(dlc > 0) {
                    for(uint32_t i = 0; i < dlc; i++) {
                        canTx.data[i] = rxFrameBuffer[(FRAME_TX_DATA_OFFSET + i) % FRAME_RX_SIZE];
                    }
                }
                if(CAN_Send(&canTx) != true) {
                    if(stat_downstream_packet_loss_cnt < UINT16_MAX) {
                        stat_downstream_packet_loss_cnt++;
                    }
                    hasError = true;
                }
            }


            // Reply
            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_SEND_DOWNSTREAM;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = hasError ? 1 : 0;
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        case CMD_GET_CAN_STATS: {
            CAN_stat_send();
            break;
        }
        case CMD_RESET_CAN_STATS: {
            CAN_reset_stats();
            stat_downstream_packet_loss_cnt = 0;
            stat_upstream_packet_loss_cnt = 0;
            stat_rx_buffer_overflow_cnt = 0;

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_RESET_CAN_STATS;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = 0;  // Success status
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        case CMD_SET_RX_FILTER: {
            HAL_StatusTypeDef sts = HAL_OK;
            uint8_t filterIndex = rxFrameBuffer[(index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE];
            uint8_t enabled = rxFrameBuffer[(index + PAYLOAD_OFFSET + 2) % FRAME_RX_SIZE];
            uint8_t idType = rxFrameBuffer[(index + PAYLOAD_OFFSET + 3) % FRAME_RX_SIZE];
            uint8_t mode = rxFrameBuffer[(index + PAYLOAD_OFFSET + 4) % FRAME_RX_SIZE];
            uint32_t id = 0U;
            uint32_t mask = 0U;

            if(hfdcan1.State != HAL_FDCAN_STATE_READY) {
                sts = HAL_ERROR;

                respLen = 0;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_SET_RX_FILTER;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
                respLen += FRAME_OVERHEAD;
                PARSER_SendFrame(responseBuffer, respLen);
                break;
            }

            id |= (uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 6) % FRAME_RX_SIZE];
            id |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 7) % FRAME_RX_SIZE] << 8);
            id |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 8) % FRAME_RX_SIZE] << 16);
            id |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 9) % FRAME_RX_SIZE] << 24);

            mask |= (uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 10) % FRAME_RX_SIZE];
            mask |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 11) % FRAME_RX_SIZE] << 8);
            mask |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 12) % FRAME_RX_SIZE] << 16);
            mask |= ((uint32_t)rxFrameBuffer[(index + PAYLOAD_OFFSET + 13) % FRAME_RX_SIZE] << 24);

            if((idType > RX_FILTER_ID_EXTENDED) ||
               ((idType == RX_FILTER_ID_STANDARD) && (filterIndex >= RX_FILTER_MAX_STANDARD)) ||
               ((idType == RX_FILTER_ID_EXTENDED) && (filterIndex >= RX_FILTER_MAX_EXTENDED))) {
                sts = HAL_ERROR;

                respLen = 0;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_SET_RX_FILTER;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
                respLen += FRAME_OVERHEAD;
                PARSER_SendFrame(responseBuffer, respLen);
                break;
            }

            if((enabled == 0U) || (mode == RX_FILTER_MODE_DISABLE)) {
                sts = CAN_ClearRxFilter(filterIndex, idType);
                respLen = 0;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_SET_RX_FILTER;
                responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
                respLen += FRAME_OVERHEAD;
                PARSER_SendFrame(responseBuffer, respLen);
                break;
            }

            if(idType == RX_FILTER_ID_STANDARD) {
                if(mode == RX_FILTER_MODE_ID) {
                    mask = 0x7FFU;
                }
            } else {
                if(mode == RX_FILTER_MODE_ID) {
                    mask = 0x1FFFFFFFU;
                }
            }

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_SET_RX_FILTER;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)CAN_SetRxFilter(filterIndex, idType, mode, id, mask);
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        case CMD_CLEAR_RX_FILTER: {
            uint8_t filterIndex = rxFrameBuffer[(index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE];
            uint8_t idType = rxFrameBuffer[(index + PAYLOAD_OFFSET + 2) % FRAME_RX_SIZE];
            HAL_StatusTypeDef sts = HAL_ERROR;

            if(filterIndex == 0xFFU) {
                sts = CAN_ClearAllRxFilters();
            } else {
                sts = CAN_ClearRxFilter(filterIndex, idType);
            }

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_CLEAR_RX_FILTER;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        case CMD_GET_RX_FILTER_COUNT: {
            uint8_t idType = rxFrameBuffer[(index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE];
            uint8_t filterCount = CAN_GetRxFilterCount(idType);

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_GET_RX_FILTER_COUNT;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = idType;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = filterCount;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = 0;  // reserved/valid
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        case CMD_GET_RX_FILTER_INFO: {
            uint8_t filterIndex = rxFrameBuffer[(index + PAYLOAD_OFFSET + 1) % FRAME_RX_SIZE];
            uint8_t idType = rxFrameBuffer[(index + PAYLOAD_OFFSET + 2) % FRAME_RX_SIZE];
            RxFilterConfig_t filterInfo = {0};
            HAL_StatusTypeDef sts = CAN_GetRxFilterInfo(filterIndex, idType, &filterInfo);

            respLen = 0;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = CMD_GET_RX_FILTER_INFO;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)sts;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = filterInfo.filterIndex;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = filterInfo.enabled;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = filterInfo.idType;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = filterInfo.mode;
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)(filterInfo.id & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.id >> 8) & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.id >> 16) & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.id >> 24) & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)(filterInfo.mask & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.mask >> 8) & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.mask >> 16) & 0xFF);
            responseBuffer[PAYLOAD_OFFSET + respLen++] = (uint8_t)((filterInfo.mask >> 24) & 0xFF);
            respLen += FRAME_OVERHEAD;
            PARSER_SendFrame(responseBuffer, respLen);
            break;
        }
        default:
            break;
    }
}

void PARSER_Store(uint8_t *pBuf, uint32_t len)
{
    uint32_t i = 0;
    uint32_t availableSpace = 0;
    uint32_t localRdPtr = rdPtr;  // Snapshot for consistent calculation
    uint32_t localWrPtr = wrPtr;

    // Calculate available space in the buffer
    // Leave 1 byte margin to distinguish full from empty
    if(localWrPtr >= localRdPtr) {
        availableSpace = (FRAME_RX_SIZE - 1) - (localWrPtr - localRdPtr);
    } else {
        availableSpace = localRdPtr - localWrPtr - 1;
    }

    // Check if we have enough space
    if(len > availableSpace) {
        // Buffer overflow - cannot store all data
        // Track the overflow event
        if(stat_rx_buffer_overflow_cnt < UINT16_MAX) {
            stat_rx_buffer_overflow_cnt++;
        }
        return;
    }

    // Safe to write all data
    for(i = 0; i < len; i++) {
        rxFrameBuffer[wrPtr] = pBuf[i];
        wrPtr = (wrPtr + 1) % FRAME_RX_SIZE;
    }
}

void PARSER_Process()
{
    uint32_t availableBytes = 0;
    uint32_t length = 0;
    uint32_t idx = 0;
    uint8_t sum = 0;

    while(wrPtr != rdPtr) {
        /* Check start of command TAG */
        if(TAG_SOF != rxFrameBuffer[rdPtr])
        {
            // Skip character
            rdPtr = (rdPtr + 1) % FRAME_RX_SIZE;
            continue;
        }

        /* Get available bytes in the buffer */
        if(wrPtr >= rdPtr) {
            availableBytes = wrPtr - rdPtr;
        } else {
            availableBytes = (wrPtr + FRAME_RX_SIZE) - rdPtr;
        }
        if(availableBytes < FRAME_OVERHEAD) {
            /*
             * Minimum of 10 bytes to proceed
             * 1byte(TAG) + 2bytes(Length) + 4bytes(Timestamp) + 2bytes(Packet Sequence) + 1byte(Checksum)
             */
            break;
        }
        // See if the packet size byte is valid.  A command packet must be at
        // least four bytes and can not be larger than the receive buffer size.
        length = (uint32_t)(rxFrameBuffer[(rdPtr+1)%FRAME_RX_SIZE]) +
                ((uint32_t)(rxFrameBuffer[(rdPtr+2)%FRAME_RX_SIZE]) << 8);

        if((length < FRAME_OVERHEAD) || (length > (FRAME_RX_SIZE-1)))
        {
            // The packet size is too small. Minimum packet size is FRAME_OVERHEAD bytes
            // 1byte(TAG) + 2bytes(Length) + 4bytes(Timestamp) + 2bytes(Packet Sequence) + 1byte(Checksum)

            // The packet size is too large, so either this is not the start of
            // a packet or an invalid packet was received.  Skip this start of
            // command packet tag.
            rdPtr = (rdPtr + 1) % FRAME_RX_SIZE;

            // Keep scanning for a start of command packet tag.
            continue;
        }

        // If the entire command packet is not in the receive buffer then stop
        if(availableBytes < length)
        {
            break;
        }

        // The entire command packet is in the receive buffer, so compute its
        // checksum.
        for(idx = 0, sum = 0; idx < length; idx++)
        {
            sum += rxFrameBuffer[(rdPtr + idx)%FRAME_RX_SIZE];
        }

        // Skip this packet if the checksum is not correct (that is, it is
        // probably not really the start of a packet).
        if(sum != 0)
        {
            // Skip this character
            rdPtr = (rdPtr + 1) % FRAME_RX_SIZE;

            // Keep scanning for a start of command packet tag.
            continue;
        }

        // A valid command packet was received, so process it now.
        _ProcessValidFrame(rdPtr, length);

        // Done with processing this command packet.
        rdPtr = (rdPtr + length) % FRAME_RX_SIZE;
    }
}

uint8_t PARSER_SendFrame(uint8_t *pBuf, uint32_t len)
{
    uint32_t i;
    uint8_t sum = 0;

    /*
     * Start of Frame
     */
    pBuf[TAG_OFFSET] = TAG_SOF;
    /*
     * Set Length
     */
    pBuf[LEN_OFFSET] = (uint8_t)(len & 0xFF);
    pBuf[LEN_OFFSET + 1] = (uint8_t)((len >> 8) & 0xFF);
    /*
     * Set Timestamp
     */
    extern TIM_HandleTypeDef htim2;
    uint32_t timestamp = __HAL_TIM_GET_COUNTER(&htim2);
    pBuf[TIMESTAMP_OFFSET] = (uint8_t)(timestamp & 0xFF);
    pBuf[TIMESTAMP_OFFSET + 1] = (uint8_t)((timestamp >> 8) & 0xFF);
    pBuf[TIMESTAMP_OFFSET + 2] = (uint8_t)((timestamp >> 16) & 0xFF);
    pBuf[TIMESTAMP_OFFSET + 3] = (uint8_t)((timestamp >> 24) & 0xFF);

    /*
     * Set Packet Sequence
     */
    pBuf[PACKET_SEQ_OFFSET] = (uint8_t)(packetSeq & 0xFF);
    pBuf[PACKET_SEQ_OFFSET + 1] = (uint8_t)((packetSeq >> 8) & 0xFF);
    packetSeq++;

    /*
     * Calculate Checksum
     */
    for(i = 0; i < (len-1); i++) {
        sum += pBuf[i];
    }
    pBuf[i] = (uint8_t)((~sum) + 1);

    if(len > UTIL_RingBufFree(&usbTxRb)) {
        if(stat_upstream_packet_loss_cnt < UINT16_MAX) {
            stat_upstream_packet_loss_cnt++;
        }
        // Not enough space
        return 1;
    }

    /*
     * Write to Tx Buffer
     */
    UTIL_RingBufWrite(&usbTxRb, pBuf, len);

    return 0;
}
