namespace serial_canfd_validation
{
    partial class FormValidation
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            comboBox_SerialPortNames = new ComboBox();
            button_ListPortNames = new Button();
            button_Connect = new Button();
            comboBox_BitRate = new ComboBox();
            comboBox_DataBitRate = new ComboBox();
            checkBox_CanFd = new CheckBox();
            checkBox_BitRateSwitch = new CheckBox();
            checkBox_extendedIdentifier = new CheckBox();
            button_busStart = new Button();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            groupBox1 = new GroupBox();
            groupBox_SendFrame = new GroupBox();
            button_SendFrame = new Button();
            textBox_FrameData = new TextBox();
            label_FrameDataHint = new Label();
            label_FrameData = new Label();
            textBox_FrameId = new TextBox();
            label_FrameId = new Label();
            groupBox_ReceivedFrames = new GroupBox();
            textBox_ReceivedFrames = new TextBox();
            groupBox_ProtocolStatus = new GroupBox();
            label_tdc = new Label();
            label_Flag = new Label();
            label_Activity = new Label();
            label_DataLastErrorCode = new Label();
            label_LastErrorCode = new Label();
            groupBox_CanStatistics = new GroupBox();
            label_MaxRxEc = new Label();
            label_RxBuffOverflow = new Label();
            label_UpstreamLossCount = new Label();
            label_DownstreamLossCount = new Label();
            label_PassiveEc = new Label();
            label_MaxTxEc = new Label();
            label_RxEC = new Label();
            label_TxEc = new Label();
            timer_UpdateCanStats = new System.Windows.Forms.Timer(components);
            button_ResetCanStats = new Button();
            groupBox1.SuspendLayout();
            groupBox_SendFrame.SuspendLayout();
            groupBox_ReceivedFrames.SuspendLayout();
            groupBox_ProtocolStatus.SuspendLayout();
            groupBox_CanStatistics.SuspendLayout();
            SuspendLayout();
            // 
            // comboBox_SerialPortNames
            // 
            comboBox_SerialPortNames.FormattingEnabled = true;
            comboBox_SerialPortNames.Location = new Point(138, 45);
            comboBox_SerialPortNames.Name = "comboBox_SerialPortNames";
            comboBox_SerialPortNames.Size = new Size(182, 33);
            comboBox_SerialPortNames.TabIndex = 0;
            // 
            // button_ListPortNames
            // 
            button_ListPortNames.Location = new Point(326, 43);
            button_ListPortNames.Name = "button_ListPortNames";
            button_ListPortNames.Size = new Size(112, 34);
            button_ListPortNames.TabIndex = 1;
            button_ListPortNames.Text = "Refresh";
            button_ListPortNames.UseVisualStyleBackColor = true;
            button_ListPortNames.Click += button_ListPortNames_Click;
            // 
            // button_Connect
            // 
            button_Connect.Location = new Point(326, 83);
            button_Connect.Name = "button_Connect";
            button_Connect.Size = new Size(112, 34);
            button_Connect.TabIndex = 2;
            button_Connect.Text = "Connect";
            button_Connect.UseVisualStyleBackColor = true;
            button_Connect.Click += button_Connect_Click;
            // 
            // comboBox_BitRate
            // 
            comboBox_BitRate.FormattingEnabled = true;
            comboBox_BitRate.Location = new Point(138, 84);
            comboBox_BitRate.Name = "comboBox_BitRate";
            comboBox_BitRate.Size = new Size(182, 33);
            comboBox_BitRate.TabIndex = 3;
            // 
            // comboBox_DataBitRate
            // 
            comboBox_DataBitRate.FormattingEnabled = true;
            comboBox_DataBitRate.Location = new Point(138, 123);
            comboBox_DataBitRate.Name = "comboBox_DataBitRate";
            comboBox_DataBitRate.Size = new Size(182, 33);
            comboBox_DataBitRate.TabIndex = 3;
            // 
            // checkBox_CanFd
            // 
            checkBox_CanFd.AutoSize = true;
            checkBox_CanFd.Enabled = false;
            checkBox_CanFd.Location = new Point(252, 41);
            checkBox_CanFd.Name = "checkBox_CanFd";
            checkBox_CanFd.Size = new Size(60, 29);
            checkBox_CanFd.TabIndex = 4;
            checkBox_CanFd.Text = "FD";
            checkBox_CanFd.UseVisualStyleBackColor = true;
            checkBox_CanFd.CheckStateChanged += checkBox_CanFd_CheckStateChanged;
            // 
            // checkBox_BitRateSwitch
            // 
            checkBox_BitRateSwitch.AutoSize = true;
            checkBox_BitRateSwitch.Enabled = false;
            checkBox_BitRateSwitch.Location = new Point(318, 43);
            checkBox_BitRateSwitch.Name = "checkBox_BitRateSwitch";
            checkBox_BitRateSwitch.Size = new Size(69, 29);
            checkBox_BitRateSwitch.TabIndex = 5;
            checkBox_BitRateSwitch.Text = "BRS";
            checkBox_BitRateSwitch.UseVisualStyleBackColor = true;
            // 
            // checkBox_extendedIdentifier
            // 
            checkBox_extendedIdentifier.AutoSize = true;
            checkBox_extendedIdentifier.Enabled = false;
            checkBox_extendedIdentifier.Location = new Point(393, 41);
            checkBox_extendedIdentifier.Name = "checkBox_extendedIdentifier";
            checkBox_extendedIdentifier.Size = new Size(77, 29);
            checkBox_extendedIdentifier.TabIndex = 6;
            checkBox_extendedIdentifier.Text = "ExtId";
            checkBox_extendedIdentifier.UseVisualStyleBackColor = true;
            // 
            // button_busStart
            // 
            button_busStart.Enabled = false;
            button_busStart.Location = new Point(326, 122);
            button_busStart.Name = "button_busStart";
            button_busStart.Size = new Size(112, 34);
            button_busStart.TabIndex = 7;
            button_busStart.Text = "Start";
            button_busStart.UseVisualStyleBackColor = true;
            button_busStart.Click += button_busStart_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(84, 48);
            label1.Name = "label1";
            label1.Size = new Size(48, 25);
            label1.TabIndex = 8;
            label1.Text = "Port:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(56, 84);
            label2.Name = "label2";
            label2.Size = new Size(76, 25);
            label2.TabIndex = 8;
            label2.Text = "Bit Rate:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(14, 126);
            label3.Name = "label3";
            label3.Size = new Size(118, 25);
            label3.TabIndex = 8;
            label3.Text = "Data Bit Rate:";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(comboBox_BitRate);
            groupBox1.Controls.Add(label3);
            groupBox1.Controls.Add(button_busStart);
            groupBox1.Controls.Add(comboBox_SerialPortNames);
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(button_ListPortNames);
            groupBox1.Controls.Add(label1);
            groupBox1.Controls.Add(button_Connect);
            groupBox1.Controls.Add(comboBox_DataBitRate);
            groupBox1.Location = new Point(12, 23);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(459, 180);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            groupBox1.Text = "Device Communication";
            // 
            // groupBox_SendFrame
            // 
            groupBox_SendFrame.Controls.Add(button_SendFrame);
            groupBox_SendFrame.Controls.Add(textBox_FrameData);
            groupBox_SendFrame.Controls.Add(checkBox_extendedIdentifier);
            groupBox_SendFrame.Controls.Add(label_FrameDataHint);
            groupBox_SendFrame.Controls.Add(checkBox_BitRateSwitch);
            groupBox_SendFrame.Controls.Add(label_FrameData);
            groupBox_SendFrame.Controls.Add(checkBox_CanFd);
            groupBox_SendFrame.Controls.Add(textBox_FrameId);
            groupBox_SendFrame.Controls.Add(label_FrameId);
            groupBox_SendFrame.Enabled = false;
            groupBox_SendFrame.Location = new Point(477, 23);
            groupBox_SendFrame.Name = "groupBox_SendFrame";
            groupBox_SendFrame.Size = new Size(599, 180);
            groupBox_SendFrame.TabIndex = 10;
            groupBox_SendFrame.TabStop = false;
            groupBox_SendFrame.Text = "Send Frame";
            // 
            // button_SendFrame
            // 
            button_SendFrame.Location = new Point(491, 30);
            button_SendFrame.Name = "button_SendFrame";
            button_SendFrame.Size = new Size(79, 94);
            button_SendFrame.TabIndex = 5;
            button_SendFrame.Text = "Send";
            button_SendFrame.UseVisualStyleBackColor = true;
            button_SendFrame.Click += button_SendFrame_Click;
            // 
            // textBox_FrameData
            // 
            textBox_FrameData.Location = new Point(138, 77);
            textBox_FrameData.Name = "textBox_FrameData";
            textBox_FrameData.Size = new Size(332, 31);
            textBox_FrameData.TabIndex = 3;
            // 
            // label_FrameDataHint
            // 
            label_FrameDataHint.AutoSize = true;
            label_FrameDataHint.Location = new Point(138, 111);
            label_FrameDataHint.Name = "label_FrameDataHint";
            label_FrameDataHint.Size = new Size(199, 25);
            label_FrameDataHint.TabIndex = 4;
            label_FrameDataHint.Text = "Hex bytes (ex: 11 22 FF)";
            // 
            // label_FrameData
            // 
            label_FrameData.AutoSize = true;
            label_FrameData.Location = new Point(76, 80);
            label_FrameData.Name = "label_FrameData";
            label_FrameData.Size = new Size(53, 25);
            label_FrameData.TabIndex = 2;
            label_FrameData.Text = "Data:";
            // 
            // textBox_FrameId
            // 
            textBox_FrameId.Location = new Point(138, 39);
            textBox_FrameId.Name = "textBox_FrameId";
            textBox_FrameId.Size = new Size(96, 31);
            textBox_FrameId.TabIndex = 1;
            // 
            // label_FrameId
            // 
            label_FrameId.AutoSize = true;
            label_FrameId.Location = new Point(94, 42);
            label_FrameId.Name = "label_FrameId";
            label_FrameId.Size = new Size(34, 25);
            label_FrameId.TabIndex = 0;
            label_FrameId.Text = "ID:";
            // 
            // groupBox_ReceivedFrames
            // 
            groupBox_ReceivedFrames.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBox_ReceivedFrames.Controls.Add(textBox_ReceivedFrames);
            groupBox_ReceivedFrames.Location = new Point(12, 333);
            groupBox_ReceivedFrames.Name = "groupBox_ReceivedFrames";
            groupBox_ReceivedFrames.Size = new Size(1157, 225);
            groupBox_ReceivedFrames.TabIndex = 11;
            groupBox_ReceivedFrames.TabStop = false;
            groupBox_ReceivedFrames.Text = "Received Frames";
            // 
            // textBox_ReceivedFrames
            // 
            textBox_ReceivedFrames.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBox_ReceivedFrames.Enabled = false;
            textBox_ReceivedFrames.Location = new Point(3, 27);
            textBox_ReceivedFrames.Multiline = true;
            textBox_ReceivedFrames.Name = "textBox_ReceivedFrames";
            textBox_ReceivedFrames.ReadOnly = true;
            textBox_ReceivedFrames.ScrollBars = ScrollBars.Vertical;
            textBox_ReceivedFrames.Size = new Size(1151, 195);
            textBox_ReceivedFrames.TabIndex = 0;
            // 
            // groupBox_ProtocolStatus
            // 
            groupBox_ProtocolStatus.Controls.Add(label_tdc);
            groupBox_ProtocolStatus.Controls.Add(label_Flag);
            groupBox_ProtocolStatus.Controls.Add(label_Activity);
            groupBox_ProtocolStatus.Controls.Add(label_DataLastErrorCode);
            groupBox_ProtocolStatus.Controls.Add(label_LastErrorCode);
            groupBox_ProtocolStatus.Location = new Point(12, 209);
            groupBox_ProtocolStatus.Name = "groupBox_ProtocolStatus";
            groupBox_ProtocolStatus.Size = new Size(523, 118);
            groupBox_ProtocolStatus.TabIndex = 12;
            groupBox_ProtocolStatus.TabStop = false;
            groupBox_ProtocolStatus.Text = "Protocol Status";
            // 
            // label_tdc
            // 
            label_tdc.AutoSize = true;
            label_tdc.Location = new Point(410, 27);
            label_tdc.Name = "label_tdc";
            label_tdc.Size = new Size(75, 25);
            label_tdc.TabIndex = 14;
            label_tdc.Text = "TDC: ---";
            // 
            // label_Flag
            // 
            label_Flag.AutoSize = true;
            label_Flag.Location = new Point(290, 63);
            label_Flag.Name = "label_Flag";
            label_Flag.Size = new Size(75, 25);
            label_Flag.TabIndex = 14;
            label_Flag.Text = "Flag: ---";
            // 
            // label_Activity
            // 
            label_Activity.AutoSize = true;
            label_Activity.Location = new Point(265, 27);
            label_Activity.Name = "label_Activity";
            label_Activity.Size = new Size(100, 25);
            label_Activity.TabIndex = 14;
            label_Activity.Text = "Activity: ---";
            // 
            // label_DataLastErrorCode
            // 
            label_DataLastErrorCode.AutoSize = true;
            label_DataLastErrorCode.Location = new Point(14, 63);
            label_DataLastErrorCode.Name = "label_DataLastErrorCode";
            label_DataLastErrorCode.Size = new Size(205, 25);
            label_DataLastErrorCode.TabIndex = 14;
            label_DataLastErrorCode.Text = "Data Last Error Code: ---";
            // 
            // label_LastErrorCode
            // 
            label_LastErrorCode.AutoSize = true;
            label_LastErrorCode.Location = new Point(56, 27);
            label_LastErrorCode.Name = "label_LastErrorCode";
            label_LastErrorCode.Size = new Size(163, 25);
            label_LastErrorCode.TabIndex = 13;
            label_LastErrorCode.Text = "Last Error Code: ---";
            // 
            // groupBox_CanStatistics
            // 
            groupBox_CanStatistics.Controls.Add(button_ResetCanStats);
            groupBox_CanStatistics.Controls.Add(label_MaxRxEc);
            groupBox_CanStatistics.Controls.Add(label_RxBuffOverflow);
            groupBox_CanStatistics.Controls.Add(label_UpstreamLossCount);
            groupBox_CanStatistics.Controls.Add(label_DownstreamLossCount);
            groupBox_CanStatistics.Controls.Add(label_PassiveEc);
            groupBox_CanStatistics.Controls.Add(label_MaxTxEc);
            groupBox_CanStatistics.Controls.Add(label_RxEC);
            groupBox_CanStatistics.Controls.Add(label_TxEc);
            groupBox_CanStatistics.Location = new Point(541, 209);
            groupBox_CanStatistics.Name = "groupBox_CanStatistics";
            groupBox_CanStatistics.Size = new Size(625, 118);
            groupBox_CanStatistics.TabIndex = 13;
            groupBox_CanStatistics.TabStop = false;
            groupBox_CanStatistics.Text = "CAN Statistics";
            // 
            // label_MaxRxEc
            // 
            label_MaxRxEc.AutoSize = true;
            label_MaxRxEc.Location = new Point(121, 63);
            label_MaxRxEc.Name = "label_MaxRxEc";
            label_MaxRxEc.Size = new Size(111, 25);
            label_MaxRxEc.TabIndex = 0;
            label_MaxRxEc.Text = "Max REC: ---";
            // 
            // label_RxBuffOverflow
            // 
            label_RxBuffOverflow.AutoSize = true;
            label_RxBuffOverflow.Location = new Point(387, 63);
            label_RxBuffOverflow.Name = "label_RxBuffOverflow";
            label_RxBuffOverflow.Size = new Size(91, 25);
            label_RxBuffOverflow.TabIndex = 0;
            label_RxBuffOverflow.Text = "Rx OV: ---";
            // 
            // label_UpstreamLossCount
            // 
            label_UpstreamLossCount.AutoSize = true;
            label_UpstreamLossCount.Location = new Point(373, 27);
            label_UpstreamLossCount.Name = "label_UpstreamLossCount";
            label_UpstreamLossCount.Size = new Size(105, 25);
            label_UpstreamLossCount.TabIndex = 0;
            label_UpstreamLossCount.Text = "Up Loss: ---";
            // 
            // label_DownstreamLossCount
            // 
            label_DownstreamLossCount.AutoSize = true;
            label_DownstreamLossCount.Location = new Point(238, 63);
            label_DownstreamLossCount.Name = "label_DownstreamLossCount";
            label_DownstreamLossCount.Size = new Size(129, 25);
            label_DownstreamLossCount.TabIndex = 0;
            label_DownstreamLossCount.Text = "Down Loss: ---";
            // 
            // label_PassiveEc
            // 
            label_PassiveEc.AutoSize = true;
            label_PassiveEc.Location = new Point(244, 27);
            label_PassiveEc.Name = "label_PassiveEc";
            label_PassiveEc.Size = new Size(123, 25);
            label_PassiveEc.TabIndex = 0;
            label_PassiveEc.Text = "Passive EC: ---";
            // 
            // label_MaxTxEc
            // 
            label_MaxTxEc.AutoSize = true;
            label_MaxTxEc.Location = new Point(121, 27);
            label_MaxTxEc.Name = "label_MaxTxEc";
            label_MaxTxEc.Size = new Size(109, 25);
            label_MaxTxEc.TabIndex = 0;
            label_MaxTxEc.Text = "Max TEC: ---";
            // 
            // label_RxEC
            // 
            label_RxEC.AutoSize = true;
            label_RxEC.Location = new Point(30, 63);
            label_RxEC.Name = "label_RxEC";
            label_RxEC.Size = new Size(73, 25);
            label_RxEC.TabIndex = 0;
            label_RxEC.Text = "REC: ---";
            // 
            // label_TxEc
            // 
            label_TxEc.AutoSize = true;
            label_TxEc.Location = new Point(30, 27);
            label_TxEc.Name = "label_TxEc";
            label_TxEc.Size = new Size(71, 25);
            label_TxEc.TabIndex = 0;
            label_TxEc.Text = "TEC: ---";
            // 
            // timer_UpdateCanStats
            // 
            timer_UpdateCanStats.Interval = 1000;
            timer_UpdateCanStats.Tick += timer_UpdateCanStats_Tick;
            // 
            // button_ResetCanStats
            // 
            button_ResetCanStats.Location = new Point(509, 27);
            button_ResetCanStats.Name = "button_ResetCanStats";
            button_ResetCanStats.Size = new Size(98, 66);
            button_ResetCanStats.TabIndex = 1;
            button_ResetCanStats.Text = "Reset";
            button_ResetCanStats.UseVisualStyleBackColor = true;
            button_ResetCanStats.Click += button_ResetCanStats_Click;
            // 
            // FormValidation
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1181, 570);
            Controls.Add(groupBox_CanStatistics);
            Controls.Add(groupBox_ProtocolStatus);
            Controls.Add(groupBox_ReceivedFrames);
            Controls.Add(groupBox_SendFrame);
            Controls.Add(groupBox1);
            Name = "FormValidation";
            Text = "Test and Validation";
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            groupBox_SendFrame.ResumeLayout(false);
            groupBox_SendFrame.PerformLayout();
            groupBox_ReceivedFrames.ResumeLayout(false);
            groupBox_ReceivedFrames.PerformLayout();
            groupBox_ProtocolStatus.ResumeLayout(false);
            groupBox_ProtocolStatus.PerformLayout();
            groupBox_CanStatistics.ResumeLayout(false);
            groupBox_CanStatistics.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private ComboBox comboBox_SerialPortNames;
        private Button button_ListPortNames;
        private Button button_Connect;
        private ComboBox comboBox_BitRate;
        private ComboBox comboBox_DataBitRate;
        private CheckBox checkBox_CanFd;
        private CheckBox checkBox_BitRateSwitch;
        private CheckBox checkBox_extendedIdentifier;
        private Button button_busStart;
        private Label label1;
        private Label label2;
        private Label label3;
        private GroupBox groupBox1;
        private GroupBox groupBox_SendFrame;
        private Button button_SendFrame;
        private TextBox textBox_FrameData;
        private Label label_FrameDataHint;
        private Label label_FrameData;
        private TextBox textBox_FrameId;
        private Label label_FrameId;
        private GroupBox groupBox_ReceivedFrames;
        private TextBox textBox_ReceivedFrames;
        private GroupBox groupBox_ProtocolStatus;
        private Label label_LastErrorCode;
        private Label label_DataLastErrorCode;
        private Label label_Activity;
        private Label label_Flag;
        private Label label_tdc;
        private GroupBox groupBox_CanStatistics;
        private Label label_TxEc;
        private Label label_MaxTxEc;
        private Label label_RxEC;
        private Label label_MaxRxEc;
        private Label label_PassiveEc;
        private Label label_DownstreamLossCount;
        private Label label_UpstreamLossCount;
        private Label label_RxBuffOverflow;
        private System.Windows.Forms.Timer timer_UpdateCanStats;
        private Button button_ResetCanStats;
    }
}
