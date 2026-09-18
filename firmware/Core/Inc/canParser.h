/*
 * canParser.h
 *
 *  Created on: Feb 26, 2026
 *      Author: Sicris
 */

#ifndef INC_CANPARSER_H_
#define INC_CANPARSER_H_

#define CONFIG_CANFD_DATA_SIZE      (64)
/* STM32G4 HAL validates these at 28 standard filters and 8 extended filters. */
#define RX_FILTER_MAX_STANDARD      (28U)
#define RX_FILTER_MAX_EXTENDED      (8U)

#define RX_FILTER_ID_STANDARD       (0U)
#define RX_FILTER_ID_EXTENDED       (1U)
#define RX_FILTER_MODE_DISABLE      (0U)
#define RX_FILTER_MODE_ID           (1U)
#define RX_FILTER_MODE_MASK         (2U)

typedef struct {
    uint16_t TxErrorCnt;
    uint16_t TxErrorCntMax;
    uint16_t RxErrorCnt;
    uint16_t RxErrorCntMax;
    uint16_t PassiveErrorCnt;
} CanStat_t;

typedef struct {
    uint8_t enabled;
    uint8_t idType;
    uint8_t mode;
    uint32_t filterIndex;
    uint32_t id;
    uint32_t mask;
} RxFilterConfig_t;

typedef struct {
    FDCAN_TxHeaderTypeDef header;
    uint8_t data[CONFIG_CANFD_DATA_SIZE];
} CanTx_t;

bool CAN_Send(CanTx_t * pCanTx);
void CANTX_Process(void);
void CANRX_Process(void);
void CANErr_Process(void);
void CAN_stat_send(void);
CanStat_t CAN_get_stats(void);
void CAN_reset_stats(void);
HAL_StatusTypeDef CAN_ApplyAllFilter(void);
HAL_StatusTypeDef CAN_SetRxFilter(uint8_t filterIndex,
                                 uint8_t idType,
                                 uint8_t mode,
                                 uint32_t id,
                                 uint32_t mask);
HAL_StatusTypeDef CAN_ClearRxFilter(uint8_t filterIndex, uint8_t idType);
HAL_StatusTypeDef CAN_ClearAllRxFilters(void);
uint8_t CAN_GetRxFilterCount(uint8_t idType);
HAL_StatusTypeDef CAN_GetRxFilterInfo(uint8_t filterIndex, uint8_t idType, RxFilterConfig_t * filterInfo);

#endif /* INC_CANPARSER_H_ */
