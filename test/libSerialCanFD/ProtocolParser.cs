using System;
using System.Collections.Generic;
using System.Text;

namespace libSerialCanFD
{
    public enum CommandIdentifier : byte
    {
        CMD_GET_DEVICE_ID = 0x00,
        CMD_CAN_START = 0x01,
        CMD_CAN_STOP = 0x02,
        CMD_DEVICE_RESET = 0x03,
        CMD_SEND_DOWNSTREAM = 0x10,
        CMD_SEND_UPSTREAM = 0x11,
        CMD_PROTOCOL_STATUS = 0x12,
        CMD_GET_CAN_STATS = 0x13,
        CMD_RESET_CAN_STATS = 0x14,
        CMD_SET_RX_FILTER = 0x15,
        CMD_CLEAR_RX_FILTER = 0x16,
        CMD_GET_RX_FILTER = 0x17,
        CMD_ENTER_DFU = 0xF0
    }

    public class GetDeviceIdArgs: EventArgs
    {
        public byte DeviceId { get; }
        public byte FirmwareMajorVersion { get; }
        public byte FirmwareMinorVersion { get; }
        public byte FirmwarePatchVersion { get; }
        public GetDeviceIdArgs(byte deviceId, byte firmwareMajorVersion, byte firmwareMinorVersion, byte firmwarePatchVersion)
        {
            DeviceId = deviceId;
            FirmwareMajorVersion = firmwareMajorVersion;
            FirmwareMinorVersion = firmwareMinorVersion;
            FirmwarePatchVersion = firmwarePatchVersion;
        }
    }

    public class CanStartArgs: EventArgs
    {
        public bool Success { get; }
        public CanStartArgs(bool success)
        {
            Success = success;
        }
    }

    public class CanStopArgs: EventArgs
    {
        public bool Success { get; }
        public CanStopArgs(bool success)
        {
            Success = success;
        }
    }

    public class SendDownstreamArgs: EventArgs
    {
        public bool Success { get; }
        public SendDownstreamArgs(bool success)
        {
            Success = success;
        }
    }

    public enum CanType
    {
        CAN_CC = 0x00,
        CAN_FD = 0x01
    }

    public enum BrsMode
    {
        BRS_ON = 0x00,
        BRS_OFF = 0x01
    }

    public enum FrameFormat
    {
        STANDARD = 0x00, // 11-bit identifier
        EXTENDED = 0x01  // 29-bit identifier
    }

    public class SendUpstreamArgs: EventArgs
    {
        public CanType CanType { get; private set; }
        public BrsMode BrsMode { get; private set; }
        public FrameFormat FrameFormat { get; private set; }
        public UInt32 Identifier { get; private set; }
        public byte Dlc { get; private set; }
        public byte[] Data { get; private set; }
        public SendUpstreamArgs(CanType canType, BrsMode brsMode, FrameFormat frameFormat, UInt32 identifier, byte dlc, byte[] data)
        {
            CanType = canType;
            BrsMode = brsMode;
            FrameFormat = frameFormat;
            Identifier = identifier;
            Dlc = dlc;
            Data = new byte[dlc];
            Array.Copy(data, Data, dlc);
        }
    }

    public class ProtocolStatusArgs: EventArgs
    {
        public byte LastErrorCode { get; private set; }
        public byte DataLastErrorCode { get; private set; }
        public byte Activity { get; private set; }
        public byte Flags { get; private set; }
        public byte TdcValue { get; private set; }
        public ProtocolStatusArgs(byte lastErrorCode, byte dataLastErrorCode, byte activity, byte flags, byte tdcValue)
        {
            LastErrorCode = lastErrorCode;
            DataLastErrorCode = dataLastErrorCode;
            Activity = activity;
            Flags = flags;
            TdcValue = tdcValue;
        }
    }

    public class GetCanStatsArgs: EventArgs
    {
        public UInt16 TxErrorCount { get; private set; }
        public UInt16 TxErrorCountMax { get; private set; }
        public UInt16 RxErrorCount { get; private set; }
        public UInt16 RxErrorCountMax { get; private set; }
        public UInt16 PassiveErrorCount { get; private set; }
        public UInt16 DownstreamPacketLossCount { get; private set; }
        public UInt16 UpstreamPacketLossCount { get; private set; }
        public UInt16 RxOverrunCount { get; private set; }
        public bool status { get; private set; }

        public GetCanStatsArgs(UInt16 txErrorCount, UInt16 txErrorCountMax, UInt16 rxErrorCount, UInt16 rxErrorCountMax,
                               UInt16 passiveErrorCount, UInt16 downstreamPacketLossCount, UInt16 upstreamPacketLossCount,
                               UInt16 rxOverrunCount, bool status)
        {
            TxErrorCount = txErrorCount;
            TxErrorCountMax = txErrorCountMax;
            RxErrorCount = rxErrorCount;
            RxErrorCountMax = rxErrorCountMax;
            PassiveErrorCount = passiveErrorCount;
            DownstreamPacketLossCount = downstreamPacketLossCount;
            UpstreamPacketLossCount = upstreamPacketLossCount;
            RxOverrunCount = rxOverrunCount;
            this.status = status;
        }
    }

