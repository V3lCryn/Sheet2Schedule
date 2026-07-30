using System;
using System.Collections.Generic;
using System.Linq;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using Sheet2Schedule.Models;
using Sheet2Schedule.Services;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// Quick, no-config-saved import: pick a sheet, type a cell range (e.g. B2:P29),
    /// preview it, and import - without going through the full "New Schedule Type"
    /// wizard or needing a pre-built config. Good for one-off imports.
    /// </summary>
    public class ImportByRangeForm : WinForms.Form
    {
        public bool UserConfirmedImport { get; private set; }
        public EquipmentScheduleDefinition ResultDefinition { get; private set; }
        public List<ScheduleRow> ResultRows { get; private set; }

        private readonly string _excelPath;
        private readonly List<string> _sheetNames;

        private WinForms.ComboBox _sheetCombo;
        private WinForms.TextBox _rangeBox;
        private WinForms.TextBox _scheduleNameBox;
        private WinForms.Button _previewButton;
        private WinForms.DataGridView _previewGrid;
        private WinForms.Button _importButton;
        private WinForms.Button _cancelButton;
        private WinForms.Label _statusLabel;
        private WinForms.ProgressBar _progressBar;

        private static readonly Drawing.Font AppFont = new Drawing.Font("Segoe UI", 9.5f);
        private static readonly Drawing.Font AppFontBold = new Drawing.Font("Segoe UI", 9.5f, Drawing.FontStyle.Bold);
        private static readonly Drawing.Color AccentColor = Drawing.Color.FromArgb(47, 84, 150);
        private static readonly Drawing.Color ZebraColor = Drawing.Color.FromArgb(246, 247, 249);

        public ImportByRangeForm(string excelPath, List<string> sheetNames)
        {
            _excelPath = excelPath;
            _sheetNames = sheetNames;
            AutoScaleMode = WinForms.AutoScaleMode.Font;
            Font = AppFont;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Import by Range";
            Width = 820;
            Height = 560;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            BackColor = Drawing.Color.White;

            var titleLabel = new WinForms.Label
            {
                Text = "Import a specific cell range",
                Left = 16,
                Top = 12,
                Width = 400,
                Font = AppFontBold,
                ForeColor = AccentColor
            };

            var sheetLabel = new WinForms.Label { Text = "Sheet:", Left = 16, Top = 44, Width = 50 };
            _sheetCombo = new WinForms.ComboBox
            {
                Left = 70,
                Top = 40,
                Width = 250,
                Font = AppFont,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };
            foreach (var name in _sheetNames)
                _sheetCombo.Items.Add(name);
            if (_sheetCombo.Items.Count > 0)
                _sheetCombo.SelectedIndex = 0;

            var rangeLabel = new WinForms.Label { Text = "Range:", Left = 336, Top = 44, Width = 50 };
            _rangeBox = new WinForms.TextBox
            {
                Left = 390,
                Top = 40,
                Width = 120,
                Font = AppFont,
                Text = "B2:P29"
            };

            _previewButton = new WinForms.Button
            {
                Text = "Preview",
                Left = 522,
                Top = 38,
                Width = 90,
                Height = 28,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont
            };
            _previewButton.FlatAppearance.BorderColor = AccentColor;
            _previewButton.Click += (s, e) => RunPreview();

            var rangeHint = new WinForms.Label
            {
                Text = "Format: StartCell:EndCell, e.g. B2:P29. The first row of the range is treated as column headers.",
                Left = 16,
                Top = 72,
                Width = 780,
                Font = new Drawing.Font(AppFont, Drawing.FontStyle.Italic),
                ForeColor = Drawing.Color.Gray
            };

            _statusLabel = new WinForms.Label
            {
                Left = 16,
                Top = 98,
                Width = 780,
                Height = 20,
                ForeColor = Drawing.Color.Firebrick
            };

            _previewGrid = new WinForms.DataGridView
            {
                Left = 16,
                Top = 124,
                Width = 780,
                Height = 300,
                Font = AppFont,
                BackgroundColor = Drawing.Color.White,
                BorderStyle = WinForms.BorderStyle.FixedSingle,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = { BackColor = ZebraColor },
                ColumnHeadersDefaultCellStyle = { Font = AppFontBold, BackColor = Drawing.Color.FromArgb(240, 242, 245) }
            };

            var nameLabel = new WinForms.Label
            {
                Text = "Schedule name:",
                Left = 16,
                Top = 436,
                Width = 110,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left
            };
            _scheduleNameBox = new WinForms.TextBox
            {
                Left = 130,
                Top = 432,
                Width = 300,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left
            };

            _progressBar = new WinForms.ProgressBar
            {
                Left = 16,
                Top = 468,
                Width = 780,
                Height = 10,
                Style = WinForms.ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right
            };

            _importButton = new WinForms.Button
            {
                Text = "Import",
                Left = 546,
                Top = 488,
                Width = 110,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Drawing.Color.White,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            _importButton.FlatAppearance.BorderSize = 0;
            _importButton.Click += (s, e) => ConfirmImport();

            _cancelButton = new WinForms.Button
            {
                Text = "Cancel",
                Left = 664,
                Top = 488,
                Width = 92,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            _cancelButton.Click += (s, e) => Close();

            Controls.Add(titleLabel);
            Controls.Add(sheetLabel);
            Controls.Add(_sheetCombo);
            Controls.Add(rangeLabel);
            Controls.Add(_rangeBox);
            Controls.Add(_previewButton);
            Controls.Add(rangeHint);
            Controls.Add(_statusLabel);
            Controls.Add(_previewGrid);
            Controls.Add(nameLabel);
            Controls.Add(_scheduleNameBox);
            Controls.Add(_progressBar);
            Controls.Add(_importButton);
            Controls.Add(_cancelButton);
        }

        private (EquipmentScheduleDefinition Definition, List<ScheduleRow> Rows) _lastResult;

        private void RunPreview()
        {
            _statusLabel.Text = "";
            _progressBar.Visible = true;
            _previewButton.Enabled = false;
            WinForms.Application.DoEvents();

            try
            {
                var parsed = RangeParser.Parse(_rangeBox.Text);
                string sheetName = _sheetCombo.SelectedItem.ToString();
                string tempName = string.IsNullOrWhiteSpace(_scheduleNameBox.Text) ? "Custom Range Import" : _scheduleNameBox.Text;

                var reader = new ExcelReader();
                _lastResult = reader.ReadCustomRange(
                    _excelPath, sheetName, tempName,
                    parsed.StartColumn, parsed.StartRow, parsed.EndColumn, parsed.EndRow);

                _previewGrid.Columns.Clear();
                _previewGrid.Rows.Clear();

                foreach (var col in _lastResult.Definition.Columns)
                    _previewGrid.Columns.Add(col.RevitFieldName, col.RevitFieldName);

                foreach (var row in _lastResult.Rows)
                {
                    var values = _lastResult.Definition.Columns
                        .Select(c => row.Get(c.RevitFieldName))
                        .ToArray();
                    _previewGrid.Rows.Add(values);
                }

                _statusLabel.ForeColor = Drawing.Color.SeaGreen;
                _statusLabel.Text = $"{_lastResult.Rows.Count} row(s), {_lastResult.Definition.Columns.Count} column(s) found in this range.";
            }
            catch (Exception ex)
            {
                _lastResult = default;
                _previewGrid.Columns.Clear();
                _previewGrid.Rows.Clear();
                _statusLabel.ForeColor = Drawing.Color.Firebrick;
                _statusLabel.Text = ex.Message;
            }
            finally
            {
                _progressBar.Visible = false;
                _previewButton.Enabled = true;
            }
        }

        private void ConfirmImport()
        {
            if (_lastResult.Definition == null || _lastResult.Rows == null)
            {
                WinForms.MessageBox.Show(this, "Click Preview first to check the range before importing.",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_scheduleNameBox.Text))
            {
                WinForms.MessageBox.Show(this, "Enter a schedule name first.",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            _lastResult.Definition.ScheduleName = _scheduleNameBox.Text.Trim();

            ResultDefinition = _lastResult.Definition;
            ResultRows = _lastResult.Rows;
            UserConfirmedImport = true;

            DialogResult = WinForms.DialogResult.OK;
            Close();
        }
    }
}