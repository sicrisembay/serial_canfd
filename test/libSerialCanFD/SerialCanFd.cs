using System.IO.Ports;
using System.Management;
using System.Text.RegularExpressions;

namespace libSerialCanFD
{
    public enum BitRate : byte
    {
        BITRATE_1MBPS = 0x00,
        BITRATE_800KBPS = 0x01,
        BITRATE_500KBPS = 0x02,
        BITRATE_250KBPS = 0x03,
        BITRATE_125KBPS = 0x04,
        BITRATE_100KBPS = 0x05,
        BITRATE_50KBPS = 0x06,
        BITRATE_20KBPS = 0x07,
        BITRATE_10KBPS = 0x08,
    }

    public enum DataBitRate : byte
    {
        DATA_5MBPS = 0x00,
        DATA_2MBPS = 0x01,
        DATA_1MBPS = 0x02,
        DATA_800KBPS = 0x03,
        DATA_500KBPS = 0x04,
        DATA_250KBPS = 0x05,
        DATA_125KBPS = 0x06,
        DATA_100KBPS = 0x07,
    }
    public class SerialCanFd : IDisposable
    {
        #region Members
        private UInt16 packet_sequence_number = 0;
        private SerialPort? _serialPort;
        private FrameParser _frameParser = new FrameParser();
        public event EventHandler<byte[]>? FrameReceived;

        #endregion // Members

        #region Methods
        public SerialCanFd()
        {
        }
        public string[] GetPorts()
        {
            HashSet<string> availablePorts = SerialPort.GetPortNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
            List<string> matchingPorts = [];

            using ManagementObjectSearcher searcher = new("SELECT Name, PNPDeviceID FROM Win32_PnPEntity WHERE Name LIKE '%(COM%'");

            foreach (ManagementObject port in searcher.Get().Cast<ManagementObject>())
            {
                string? pnpDeviceId = port["PNPDeviceID"]?.ToString();
                string? name = port["Name"]?.ToString();

                if (string.IsNullOrWhiteSpace(pnpDeviceId) || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!pnpDeviceId.Contains("VID_0483", StringComparison.OrdinalIgnoreCase) ||
                    !pnpDeviceId.Contains("PID_5740", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Match match = Regex.Match(name, @"\((COM\d+)\)");
                if (!match.Success)
                {
                    continue;
                }

                string portName = match.Groups[1].Value;
                if (availablePorts.Contains(portName))
                {
                    matchingPorts.Add(portName);
                }
            }

            return matchingPorts
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(portName => portName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public void Open(string portName, int baudRate = 115200)
        {
            if (string.IsNullOrWhiteSpace(portName))
            {
                throw new ArgumentException("Port name must be provided.", nameof(portName));
            }

            if (baudRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baudRate), "Baud rate must be greater than zero.");
            }

            Close();
            _serialPort?.Dispose();

            _serialPort = new SerialPort(portName)
            {
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None
            };

            _serialPort.DataReceived += OnSerialPortDataReceived;
            _serialPort.Open();

            _frameParser.FrameReceived += OnFrameParserReceived;
            GetDeviceId();
        }

        public void Close()
        {
            if (_serialPort is null)
            {
                return;
            }

            _frameParser.FrameReceived -= OnFrameParserReceived;
            _serialPort.DataReceived -= OnSerialPortDataReceived;
            _serialPort.Close();
        }

        public void Dispose()
        {
            Close();
            _serialPort?.Dispose();
            _serialPort = null;
        }

        private void OnSerialPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_serialPort is null || !_serialPort.IsOpen)
            {
                return;
            }

            try
            {
                var bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead <= 0)
                {
                    return;
                }

                var buffer = new byte[bytesToRead];
                var totalRead = 0;

                while (totalRead < bytesToRead)
                {
                    var read = _serialPort.Read(buffer, totalRead, bytesToRead - totalRead);
                    if (read <= 0)
                    {
                        break;
                    }

                    totalRead += read;
                }

                _frameParser.Parse(buffer, totalRead);
            } catch (InvalidOperationException)
            {
                // Port may close between event and read.
            }
        }

        private void OnFrameParserReceived(object? sender, byte[] frame)
        {
            FrameReceived?.Invoke(this, frame);
        }

