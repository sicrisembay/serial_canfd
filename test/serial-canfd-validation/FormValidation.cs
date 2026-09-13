using System.Globalization;
using System.Text.RegularExpressions;
using libSerialCanFD;

namespace serial_canfd_validation
{
    public partial class FormValidation : Form
    {
        #region Members
        private readonly SerialCanFd _serialCanFd = new SerialCanFd();
        private ProtocolParser _protocolParser = new ProtocolParser();
        #endregion // Members

        #region Methods
        public FormValidation()
        {
            InitializeComponent();

            _serialCanFd.FrameReceived += OnFrameParserReceived;
            _protocolParser.SendUpstreamReceived += OnSendUpstreamReceived;
            _protocolParser.ProtocolStatusReceived += OnProtocolStatusReceived;
            _protocolParser.GetCanStatsReceived += OnGetCanStatsReceived;

            ListPortNames();

            var bitrateValues = Enum.GetValues(typeof(BitRate)).Cast<BitRate>().ToArray();
            comboBox_BitRate.DataSource = bitrateValues;
            comboBox_BitRate.SelectedItem = BitRate.BITRATE_1MBPS;

            var dataBitrateValues = Enum.GetValues(typeof(DataBitRate)).Cast<DataBitRate>().ToArray();
            comboBox_DataBitRate.DataSource = dataBitrateValues;
            comboBox_DataBitRate.SelectedItem = DataBitRate.DATA_2MBPS;

            textBox_FrameId.Text = "123";
            textBox_FrameData.Text = "11 22 33 44";
            UpdateSendFrameControls();
        }

        #region Helpers
        private void EnableControls(bool isConnected)
        {
            comboBox_SerialPortNames.Enabled = !isConnected;
            comboBox_BitRate.Enabled = !isConnected;
            comboBox_DataBitRate.Enabled = !isConnected;
            button_ListPortNames.Enabled = !isConnected;

            button_busStart.Enabled = isConnected;
            checkBox_CanFd.Enabled = isConnected;
            checkBox_BitRateSwitch.Enabled = isConnected && checkBox_CanFd.Checked;
            checkBox_extendedIdentifier.Enabled = isConnected;

            if (!isConnected)
            {
                button_busStart.Text = "Start";
            }

            timer_UpdateCanStats.Interval = 1000; // 1 second
            timer_UpdateCanStats.Enabled = isConnected;

            UpdateSendFrameControls();
        }

        private void UpdateSendFrameControls()
        {
            bool isReadyToSend = button_Connect.Text == "Disconnect" && button_busStart.Text == "Stop";
            groupBox_SendFrame.Enabled = isReadyToSend;
            button_SendFrame.Enabled = isReadyToSend;
        }

        private void ListPortNames()
        {
            string[] portNames = _serialCanFd.GetPorts();
            comboBox_SerialPortNames.Items.Clear();
            if (portNames.Length != 0)
            {
                comboBox_SerialPortNames.Items.AddRange(portNames);
                comboBox_SerialPortNames.SelectedIndex = 0;
            }
        }

        private static byte[] ParseDataBytes(string rawData)
        {
            if (string.IsNullOrWhiteSpace(rawData))
            {
                return [];
            }

            string[] tokens = Regex.Split(rawData.Trim(), @"[\s,;:-]+")
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToArray();

            List<byte> parsedBytes = [];
            foreach (string token in tokens)
            {
                string normalizedToken = token.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? token[2..]
                    : token;

                if (!byte.TryParse(normalizedToken, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte value))
                {
                    throw new FormatException($"Invalid data byte '{token}'. Use hex byte values from 00 to FF.");
                }

                parsedBytes.Add(value);
            }

            return [.. parsedBytes];
        }

        private void AppendReceivedFrame(string frameDescription)
        {
            if (textBox_ReceivedFrames.TextLength > 0)
            {
                textBox_ReceivedFrames.AppendText(Environment.NewLine);
            }

            textBox_ReceivedFrames.AppendText(frameDescription);
        }

        private static string FormatFrameIdentifier(uint identifier, FrameFormat frameFormat)
        {
            return frameFormat == FrameFormat.STANDARD
                ? $"0x{identifier:X3}"
                : $"0x{identifier:X8}";
        }