    public class ResetCanStatsArgs: EventArgs
    {
        public bool Success { get; private set; }
        public ResetCanStatsArgs(bool success)
        {
            Success = success;
        }
    }

    public class SetRxFilterArgs: EventArgs
    {
        public bool Success { get; private set; }
        public SetRxFilterArgs(bool success)
        {
            Success = success;
        }
    }

    public class ClearRxFilterArgs: EventArgs
    {
        public bool Success { get; private set; }
        public ClearRxFilterArgs(bool success)
        {
            Success = success;
        }
    }
    public class ProtocolParser
    {
        public event EventHandler<GetDeviceIdArgs>? GetDeviceIdReceived;
        public event EventHandler<CanStartArgs>? CanStartReceived;
        public event EventHandler<CanStopArgs>? CanStopReceived;
        public event EventHandler<SendDownstreamArgs>? SendDownstreamReceived;
        public event EventHandler<SendUpstreamArgs>? SendUpstreamReceived;
        public event EventHandler<ProtocolStatusArgs>? ProtocolStatusReceived;
        public event EventHandler<GetCanStatsArgs>? GetCanStatsReceived;
        public event EventHandler<ResetCanStatsArgs>? ResetCanStatsReceived;
        public event EventHandler<SetRxFilterArgs>? SetRxFilterReceived;
        public event EventHandler<ClearRxFilterArgs>? ClearRxFilterReceived;

        public ProtocolParser()
        {

        }

        private static bool HasRequiredPayloadLength(byte[] payload, int requiredLength, CommandIdentifier command)
        {
            if (payload.Length >= requiredLength)
            {
                return true;
            }

            Console.WriteLine($"Invalid payload length for {command}: expected at least {requiredLength} bytes, got {payload.Length}.");
            return false;
        }

