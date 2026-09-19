using libSerialCanFD;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace serial_canfd_validation
{
    public partial class FormFilterSettings : Form
    {
        public FilterEntry[] StdIdFilterEntries { get; private set; }
        public byte activeStdIdFilterCount { get; private set; }
        public FilterEntry[] ExtIdFilterEntries { get; private set; }
        public byte activeExtIdFilterCount { get; private set; }

        private SerialCanFd _serialCanFd;
        private ProtocolParser _protocolParser;

        public FormFilterSettings(ref SerialCanFd serialCanFd, ref ProtocolParser protocolParser)
        {
            InitializeComponent();

            this._serialCanFd = serialCanFd;
            this._protocolParser = protocolParser;

            StdIdFilterEntries = new FilterEntry[ProtocolParser.STD_ID_FILTER_COUNT];
            ExtIdFilterEntries = new FilterEntry[ProtocolParser.EXT_ID_FILTER_COUNT];

            InitializeStdIdGrid();
            InitializeExtIdGrid();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this._protocolParser.GetRxFilterInfoReceived -= OnGetRxFilterInfoReceived;

            UpdateStdIdFilterEntriesFromGrid();
            UpdateExtIdFilterEntriesFromGrid();

            byte count = 0;
            for(int i = 0; i < StdIdFilterEntries.Length; i++)
            {
                if (StdIdFilterEntries[i].Enable)
                {
                    count++;
                }
            }
            activeStdIdFilterCount = count;
            count = 0;
            for(int i = 0; i < ExtIdFilterEntries.Length; i++)
            {
                if (ExtIdFilterEntries[i].Enable)
                {
                    count++;
                }
            }
            activeExtIdFilterCount = count;
            base.OnFormClosed(e);
        }

        private void UpdateStdIdFilterEntriesFromGrid()
        {
            for (int i = 0; i < StdIdFilterEntries.Length && i < dataGridView_stdId.Rows.Count; i++)
            {
                DataGridViewRow row = dataGridView_stdId.Rows[i];
                FilterEntry entry = StdIdFilterEntries[i];

                entry.Enable = Convert.ToBoolean(row.Cells[column_stdEnable.Index].Value ?? false);

                if (Enum.TryParse(row.Cells[column_stdMode.Index].Value?.ToString(), out FilterMode mode))
                {
                    entry.Mode = mode;
                }

                if (TryParseHex(row.Cells[column_stdId.Index].Value?.ToString(), 0x7FF, out uint id))
                {
                    entry.Id = id;
                }

                if (TryParseHex(row.Cells[column_stdMask.Index].Value?.ToString(), 0x7FF, out uint mask))
                {
                    entry.Mask = mask;
                }
            }
        }

        private void UpdateExtIdFilterEntriesFromGrid()
        {
            for (int i = 0; i < ExtIdFilterEntries.Length && i < dataGridView_extId.Rows.Count; i++)
            {
                DataGridViewRow row = dataGridView_extId.Rows[i];
                FilterEntry entry = ExtIdFilterEntries[i];

                entry.Enable = Convert.ToBoolean(row.Cells[column_extEnable.Index].Value ?? false);

                if (Enum.TryParse(row.Cells[column_extMode.Index].Value?.ToString(), out FilterMode mode))
                {
                    entry.Mode = mode;
                }

                if (TryParseHex(row.Cells[column_extId.Index].Value?.ToString(), 0x1FFFFFFF, out uint id))
                {
                    entry.Id = id;
                }

                if (TryParseHex(row.Cells[column_extMask.Index].Value?.ToString(), 0x1FFFFFFF, out uint mask))
                {
                    entry.Mask = mask;
                }
            }
        }

        private void ReadFilterSettings()
        {
            int stdCount = StdIdFilterEntries.Length;
            int extCount = ExtIdFilterEntries.Length;

            // Each request is a blocking SerialPort.Write, so keep it off the UI thread.
            Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < stdCount; i++)
                    {
                        _serialCanFd.GetCanRxFilterInfo(FrameFormat.STANDARD, (byte)i);
                    }
                    for (int i = 0; i < extCount; i++)
                    {
                        _serialCanFd.GetCanRxFilterInfo(FrameFormat.EXTENDED, (byte)i);
                    }
                } catch (InvalidOperationException)
                {
                    // Port closed while reading the filter settings.
                }
            });
        }

        private void InitializeExtIdGrid()
        {
            for (int i = 0; i < ExtIdFilterEntries.Length; i++)
            {
                ExtIdFilterEntries[i] = new FilterEntry
                {
                    Index = i,
                    Enable = false,
                    Mode = FilterMode.ID_MATCH,
                    Id = 0x00000000,
                    Mask = 0x1FFF_FFFF  // 29-bit mask
                };
            }

            dataGridView_extId.Rows.Clear();
            dataGridView_extId.Rows.Add(ExtIdFilterEntries.Length);
            for (int i = 0; i < ExtIdFilterEntries.Length; i++)
            {
                FilterEntry entry = ExtIdFilterEntries[i];
                DataGridViewRow row = dataGridView_extId.Rows[i];
                row.Cells[column_extIndex.Index].Value = entry.Index;
                row.Cells[column_extEnable.Index].Value = entry.Enable;
                row.Cells[column_extMode.Index].Value = entry.Mode.ToString();
                row.Cells[column_extId.Index].Value = $"0x{entry.Id:X8}";
                row.Cells[column_extMask.Index].Value = $"0x{entry.Mask:X8}";
            }
        }

        private void InitializeStdIdGrid()
        {
            for (int i = 0; i < StdIdFilterEntries.Length; i++)
            {
                StdIdFilterEntries[i] = new FilterEntry
                {
                    Index = i,
                    Enable = false,
                    Mode = FilterMode.ID_MATCH,
                    Id = 0x000,
                    Mask = 0x7FF  // 11-bit mask
                };
            }
            dataGridView_stdId.Rows.Clear();
            dataGridView_stdId.Rows.Add(StdIdFilterEntries.Length);
            for (int i = 0; i < StdIdFilterEntries.Length; i++)
            {
                FilterEntry entry = StdIdFilterEntries[i];
                DataGridViewRow row = dataGridView_stdId.Rows[i];
                row.Cells[column_stdIndex.Index].Value = entry.Index;
                row.Cells[column_stdEnable.Index].Value = entry.Enable;
                row.Cells[column_stdMode.Index].Value = entry.Mode.ToString();
                row.Cells[column_stdId.Index].Value = $"0x{entry.Id:X3}";
                row.Cells[column_stdMask.Index].Value = $"0x{entry.Mask:X3}";
            }
        }

        private static bool TryParseHex(string? text, uint maxValue, out uint value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                trimmed = trimmed.Substring(2);
            }

            if (trimmed.Length == 0 ||
                !uint.TryParse(trimmed, System.Globalization.NumberStyles.HexNumber,
                               System.Globalization.CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            return value <= maxValue;
        }

        private void dataGridView_stdId_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != column_stdId.Index && e.ColumnIndex != column_stdMask.Index)
            {
                return;
            }

            if (!TryParseHex(e.FormattedValue?.ToString(), 0x7FF, out uint value))
            {
                MessageBox.Show(
                    "Value must be in hex format within 0x000 - 0x7FF (11-bit identifier).",
                    "Invalid value",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            dataGridView_stdId.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = $"0x{value:X3}";
        }

        private void dataGridView_extId_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.ColumnIndex != column_extId.Index && e.ColumnIndex != column_extMask.Index)
            {
                return;
            }

            if (!TryParseHex(e.FormattedValue?.ToString(), 0x1FFFFFFF, out uint value))
            {
                MessageBox.Show(
                    "Value must be in hex format within 0x00000000 - 0x1FFFFFFF (29-bit identifier).",
                    "Invalid value",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            dataGridView_extId.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = $"0x{value:X8}";
        }

        private void OnGetRxFilterInfoReceived(object? sender, GetRxFilterInfoArgs e)
        {
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(() => OnGetRxFilterInfoReceived(sender, e));
                return;
            }

            if (!e.Success)
            {
                return;
            }

            int index = e.FilterIndex;

            FilterEntry[] entries;
            DataGridView grid;
            int idColumnIndex;
            int maskColumnIndex;
            int enableColumnIndex;
            int modeColumnIndex;
            string format;

            if (e.IdType == FrameFormat.STANDARD)
            {
                entries = StdIdFilterEntries;
                grid = dataGridView_stdId;
                enableColumnIndex = column_stdEnable.Index;
                modeColumnIndex = column_stdMode.Index;
                idColumnIndex = column_stdId.Index;
                maskColumnIndex = column_stdMask.Index;
                format = "X3";
            } else
            {
                entries = ExtIdFilterEntries;
                grid = dataGridView_extId;
                enableColumnIndex = column_extEnable.Index;
                modeColumnIndex = column_extMode.Index;
                idColumnIndex = column_extId.Index;
                maskColumnIndex = column_extMask.Index;
                format = "X8";
            }

            if (index < 0 || index >= entries.Length || index >= grid.Rows.Count)
            {
                return;
            }

            FilterEntry entry = entries[index];
            entry.Enable = e.Enable;
            entry.Mode = e.Mode;
            entry.Id = e.IdValue;
            entry.Mask = e.MaskValue;

            DataGridViewRow row = grid.Rows[index];
            row.Cells[enableColumnIndex].Value = entry.Enable;
            row.Cells[modeColumnIndex].Value = entry.Mode.ToString();
            row.Cells[idColumnIndex].Value = "0x" + entry.Id.ToString(format);
            row.Cells[maskColumnIndex].Value = "0x" + entry.Mask.ToString(format);
        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void FormFilterSettings_Load(object sender, EventArgs e)
        {
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Subscribe on every show: the dialog instance is reused, so Load only fires once.
            this._protocolParser.GetRxFilterInfoReceived -= OnGetRxFilterInfoReceived;
            this._protocolParser.GetRxFilterInfoReceived += OnGetRxFilterInfoReceived;
            ReadFilterSettings();
        }
    }

    public class FilterEntry
    {
        public int Index { get; set; }
        public bool Enable { get; set; }
        public FilterMode Mode { get; set; }
        public uint Id { get; set; }
        public uint Mask { get; set; }
    }
}
