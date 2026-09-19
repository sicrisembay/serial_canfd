using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using libSerialCanFD;

namespace serial_canfd_validation
{
    public partial class FormValidation : Form
    {
        #region Members
        private const int MAX_RECEIVED_FRAME_LINES = 2000;

        private readonly SerialCanFd _serialCanFd = new SerialCanFd();
        private ProtocolParser _protocolParser = new ProtocolParser();
        private readonly FormFilterSettings _formFilterSettings;
        private readonly ConcurrentQueue<string> _pendingReceivedFrames = new();
        private readonly Queue<string> _receivedFrameLines = new();
        private readonly System.Windows.Forms.Timer _receivedFrameFlushTimer = new() { Interval = 100 };
        #endregion // Members

        #region Methods
        public FormValidation()
        {
            InitializeComponent();

            _formFilterSettings = new FormFilterSettings(ref _serialCanFd, ref _protocolParser);

            _serialCanFd.FrameReceived += OnFrameParserReceived;
            _protocolParser.GetDeviceIdReceived += OnGetDeviceIdReceived;
            _protocolParser.CanStartReceived += OnCanStartReceived;
            _protocolParser.CanStopReceived += OnCanStopReceived;
            _protocolParser.SendUpstreamReceived += OnSendUpstreamReceived;
            _protocolParser.ProtocolStatusReceived += OnProtocolStatusReceived;
            _protocolParser.GetCanStatsReceived += OnGetCanStatsReceived;
            _protocolParser.GetActiveRxFilterCountReceived += OnGetActiveRxFilterCountReceived;

            var bitrateValues = Enum.GetValues(typeof(BitRate)).Cast<BitRate>().ToArray();
            comboBox_BitRate.DataSource = bitrateValues;
            comboBox_BitRate.SelectedItem = BitRate.BITRATE_1MBPS;

            var dataBitrateValues = Enum.GetValues(typeof(DataBitRate)).Cast<DataBitRate>().ToArray();
            comboBox_DataBitRate.DataSource = dataBitrateValues;
            comboBox_DataBitRate.SelectedItem = DataBitRate.DATA_2MBPS;

            textBox_FrameId.Text = "123";
            textBox_FrameData.Text = "11 22 33 44";

            EnableControls(false);
            UpdateSendFrameControls();

            _receivedFrameFlushTimer.Tick += FlushReceivedFrames;
            _receivedFrameFlushTimer.Start();
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            await ListPortNames();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _receivedFrameFlushTimer.Stop();
            _receivedFrameFlushTimer.Tick -= FlushReceivedFrames;
            _receivedFrameFlushTimer.Dispose();
            base.OnFormClosed(e);
        }

        #region Helpers
        private void EnableControls(bool isConnected)
        {
            comboBox_SerialPortNames.Enabled = !isConnected;
            comboBox_BitRate.Enabled = !isConnected;
            comboBox_DataBitRate.Enabled = !isConnected;
            button_ListPortNames.Enabled = !isConnected;

            button_busStart.Enabled = isConnected;
            button_ResetDevice.Enabled = isConnected;
            button_EnterDFU.Enabled = isConnected;
            button_FilterSettings.Enabled = isConnected;
            checkBox_CanFd.Enabled = isConnected;
            checkBox_BitRateSwitch.Enabled = isConnected && checkBox_CanFd.Checked;
            checkBox_extendedIdentifier.Enabled = isConnected;
            groupBox_CanStatistics.Enabled = isConnected;

            if (!isConnected)
            {
                button_busStart.Text = "Start";
            }

            timer_UpdateCanStats.Interval = 1000; // 1 second

            UpdateSendFrameControls();
        }

        private void UpdateSendFrameControls()
        {
            bool isReadyToSend = button_Connect.Text == "Disconnect" && button_busStart.Text == "Stop";
            groupBox_SendFrame.Enabled = isReadyToSend;
            button_SendFrame.Enabled = isReadyToSend;
        }

