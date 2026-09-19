namespace serial_canfd_validation
{
    partial class FormFilterSettings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            tabControl_filter = new TabControl();
            tabPage_stdId = new TabPage();
            dataGridView_stdId = new DataGridView();
            column_stdIndex = new DataGridViewTextBoxColumn();
            column_stdEnable = new DataGridViewCheckBoxColumn();
            column_stdMode = new DataGridViewComboBoxColumn();
            column_stdId = new DataGridViewTextBoxColumn();
            column_stdMask = new DataGridViewTextBoxColumn();
            tabPage_extId = new TabPage();
            dataGridView_extId = new DataGridView();
            column_extIndex = new DataGridViewTextBoxColumn();
            column_extEnable = new DataGridViewCheckBoxColumn();
            column_extMode = new DataGridViewComboBoxColumn();
            column_extId = new DataGridViewTextBoxColumn();
            column_extMask = new DataGridViewTextBoxColumn();
            button_ok = new Button();
            tabControl_filter.SuspendLayout();
            tabPage_stdId.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_stdId).BeginInit();
            tabPage_extId.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridView_extId).BeginInit();
            SuspendLayout();
            // 
            // tabControl_filter
            // 
            tabControl_filter.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl_filter.Controls.Add(tabPage_stdId);
            tabControl_filter.Controls.Add(tabPage_extId);
            tabControl_filter.Location = new Point(12, 59);
            tabControl_filter.Name = "tabControl_filter";
            tabControl_filter.SelectedIndex = 0;
            tabControl_filter.Size = new Size(616, 346);
            tabControl_filter.TabIndex = 4;
            // 
            // tabPage_stdId
            // 
            tabPage_stdId.Controls.Add(dataGridView_stdId);
            tabPage_stdId.Location = new Point(4, 34);
            tabPage_stdId.Name = "tabPage_stdId";
            tabPage_stdId.Padding = new Padding(3);
            tabPage_stdId.Size = new Size(608, 308);
            tabPage_stdId.TabIndex = 0;
            tabPage_stdId.Text = "Standard ID";
            tabPage_stdId.UseVisualStyleBackColor = true;
            // 
            // dataGridView_stdId
            // 
            dataGridView_stdId.AllowUserToAddRows = false;
            dataGridView_stdId.AllowUserToDeleteRows = false;
            dataGridView_stdId.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_stdId.Columns.AddRange(new DataGridViewColumn[] { column_stdIndex, column_stdEnable, column_stdMode, column_stdId, column_stdMask });
            dataGridView_stdId.Dock = DockStyle.Fill;
            dataGridView_stdId.Location = new Point(3, 3);
            dataGridView_stdId.Name = "dataGridView_stdId";
            dataGridView_stdId.RowHeadersVisible = false;
            dataGridView_stdId.RowHeadersWidth = 62;
            dataGridView_stdId.Size = new Size(602, 302);
            dataGridView_stdId.TabIndex = 0;
            dataGridView_stdId.CellValidating += dataGridView_stdId_CellValidating;
            // 
            // column_stdIndex
            // 
            column_stdIndex.HeaderText = "Index";
            column_stdIndex.MinimumWidth = 8;
            column_stdIndex.Name = "column_stdIndex";
            column_stdIndex.ReadOnly = true;
            column_stdIndex.Width = 80;
            // 
            // column_stdEnable
            // 
            column_stdEnable.HeaderText = "Enable";
            column_stdEnable.MinimumWidth = 8;
            column_stdEnable.Name = "column_stdEnable";
            column_stdEnable.Width = 80;
            // 
            // column_stdMode
            // 
            column_stdMode.HeaderText = "Mode";
            column_stdMode.Items.AddRange(new object[] { "ID_MATCH", "MASK_MATCH" });
            column_stdMode.MinimumWidth = 8;
            column_stdMode.Name = "column_stdMode";
            column_stdMode.Width = 170;
            // 
            // column_stdId
            // 
            column_stdId.HeaderText = "ID";
            column_stdId.MinimumWidth = 8;
            column_stdId.Name = "column_stdId";
            column_stdId.Width = 120;
            // 
            // column_stdMask
            // 
            column_stdMask.HeaderText = "Mask";
            column_stdMask.MinimumWidth = 8;
            column_stdMask.Name = "column_stdMask";
            column_stdMask.Width = 120;
            // 
            // tabPage_extId
            // 
            tabPage_extId.Controls.Add(dataGridView_extId);
            tabPage_extId.Location = new Point(4, 34);
            tabPage_extId.Name = "tabPage_extId";
            tabPage_extId.Padding = new Padding(3);
            tabPage_extId.Size = new Size(608, 308);
            tabPage_extId.TabIndex = 1;
            tabPage_extId.Text = "Extended ID";
            tabPage_extId.UseVisualStyleBackColor = true;
            // 
            // dataGridView_extId
            // 
            dataGridView_extId.AllowUserToAddRows = false;
            dataGridView_extId.AllowUserToDeleteRows = false;
            dataGridView_extId.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView_extId.Columns.AddRange(new DataGridViewColumn[] { column_extIndex, column_extEnable, column_extMode, column_extId, column_extMask });
            dataGridView_extId.Dock = DockStyle.Fill;
            dataGridView_extId.Location = new Point(3, 3);
            dataGridView_extId.Name = "dataGridView_extId";
            dataGridView_extId.RowHeadersVisible = false;
            dataGridView_extId.RowHeadersWidth = 62;
            dataGridView_extId.Size = new Size(602, 302);
            dataGridView_extId.TabIndex = 0;
            dataGridView_extId.CellValidating += dataGridView_extId_CellValidating;
            // 
            // column_extIndex
            // 
            column_extIndex.HeaderText = "Index";
            column_extIndex.MinimumWidth = 8;
            column_extIndex.Name = "column_extIndex";
            column_extIndex.ReadOnly = true;
            column_extIndex.Width = 80;
            // 
            // column_extEnable
            // 
            column_extEnable.HeaderText = "Enable";
            column_extEnable.MinimumWidth = 8;
            column_extEnable.Name = "column_extEnable";
            column_extEnable.Width = 80;
            // 
            // column_extMode
            // 
            column_extMode.HeaderText = "Mode";
            column_extMode.Items.AddRange(new object[] { "ID_MATCH", "MASK_MATCH" });
            column_extMode.MinimumWidth = 8;
            column_extMode.Name = "column_extMode";
            column_extMode.Width = 170;
            // 
            // column_extId
            // 
            column_extId.HeaderText = "ID";
            column_extId.MinimumWidth = 8;
            column_extId.Name = "column_extId";
            column_extId.Width = 120;
            // 
            // column_extMask
            // 
            column_extMask.HeaderText = "Mask";
            column_extMask.MinimumWidth = 8;
            column_extMask.Name = "column_extMask";
            column_extMask.Width = 120;
            // 
            // button_ok
            // 
            button_ok.Location = new Point(522, 19);
            button_ok.Name = "button_ok";
            button_ok.Size = new Size(99, 34);
            button_ok.TabIndex = 5;
            button_ok.Text = "OK";
            button_ok.UseVisualStyleBackColor = true;
            button_ok.Click += button_ok_Click;
            // 
            // FormFilterSettings
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(640, 419);
            Controls.Add(button_ok);
            Controls.Add(tabControl_filter);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            Name = "FormFilterSettings";
            Text = "Filter Settings";
            Load += FormFilterSettings_Load;
            tabControl_filter.ResumeLayout(false);
            tabPage_stdId.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_stdId).EndInit();
            tabPage_extId.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridView_extId).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private TabControl tabControl_filter;
        private TabPage tabPage_stdId;
        private TabPage tabPage_extId;
        private DataGridView dataGridView_stdId;
        private DataGridView dataGridView_extId;
        private DataGridViewTextBoxColumn column_stdIndex;
        private DataGridViewCheckBoxColumn column_stdEnable;
        private DataGridViewComboBoxColumn column_stdMode;
        private DataGridViewTextBoxColumn column_stdId;
        private DataGridViewTextBoxColumn column_stdMask;
        private DataGridViewTextBoxColumn column_extIndex;
        private DataGridViewCheckBoxColumn column_extEnable;
        private DataGridViewComboBoxColumn column_extMode;
        private DataGridViewTextBoxColumn column_extId;
        private DataGridViewTextBoxColumn column_extMask;
        private Button button_ok;
    }
    
}