        #region Command Methods
        public void GetDeviceId()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_GET_DEVICE_ID;
            SendPayload(payload);
        }

        public void CanStart(BitRate arbitrationBitRate, DataBitRate dataBitRate)
        {
            byte[] payload = new byte[3];
            payload[0] = (byte)CommandIdentifier.CMD_CAN_START;
            payload[1] = (byte)arbitrationBitRate;
            payload[2] = (byte)dataBitRate;
            SendPayload(payload);
        }

        public void CanStop()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_CAN_STOP;
            SendPayload(payload);
        }

        public void DeviceReset()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_DEVICE_RESET;
            SendPayload(payload);
        }

        public void SendCanFrame(bool isCanFd, bool isBrsOn, bool isStdId,
                        UInt32 id, byte[] data)
        {
            byte dlc = (byte)data.Length;
            byte tx_type = 0;
            if(isCanFd)
            {
                tx_type |= 0x01;
            }
            if(!isBrsOn)
            {
                tx_type |= 0x02;
            }
            if(!isStdId)
            {
                tx_type |= 0x04;
            }

            byte[] payload = new byte[7 + data.Length];
            payload[0] = (byte)CommandIdentifier.CMD_SEND_DOWNSTREAM;
            payload[1] = tx_type;
            payload[2] = (byte)(id & 0xFF);
            payload[3] = (byte)((id >> 8) & 0xFF);
            payload[4] = (byte)((id >> 16) & 0xFF);
            payload[5] = (byte)((id >> 24) & 0xFF);
            payload[6] = dlc;
            Array.Copy(data, 0, payload, 7, data.Length);
            SendPayload(payload);
        }

        public void GetCanStats()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_GET_CAN_STATS;
            SendPayload(payload);
        }

        public void ResetCanStats()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_RESET_CAN_STATS;
            SendPayload(payload);
        }

        public void SetCanRxFilter(byte filterIndex,bool enable, FrameFormat idType,
                        byte mode, UInt32 id, UInt32 mask)
        {
            if(idType == FrameFormat.STANDARD)
            {
                if(filterIndex >= ProtocolParser.STD_ID_FILTER_COUNT)
                {
                    throw new ArgumentOutOfRangeException("Filter index must be less than " + ProtocolParser.STD_ID_FILTER_COUNT + " for standard IDs.");
                }
                if(id > 0x7FF || mask > 0x7FF)
                {
                    throw new ArgumentOutOfRangeException("Standard ID and mask must be 11 bits or less.");
                }
            } else if(idType == FrameFormat.EXTENDED)
            {
                if(filterIndex >= ProtocolParser.EXT_ID_FILTER_COUNT)
                {
                    throw new ArgumentOutOfRangeException("Filter index must be less than " + ProtocolParser.EXT_ID_FILTER_COUNT + " for extended IDs.");
                }
                if(id > 0x1FFFFFFF || mask > 0x1FFFFFFF)
                {
                    throw new ArgumentOutOfRangeException("Extended ID and mask must be 29 bits or less.");
                }
            } else
            {
                throw new ArgumentException("Invalid ID type specified.", nameof(idType));
            }

            if(mode != 1 && mode != 2)
            {
                throw new ArgumentOutOfRangeException("Mode must be either 1 (ID-match) or 2 (Mask-match).");
            }

            byte[] payload = new byte[14];
            payload[0] = (byte)CommandIdentifier.CMD_SET_RX_FILTER;
            payload[1] = filterIndex;
            payload[2] = enable ? (byte)0x01 : (byte)0x00;
            payload[3] = (idType == FrameFormat.STANDARD) ? (byte)0x00 : (byte)0x01;
            payload[4] = mode;
            payload[5] = 0; // Reserved
            payload[6] = (byte)(id & 0xFF);
            payload[7] = (byte)((id >> 8) & 0xFF);
            payload[8] = (byte)((id >> 16) & 0xFF);
            payload[9] = (byte)((id >> 24) & 0xFF);
            payload[10] = (byte)(mask & 0xFF);
            payload[11] = (byte)((mask >> 8) & 0xFF);
            payload[12] = (byte)((mask >> 16) & 0xFF);
            payload[13] = (byte)((mask >> 24) & 0xFF);
            SendPayload(payload);
        }

        public void GetActiveCanRxFilters(FrameFormat frameFormat)
        {
            byte[] payload = new byte[2];
            payload[0] = (byte)CommandIdentifier.CMD_GET_ACTIVE_RX_FILTER_COUNT;
            payload[1] = (frameFormat == FrameFormat.STANDARD) ? (byte)0x00 : (byte)0x01;
            SendPayload(payload);
        }

        public void GetCanRxFilterInfo(FrameFormat frameFormat, byte filterIndex)
        {
            if(frameFormat == FrameFormat.STANDARD && filterIndex >= ProtocolParser.STD_ID_FILTER_COUNT)
            {
                throw new ArgumentOutOfRangeException("Filter index must be less than 28 for standard IDs.");
            }
            if(frameFormat == FrameFormat.EXTENDED && filterIndex >= ProtocolParser.EXT_ID_FILTER_COUNT)
            {
                throw new ArgumentOutOfRangeException("Filter index must be less than 8 for extended IDs.");
            }

            byte[] payload = new byte[3];
            payload[0] = (byte)CommandIdentifier.CMD_GET_RX_FILTER_INFO;
            payload[1] = filterIndex;
            payload[2] = (frameFormat == FrameFormat.STANDARD) ? (byte)0x00 : (byte)0x01;

            SendPayload(payload);
        }


        public void EnterDFU()
        {
            byte[] payload = new byte[1];
            payload[0] = (byte)CommandIdentifier.CMD_ENTER_DFU;
            SendPayload(payload);
        }

        private void SendPayload(byte[] payload)
        {
            if (_serialPort is null || !_serialPort.IsOpen)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }
            byte[] frame = new byte[payload.Length + FrameParser.FRAME_OVERHEAD];
            Int64 ticks = DateTime.Now.Ticks;
            UInt32 timestamp = (UInt32)(ticks / 100);
            frame[0] = FrameParser.TAG_START_OF_FRAME;
            frame[1] = (byte)(frame.Length & 0xFF);
            frame[2] = (byte)((frame.Length >> 8) & 0xFF);
            frame[3] = (byte)(timestamp & 0xFF);
            frame[4] = (byte)((timestamp >> 8) & 0xFF);
            frame[5] = (byte)((timestamp >> 16) & 0xFF);
            frame[6] = (byte)((timestamp >> 24) & 0xFF);
            frame[7] = (byte)(packet_sequence_number & 0xFF);
            frame[8] = (byte)((packet_sequence_number >> 8) & 0xFF);
            packet_sequence_number = (UInt16)((packet_sequence_number + 1) & 0xFFFF);
            Array.Copy(payload, 0, frame, 9, payload.Length);
            UInt32 sum = 0;
            for(int i = 0; i < frame.Length - 1; i++)
            {
                sum += frame[i];
            }
            frame[frame.Length - 1] = (byte)((0 - sum) & 0xFF);

            _serialPort.Write(frame, 0, frame.Length);
        }
        #endregion // Command Methods

        #endregion // Methods
    }
}