        private static string FormatFrameData(byte[] data)
        {
            return data.Length == 0
                ? "-"
                : string.Join(" ", data.Select(value => value.ToString("X2", CultureInfo.InvariantCulture)));
        }
        #endregion

        #region Event Handlers: Buttons
        private void button_ListPortNames_Click(object sender, EventArgs e)
        {
            ListPortNames();
        }

        private void button_Connect_Click(object sender, EventArgs e)
        {
            if (button_Connect.Text == "Connect")
            {
                string selectedPortName = comboBox_SerialPortNames.SelectedItem?.ToString() ?? string.Empty;
                _serialCanFd.Open(selectedPortName);
                EnableControls(true);
                button_Connect.Text = "Disconnect";
            } else
            {
                _serialCanFd.Close();
                EnableControls(false);
                button_Connect.Text = "Connect";
            }

            UpdateSendFrameControls();
        }

        private void button_busStart_Click(object sender, EventArgs e)
        {
            if (button_busStart.Text == "Start")
            {
                BitRate selectedBitRate = (BitRate)comboBox_BitRate.SelectedItem;
                DataBitRate selectedDataBitRate = (DataBitRate)comboBox_DataBitRate.SelectedItem;
                _serialCanFd.CanStart(selectedBitRate, selectedDataBitRate);
                button_busStart.Text = "Stop";
            } else
            {
                _serialCanFd.CanStop();
                button_busStart.Text = "Start";
            }

            UpdateSendFrameControls();
        }