        private async Task ListPortNames()
        {
            // GetPorts() runs a WMI query which can take seconds; keep it off the UI thread.
            string[] portNames = await Task.Run(_serialCanFd.GetPorts);

            comboBox_SerialPortNames.Text = string.Empty;
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

        private static uint ParseHexUInt32(string value, string fieldName)
        {
            string normalizedValue = value.Trim();
            if (normalizedValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                normalizedValue = normalizedValue[2..];
            }

            if (!uint.TryParse(normalizedValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint parsedValue))
            {
                throw new FormatException($"{fieldName} must be a valid hexadecimal number.");
            }

            return parsedValue;
        }


        private void AppendReceivedFrame(string frameDescription)
        {
            _pendingReceivedFrames.Enqueue(frameDescription);
        }

        private void FlushReceivedFrames(object? sender, EventArgs e)
        {
            if (_pendingReceivedFrames.IsEmpty)
            {
                return;
            }

            StringBuilder appended = new();
            bool trimmed = false;
            while (_pendingReceivedFrames.TryDequeue(out string? line))
            {
                _receivedFrameLines.Enqueue(line);
                appended.AppendLine(line);
                while (_receivedFrameLines.Count > MAX_RECEIVED_FRAME_LINES)
                {
                    _receivedFrameLines.Dequeue();
                    trimmed = true;
                }
            }

            if (trimmed)
            {
                textBox_ReceivedFrames.Text = string.Join(Environment.NewLine, _receivedFrameLines);
                textBox_ReceivedFrames.SelectionStart = textBox_ReceivedFrames.TextLength;
                textBox_ReceivedFrames.ScrollToCaret();
            } else
            {
                if (textBox_ReceivedFrames.TextLength > 0)
                {
                    appended.Insert(0, Environment.NewLine);
                }

                textBox_ReceivedFrames.AppendText(appended.ToString().TrimEnd('\r', '\n'));
            }
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

        #region Event Handler: CheckBox
        private void checkBox_CanFd_CheckStateChanged(object sender, EventArgs e)
        {
            checkBox_BitRateSwitch.Enabled = checkBox_CanFd.Checked && checkBox_CanFd.Enabled;
            if (!checkBox_CanFd.Checked)
            {
                checkBox_BitRateSwitch.Checked = false;
            }
        }

        #endregion // Event Handler: CheckBox

        #region Event Handlers: Buttons
        private async void button_ListPortNames_Click(object sender, EventArgs e)
        {
            button_ListPortNames.Enabled = false;
            try
            {
                await ListPortNames();
            } finally
            {
                button_ListPortNames.Enabled = true;
            }
        }

        private async void button_Connect_Click(object sender, EventArgs e)
        {
            button_Connect.Enabled = false;
            try
            {
                if (button_Connect.Text == "Connect")
                {
                    string selectedPortName = comboBox_SerialPortNames.SelectedItem?.ToString() ?? string.Empty;
                    if (!string.IsNullOrEmpty(selectedPortName))
                    {
                        await Task.Run(() => _serialCanFd.Open(selectedPortName));
                        EnableControls(true);
                        button_Connect.Text = "Disconnect";
                    }
                } else
                {
                    timer_UpdateCanStats.Enabled = false;
                    await Task.Run(_serialCanFd.Close);
                    EnableControls(false);
                    button_Connect.Text = "Connect";
                }

                UpdateSendFrameControls();
            } finally
            {
                button_Connect.Enabled = true;
                timer_UpdateCanStats.Enabled = false;
            }
        }

        private void button_busStart_Click(object sender, EventArgs e)
        {
            if (button_busStart.Text == "Start")
            {
                BitRate selectedBitRate = (BitRate)comboBox_BitRate.SelectedItem;
                DataBitRate selectedDataBitRate = (DataBitRate)comboBox_DataBitRate.SelectedItem;
                _serialCanFd.CanStart(selectedBitRate, selectedDataBitRate);
                timer_UpdateCanStats.Enabled = true;

            } else
            {
                timer_UpdateCanStats.Enabled = false;
                _serialCanFd.CanStop();
            }
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

        private void button_ResetCanStats_Click(object sender, EventArgs e)
        {
            _serialCanFd.ResetCanStats();
        }

        private void button_ResetDevice_Click(object sender, EventArgs e)
        {
            button_Connect.Text = "Connect";
            EnableControls(false);
            timer_UpdateCanStats.Enabled = false;
            _serialCanFd.DeviceReset();

            comboBox_SerialPortNames.Text = string.Empty;
            comboBox_SerialPortNames.Items.Clear();
        }

        private void button_EnterDFU_Click(object sender, EventArgs e)
        {
            button_Connect.Text = "Connect";
            EnableControls(false);
            timer_UpdateCanStats.Enabled = false;
            _serialCanFd.EnterDFU();

            comboBox_SerialPortNames.Text = string.Empty;
            comboBox_SerialPortNames.Items.Clear();
        }

        #endregion // Event Handlers: Buttons

        #region Event Handlers: Timer
        private void timer_UpdateCanStats_Tick(object sender, EventArgs e)
        {
            _serialCanFd.GetCanStats();
        }
        #endregion // Event Handlers: Timer

        #region Event Handlers: ProtocolParser
        private void OnFrameParserReceived(object? sender, byte[] frame)
        {
            _protocolParser.ParseProtocol(frame);
        }

        private void OnGetDeviceIdReceived(object? sender, GetDeviceIdArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(() => this.Text = "Test and Validation - (Device 0x" + e.DeviceId.ToString("X2") +
                                ", v" + e.FirmwareMajorVersion + "." + e.FirmwareMinorVersion.ToString("D2") +
                                "-" + e.FirmwarePatchVersion.ToString("D2") + ")");
            } else
            {
                this.Text = "Test and Validation - (Device 0x" + e.DeviceId.ToString("X2") +
                            ", v" + e.FirmwareMajorVersion + "." + e.FirmwareMinorVersion.ToString("D2") +
                            "-" + e.FirmwarePatchVersion.ToString("D2") + ")";
            }
        }

        private void OnCanStartReceived(object? sender, CanStartArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(() =>
                {
                    button_FilterSettings.Enabled = !e.Success;
                    button_busStart.Text = e.Success ? "Stop" : "Start";
                    UpdateSendFrameControls();
                });
            } else
            {
                button_FilterSettings.Enabled = !e.Success;
                button_busStart.Text = e.Success ? "Stop" : "Start";
                UpdateSendFrameControls();
            }
        }

        private void OnCanStopReceived(object? sender, CanStopArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(() =>
                {
                    if (!e.Success)
                    {
                        MessageBox.Show(this, "Failed to stop CAN bus.", "CAN Stop", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    button_FilterSettings.Enabled = true;
                    button_busStart.Text = "Start";
                    UpdateSendFrameControls();
                });
            } else
            {
                if (!e.Success)
                {
                    MessageBox.Show(this, "Failed to stop CAN bus.", "CAN Stop", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                button_FilterSettings.Enabled = true;
                button_busStart.Text = "Start";
                UpdateSendFrameControls();
            }
        }
        private void OnSendUpstreamReceived(object? sender, SendUpstreamArgs e)
        {
            string frameDescription = $"{DateTime.Now:HH:mm:ss.fff} | {e.CanType} | {e.BrsMode} | {e.FrameFormat} | ID: {FormatFrameIdentifier(e.Identifier, e.FrameFormat)} | DLC: {e.Dlc} | Data: {FormatFrameData(e.Data)}";

            // Queue only; the UI is updated in batches by _receivedFrameFlushTimer.
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

        private void OnGetActiveRxFilterCountReceived(object? sender, GetActiveRxFilterCountArgs e)
        {
            /// TODO
        }

        #endregion // Event Handlers: ProtocolParser
        #endregion // Methods

        private void button_FilterSettings_Click(object sender, EventArgs e)
        {
            if (_formFilterSettings.ShowDialog() == DialogResult.OK)
            {
                for (int i = 0; i < _formFilterSettings.StdIdFilterEntries.Length; i++)
                {
                    FilterEntry entry = _formFilterSettings.StdIdFilterEntries[i];
                    _serialCanFd.SetCanRxFilter((byte)i, entry.Enable, FrameFormat.STANDARD, (byte)entry.Mode, entry.Id, entry.Mask);
                }
                for (int i = 0; i < _formFilterSettings.ExtIdFilterEntries.Length; i++)
                {
                    FilterEntry entry = _formFilterSettings.ExtIdFilterEntries[i];
                    _serialCanFd.SetCanRxFilter((byte)i, entry.Enable, FrameFormat.EXTENDED, (byte)entry.Mode, entry.Id, entry.Mask);
                }
            }
        }
    }
}