        public void ParseProtocol(byte[] frame)
        {
            // Paranoid check1
            if ((frame.Length < FrameParser.FRAME_OVERHEAD) ||
                (frame[0] != FrameParser.TAG_START_OF_FRAME))
            {
                Console.WriteLine("Invalid frame received: Failed paranoid check1");
                return;
            }

            // Paranoid check2
            if(BitConverter.ToInt16(frame, 1) != frame.Length)
            {
                Console.WriteLine("Invalid frame length received: Failed paranoid check2");
                return;
            }

            UInt32 timestamp_us = BitConverter.ToUInt32(frame, 3);
            UInt16 sequence_number = BitConverter.ToUInt16(frame, 7);
            int payload_length = frame.Length - FrameParser.FRAME_OVERHEAD;
            // Paranoid check3
            if(payload_length <= 0)
            {
                Console.WriteLine("Invalid frame received: Failed paranoid check3");
                return;
            }
            byte[] payload = new byte[frame.Length - FrameParser.FRAME_OVERHEAD];
            Array.Copy(frame, 9, payload, 0, payload.Length);

            // Process the payload
            CommandIdentifier command = (CommandIdentifier)payload[0];
            switch (command)
            {
                case CommandIdentifier.CMD_GET_DEVICE_ID:
                    if (!HasRequiredPayloadLength(payload, 5, command))
                    {
                        return;
                    }

                    // Handle CMD_GET_DEVICE_ID
                    System.Diagnostics.Debug.WriteLine($"CMD_GET_DEVICE_ID received: DeviceId={payload[1]}, FirmwareVersion={payload[2]}.{payload[3]}.{payload[4]}");
                    GetDeviceIdReceived?.Invoke(this, 
                                new GetDeviceIdArgs(payload[1], payload[2], payload[3], payload[4]));
                    break;
                case CommandIdentifier.CMD_CAN_START:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_CAN_START
                    System.Diagnostics.Debug.WriteLine($"CMD_CAN_START received: Success={payload[1] == 0x00}");
                    CanStartReceived?.Invoke(this, new CanStartArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_CAN_STOP:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_CAN_STOP
                    System.Diagnostics.Debug.WriteLine($"CMD_CAN_STOP received: Success={payload[1] == 0x00}");
                    CanStopReceived?.Invoke(this, new CanStopArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_DEVICE_RESET:
                    // Handle CMD_DEVICE_RESET
                    System.Diagnostics.Debug.WriteLine("CMD_DEVICE_RESET unexpectedly received.");
                    break;
                case CommandIdentifier.CMD_SEND_DOWNSTREAM:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_SEND_DOWNSTREAM
                    System.Diagnostics.Debug.WriteLine($"CMD_SEND_DOWNSTREAM received: Success={payload[1] == 0x00}");
                    SendDownstreamReceived?.Invoke(this, new SendDownstreamArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_SEND_UPSTREAM:
                    if (!HasRequiredPayloadLength(payload, 7, command))
                    {
                        return;
                    }

                    // Handle CMD_SEND_UPSTREAM
                    CanType canType = (CanType)(payload[1] & 0x01);
                    BrsMode brsMode = (BrsMode)((payload[1] >> 1) & 0x01);
                    FrameFormat frameFormat = (FrameFormat)((payload[1] >> 2) & 0x01);
                    UInt32 identifier = BitConverter.ToUInt32(payload, 2) & 0x1FFFFFFF; // Mask to 29 bits
                    byte dlc = payload[6];

                    if (!HasRequiredPayloadLength(payload, 7 + dlc, command))
                    {
                        return;
                    }

                    byte[] data = new byte[dlc];
                    Array.Copy(payload, 7, data, 0, dlc);

                    System.Diagnostics.Debug.WriteLine($"CMD_SEND_UPSTREAM received: CanType={canType}, BrsMode={brsMode}, FrameFormat={frameFormat}, Identifier=0x{identifier:X}, DLC={dlc}, Data={BitConverter.ToString(data)}");
                    SendUpstreamReceived?.Invoke(this, new SendUpstreamArgs(canType, brsMode, frameFormat, identifier, dlc, data));
                    break;
                case CommandIdentifier.CMD_PROTOCOL_STATUS:
                    if (!HasRequiredPayloadLength(payload, 6, command))
                    {
                        return;
                    }

                    // Handle CMD_PROTOCOL_STATUS
                    byte lastErrorCode = payload[1];
                    byte dataLastErrorCode = payload[2];
                    byte activity = payload[3];
                    byte flags = payload[4];
                    byte tdcValue = payload[5];

                    System.Diagnostics.Debug.WriteLine($"CMD_PROTOCOL_STATUS received: LastErrorCode={lastErrorCode}, DataLastErrorCode={dataLastErrorCode}, Activity={activity}, Flags={flags}, TdcValue={tdcValue}");
                    ProtocolStatusReceived?.Invoke(this, new ProtocolStatusArgs(lastErrorCode, dataLastErrorCode, activity, flags, tdcValue));
                    break;
                case CommandIdentifier.CMD_GET_CAN_STATS:
                    if (!HasRequiredPayloadLength(payload, 18, command))
                    {
                        return;
                    }

                    // Handle CMD_GET_CAN_STATS
                    UInt16 txErrorCount = BitConverter.ToUInt16(payload, 1);
                    UInt16 txErrorCountMax = BitConverter.ToUInt16(payload, 3);
                    UInt16 rxErrorCount = BitConverter.ToUInt16(payload, 5);
                    UInt16 rxErrorCountMax = BitConverter.ToUInt16(payload, 7);
                    UInt16 passiveErrorCount = BitConverter.ToUInt16(payload, 9);
                    UInt16 downstreamPacketLossCount = BitConverter.ToUInt16(payload, 11);
                    UInt16 upstreamPacketLossCount = BitConverter.ToUInt16(payload, 13);
                    UInt16 rxOverrunCount = BitConverter.ToUInt16(payload, 15);
                    bool status = payload[17] == 0x00;

                    System.Diagnostics.Debug.WriteLine($"CMD_GET_CAN_STATS received: TxErrorCount={txErrorCount}, TxErrorCountMax={txErrorCountMax}, RxErrorCount={rxErrorCount}, RxErrorCountMax={rxErrorCountMax}, PassiveErrorCount={passiveErrorCount}, DownstreamPacketLossCount={downstreamPacketLossCount}, UpstreamPacketLossCount={upstreamPacketLossCount}, RxOverrunCount={rxOverrunCount}, Status={status}");
                    GetCanStatsReceived?.Invoke(this, new GetCanStatsArgs(txErrorCount, txErrorCountMax, rxErrorCount, rxErrorCountMax,
                                                                  passiveErrorCount, downstreamPacketLossCount, upstreamPacketLossCount,
                                                                  rxOverrunCount, status));
                    break;
                case CommandIdentifier.CMD_RESET_CAN_STATS:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_RESET_CAN_STATS
                    System.Diagnostics.Debug.WriteLine($"CMD_RESET_CAN_STATS received: Success={payload[1] == 0x00}");
                    ResetCanStatsReceived?.Invoke(this, new ResetCanStatsArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_SET_RX_FILTER:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_SET_RX_FILTER
                    System.Diagnostics.Debug.WriteLine($"CMD_SET_RX_FILTER received: Success={payload[1] == 0x00}");
                    SetRxFilterReceived?.Invoke(this, new SetRxFilterArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_CLEAR_RX_FILTER:
                    if (!HasRequiredPayloadLength(payload, 2, command))
                    {
                        return;
                    }

                    // Handle CMD_CLEAR_RX_FILTER
                    System.Diagnostics.Debug.WriteLine($"CMD_CLEAR_RX_FILTER received: Success={payload[1] == 0x00}");
                    ClearRxFilterReceived?.Invoke(this, new ClearRxFilterArgs(payload[1] == 0x00));
                    break;
                case CommandIdentifier.CMD_GET_RX_FILTER:
                    // Handle CMD_GET_RX_FILTER
                    /// TODO
                    break;
                case CommandIdentifier.CMD_ENTER_DFU:
                    // Handle CMD_ENTER_DFU
                    Console.WriteLine("CMD_ENTER_DFU unexpectedly received.");
                    break;
                default:
                    Console.WriteLine($"Unknown command received: {command}");
                    break;
            }
        }
    }
}