        private void button_SendFrame_Click(object sender, EventArgs e)
        {
            try
            {
                string idText = textBox_FrameId.Text.Trim();
                if (idText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    idText = idText[2..];
                }

                if (!uint.TryParse(idText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint frameId))
                {
                    throw new FormatException("Frame ID must be a valid hexadecimal number.");
                }

                bool isStdId = !checkBox_extendedIdentifier.Checked;
                uint maxFrameId = isStdId ? 0x7FFu : 0x1FFFFFFFu;
                if (frameId > maxFrameId)
                {
                    throw new ArgumentOutOfRangeException(nameof(frameId), $"Frame ID exceeds {(isStdId ? "standard" : "extended")} identifier range.");
                }

                byte[] data = ParseDataBytes(textBox_FrameData.Text);
                int maxDataLength = checkBox_CanFd.Checked ? 64 : 8;
                if (data.Length > maxDataLength)
                {
                    throw new ArgumentOutOfRangeException(nameof(data), $"Data length must be {maxDataLength} bytes or less for the selected mode.");
                }

                _serialCanFd.SendCanFrame(
                    checkBox_CanFd.Checked,
                    checkBox_BitRateSwitch.Checked,
                    isStdId,
                    frameId,
                    data);
            } catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Send Frame", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion // Event Handlers: Buttons
        #endregion // Methods

        private void checkBox_CanFd_CheckStateChanged(object sender, EventArgs e)
        {
            checkBox_BitRateSwitch.Enabled = checkBox_CanFd.Checked && checkBox_CanFd.Enabled;
            if (!checkBox_CanFd.Checked)
            {
                checkBox_BitRateSwitch.Checked = false;
            }
        }

        private void OnFrameParserReceived(object? sender, byte[] frame)
        {
            _protocolParser.ParseProtocol(frame);
        }

        private void OnSendUpstreamReceived(object? sender, SendUpstreamArgs e)
        {
            string frameDescription = $"{DateTime.Now:HH:mm:ss.fff} | {e.CanType} | {e.BrsMode} | {e.FrameFormat} | ID: {FormatFrameIdentifier(e.Identifier, e.FrameFormat)} | DLC: {e.Dlc} | Data: {FormatFrameData(e.Data)}";

            if (textBox_ReceivedFrames.InvokeRequired)
            {
                textBox_ReceivedFrames.BeginInvoke(() => AppendReceivedFrame(frameDescription));
                return;
            }

            AppendReceivedFrame(frameDescription);
        }

        private void OnProtocolStatusReceived(object? sender, ProtocolStatusArgs e)
        {
            if (label_LastErrorCode.InvokeRequired)
            {
                label_LastErrorCode.BeginInvoke(() => label_LastErrorCode.Text = "Last Error Code: 0x" + e.LastErrorCode.ToString("X2"));
            } else
            {
                label_LastErrorCode.Text = "Last Error Code: 0x" + e.LastErrorCode.ToString("X2");
            }

            if (label_DataLastErrorCode.InvokeRequired)
            {
                label_DataLastErrorCode.BeginInvoke(() => label_DataLastErrorCode.Text = "Data Last Error Code: 0x" + e.DataLastErrorCode.ToString("X2"));
            } else
            {
                label_DataLastErrorCode.Text = "Data Last Error Code: 0x" + e.DataLastErrorCode.ToString("X2");
            }

            if (label_Activity.InvokeRequired)
            {
                label_Activity.BeginInvoke(() => label_Activity.Text = "Activity: 0x" + e.Activity.ToString("X2"));
            } else
            {
                label_Activity.Text = "Activity: 0x" + e.Activity.ToString("X2");
            }

            if (label_Flag.InvokeRequired)
            {
                label_Flag.BeginInvoke(() => label_Flag.Text = "Flag: 0x" + e.Flags.ToString("X2"));
            } else
            {
                label_Flag.Text = "Flag: 0x" + e.Flags.ToString("X2");
            }

            if (label_tdc.InvokeRequired)
            {
                label_tdc.BeginInvoke(() => label_tdc.Text = "TDC: 0x" + e.TdcValue.ToString("X2"));
            } else
            {
                label_tdc.Text = "TDC: 0x" + e.TdcValue.ToString("X2");
            }
        }

        private void OnGetCanStatsReceived(object? sender, GetCanStatsArgs e)
        {
            if (label_TxEc.InvokeRequired)
            {
                label_TxEc.BeginInvoke(() => label_TxEc.Text = "TEC: " + e.TxErrorCount);
            } else
            {
                label_TxEc.Text = "TEC: " + e.TxErrorCount;
            }

            if (label_RxEC.InvokeRequired)
            {
                label_RxEC.BeginInvoke(() => label_RxEC.Text = "REC: " + e.RxErrorCount);
            } else
            {
                label_RxEC.Text = "REC: " + e.RxErrorCount;
            }

            if (label_MaxTxEc.InvokeRequired)
            {
                label_MaxTxEc.BeginInvoke(() => label_MaxTxEc.Text = "Max TEC: " + e.TxErrorCountMax);

            } else
            {
                label_MaxTxEc.Text = "Max TEC: " + e.TxErrorCountMax;
            }

            if (label_MaxRxEc.InvokeRequired)
            {
                label_MaxRxEc.BeginInvoke(() => label_MaxRxEc.Text = "Max REC: " + e.RxErrorCountMax);
            } else
            {
                label_MaxRxEc.Text = "Max REC: " + e.RxErrorCountMax;
            }

            if (label_PassiveEc.InvokeRequired)
            {
                label_PassiveEc.BeginInvoke(() => label_PassiveEc.Text = "Passive EC: " + e.PassiveErrorCount);
            } else
            {
                label_PassiveEc.Text = "Passive EC: " + e.PassiveErrorCount;
            }

            if (label_DownstreamLossCount.InvokeRequired)
            {
                label_DownstreamLossCount.BeginInvoke(() => label_DownstreamLossCount.Text = "Down Loss: " + e.DownstreamPacketLossCount);
            } else
            {
                label_DownstreamLossCount.Text = "Down Loss: " + e.DownstreamPacketLossCount;
            }

            if (label_UpstreamLossCount.InvokeRequired)
            {
                label_UpstreamLossCount.BeginInvoke(() => label_UpstreamLossCount.Text = "Up Loss: " + e.UpstreamPacketLossCount);
            } else
            {
                label_UpstreamLossCount.Text = "Up Loss: " + e.UpstreamPacketLossCount;
            }

            if (label_RxBuffOverflow.InvokeRequired)
            {
                label_RxBuffOverflow.BeginInvoke(() => label_RxBuffOverflow.Text = "Rx OV: " + e.RxOverrunCount);
            } else
            {
                label_RxBuffOverflow.Text = "Rx OV: " + e.RxOverrunCount;
            }
        }

        private void timer_UpdateCanStats_Tick(object sender, EventArgs e)
        {
            _serialCanFd.GetCanStats();
        }

        private void button_ResetCanStats_Click(object sender, EventArgs e)
        {
            _serialCanFd.ResetCanStats();
        }
    }
}
