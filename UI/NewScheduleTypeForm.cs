using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using Sheet2Schedule.Models;
using Sheet2Schedule.Services;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// "New Schedule Type" wizard: lets the user point at ANY Excel file/sheet the tool
    /// has never seen before, previews the raw data, guesses the header/data rows, and
    /// lets the user confirm/adjust a column-to-Revit-field mapping. On Save, writes a
    /// brand-new config JSON file - after which that schedule type is permanently
    /// available in the normal Import picker, no code changes needed.
    /// </summary>
    public class NewScheduleTypeForm : WinForms.Form
    {
        private readonly string _excelPath;
        private readonly string _configDirectory;

        private WinForms.ComboBox _sheetCombo;
        private WinForms.NumericUpDown _headerRowInput;
        private WinForms.NumericUpDown _dataStartRowInput;
        private WinForms.DataGridView _previewGrid;
        private WinForms.DataGridView _mappingGrid;
        private WinForms.TextBox _scheduleNameBox;
        private WinForms.Button _analyzeButton;
        private WinForms.Button _saveButton;
        private WinForms.Button _cancelButton;
        private WinForms.ProgressBar _progressBar;

        private SheetAnalyzer.PreviewResult _currentPreview;
        private List<string> _sheetNames;

        private static readonly Drawing.Font AppFont = new Drawing.Font("Segoe UI", 9.5f);
        private static readonly Drawing.Font AppFontBold = new Drawing.Font("Segoe UI", 9.5f, Drawing.FontStyle.Bold);
        private static readonly Drawing.Color AccentColor = Drawing.Color.FromArgb(47, 84, 150);
        private static readonly Drawing.Color ZebraColor = Drawing.Color.FromArgb(246, 247, 249);

        public NewScheduleTypeForm(string excelPath, List<string> sheetNames, string configDirectory)
        {
            _excelPath = excelPath;
            _sheetNames = sheetNames;
            _configDirectory = configDirectory;
            AutoScaleMode = WinForms.AutoScaleMode.Font;
            Font = AppFont;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "New Schedule Type";
            Width = 920;
            Height = 680;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            BackColor = Drawing.Color.White;

            var titleLabel = new WinForms.Label
            {
                Text = "Build a new schedule type from an Excel sheet",
                Left = 16,
                Top = 12,
                Width = 500,
                Font = AppFontBold,
                ForeColor = AccentColor
            };

            var sheetLabel = new WinForms.Label { Text = "Sheet:", Left = 16, Top = 44, Width = 45 };
            _sheetCombo = new WinForms.ComboBox
            {
                Left = 64,
                Top = 40,
                Width = 260,
                Font = AppFont,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };
            foreach (var name in _sheetNames)
                _sheetCombo.Items.Add(name);
            if (_sheetCombo.Items.Count > 0)
                _sheetCombo.SelectedIndex = 0;

            _analyzeButton = new WinForms.Button
            {
                Text = "Preview",
                Left = 336,
                Top = 38,
                Width = 90,
                Height = 28,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont
            };
            _analyzeButton.FlatAppearance.BorderColor = AccentColor;
            _analyzeButton.Click += (s, e) => RunAnalysis();

            var headerLabel = new WinForms.Label { Text = "Header row:", Left = 442, Top = 44, Width = 78 };
            _headerRowInput = new WinForms.NumericUpDown { Left = 522, Top = 40, Width = 55, Font = AppFont, Minimum = 1, Maximum = 100 };

            var dataLabel = new WinForms.Label { Text = "Data starts row:", Left = 592, Top = 44, Width = 100 };
            _dataStartRowInput = new WinForms.NumericUpDown { Left = 694, Top = 40, Width = 55, Font = AppFont, Minimum = 1, Maximum = 200 };

            var previewLabel = new WinForms.Label
            {
                Text = "Preview (first 15 rows):",
                Left = 16,
                Top = 76,
                Width = 250,
                Font = AppFontBold,
                ForeColor = Drawing.Color.FromArgb(90, 90, 90)
            };
            _previewGrid = new WinForms.DataGridView
            {
                Left = 16,
                Top = 98,
                Width = 880,
                Height = 220,
                Font = AppFont,
                BackgroundColor = Drawing.Color.White,
                BorderStyle = WinForms.BorderStyle.FixedSingle,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = { BackColor = ZebraColor },
                ColumnHeadersDefaultCellStyle = { Font = AppFontBold, BackColor = Drawing.Color.FromArgb(240, 242, 245) }
            };

            var mappingLabel = new WinForms.Label
            {
                Text = "Column mapping - edit \"Revit Field Name\" below (leave blank to skip a column):",
                Left = 16,
                Top = 330,
                Width = 600,
                Font = AppFontBold,
                ForeColor = Drawing.Color.FromArgb(90, 90, 90)
            };
            _mappingGrid = new WinForms.DataGridView
            {
                Left = 16,
                Top = 352,
                Width = 880,
                Height = 190,
                Font = AppFont,
                BackgroundColor = Drawing.Color.White,
                BorderStyle = WinForms.BorderStyle.FixedSingle,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AlternatingRowsDefaultCellStyle = { BackColor = ZebraColor },
                ColumnHeadersDefaultCellStyle = { Font = AppFontBold, BackColor = Drawing.Color.FromArgb(240, 242, 245) }
            };
            _mappingGrid.Columns.Add("ExcelColumn", "Excel Column");
            _mappingGrid.Columns.Add("HeaderText", "Detected Header Text");
            _mappingGrid.Columns.Add("RevitFieldName", "Revit Field Name");
            _mappingGrid.Columns["ExcelColumn"].ReadOnly = true;
            _mappingGrid.Columns["HeaderText"].ReadOnly = true;

            var nameLabel = new WinForms.Label
            {
                Text = "Schedule type name:",
                Left = 16,
                Top = 558,
                Width = 140,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left
            };
            _scheduleNameBox = new WinForms.TextBox
            {
                Left = 160,
                Top = 554,
                Width = 320,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left
            };

            _progressBar = new WinForms.ProgressBar
            {
                Left = 16,
                Top = 592,
                Width = 880,
                Height = 10,
                Style = WinForms.ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Visible = false,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right
            };

            _saveButton = new WinForms.Button
            {
                Text = "Save Schedule Type",
                Left = 646,
                Top = 612,
                Width = 150,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Drawing.Color.White,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            _saveButton.FlatAppearance.BorderSize = 0;
            _saveButton.Click += (s, e) => SaveConfig();

            _cancelButton = new WinForms.Button
            {
                Text = "Cancel",
                Left = 804,
                Top = 612,
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
            Controls.Add(_analyzeButton);
            Controls.Add(headerLabel);
            Controls.Add(_headerRowInput);
            Controls.Add(dataLabel);
            Controls.Add(_dataStartRowInput);
            Controls.Add(previewLabel);
            Controls.Add(_previewGrid);
            Controls.Add(mappingLabel);
            Controls.Add(_mappingGrid);
            Controls.Add(nameLabel);
            Controls.Add(_scheduleNameBox);
            Controls.Add(_progressBar);
            Controls.Add(_saveButton);
            Controls.Add(_cancelButton);

            if (_sheetCombo.Items.Count > 0)
                RunAnalysis();
        }

        private void RunAnalysis()
        {
            if (_sheetCombo.SelectedItem == null) return;
            string sheetName = _sheetCombo.SelectedItem.ToString();

            _progressBar.Visible = true;
            _analyzeButton.Enabled = false;
            WinForms.Application.DoEvents();

            try
            {
                var analyzer = new SheetAnalyzer();
                _currentPreview = analyzer.AnalyzeSheet(_excelPath, sheetName);

                _headerRowInput.Value = _currentPreview.GuessedHeaderRow;
                _dataStartRowInput.Value = _currentPreview.GuessedDataStartRow;

                PopulatePreviewGrid();
                PopulateMappingGrid();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(this, $"Could not analyze sheet.\n\nDetails: {ex.Message}",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
            finally
            {
                _progressBar.Visible = false;
                _analyzeButton.Enabled = true;
            }
        }

        private void PopulatePreviewGrid()
        {
            _previewGrid.Columns.Clear();
            _previewGrid.Rows.Clear();

            foreach (var letter in _currentPreview.ColumnLetters)
                _previewGrid.Columns.Add(letter, letter);

            foreach (var row in _currentPreview.Rows)
                _previewGrid.Rows.Add(row.ToArray());
        }

        private void PopulateMappingGrid()
        {
            _mappingGrid.Rows.Clear();

            int headerRowIndex = (int)_headerRowInput.Value - 1;
            if (headerRowIndex < 0 || headerRowIndex >= _currentPreview.Rows.Count)
                return;

            var headerRowValues = _currentPreview.Rows[headerRowIndex];

            for (int c = 0; c < _currentPreview.ColumnLetters.Count; c++)
            {
                string headerText = headerRowValues[c];
                if (string.IsNullOrWhiteSpace(headerText))
                    continue;

                _mappingGrid.Rows.Add(_currentPreview.ColumnLetters[c], headerText, headerText);
            }
        }

        private void SaveConfig()
        {
            if (string.IsNullOrWhiteSpace(_scheduleNameBox.Text))
            {
                WinForms.MessageBox.Show(this, "Enter a name for this schedule type first.", "Sheet2Schedule",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            var columns = new List<ColumnMapping>();
            foreach (WinForms.DataGridViewRow row in _mappingGrid.Rows)
            {
                if (row.IsNewRow) continue;

                string excelCol = row.Cells["ExcelColumn"].Value?.ToString();
                string revitField = row.Cells["RevitFieldName"].Value?.ToString();

                if (string.IsNullOrWhiteSpace(revitField)) continue;

                columns.Add(new ColumnMapping
                {
                    ExcelColumn = excelCol,
                    RevitFieldName = revitField
                });
            }

            if (columns.Count == 0)
            {
                WinForms.MessageBox.Show(this, "At least one column needs a Revit Field Name.", "Sheet2Schedule",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            var definition = new EquipmentScheduleDefinition
            {
                ScheduleName = _scheduleNameBox.Text.Trim(),
                SheetName = _sheetCombo.SelectedItem.ToString(),
                HeaderRow = (int)_headerRowInput.Value,
                DataStartRow = (int)_dataStartRowInput.Value,
                Columns = columns
            };

            _progressBar.Visible = true;
            _saveButton.Enabled = false;
            WinForms.Application.DoEvents();

            try
            {
                string savedPath = ConfigLoader.Save(definition, _configDirectory);
                WinForms.MessageBox.Show(this, $"Schedule type saved.\n\n{Path.GetFileName(savedPath)}\n\nIt will now appear in the Import Schedule picker for any file containing a \"{definition.SheetName}\" sheet.",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);

                DialogResult = WinForms.DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(this, $"Could not save config.\n\nDetails: {ex.Message}",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
            finally
            {
                _progressBar.Visible = false;
                _saveButton.Enabled = true;
            }
        }
    }
}