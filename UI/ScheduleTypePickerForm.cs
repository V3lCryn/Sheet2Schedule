using System.Collections.Generic;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// Simple picker shown after peeking at an Excel file's sheets, letting the user
    /// choose which matching schedule type to import.
    /// </summary>
    public class ScheduleTypePickerForm : WinForms.Form
    {
        public string SelectedConfigFileName { get; private set; }

        private WinForms.ComboBox _comboBox;
        private List<(string FileName, string DisplayName)> _comboBoxConfigs;

        private static readonly Drawing.Font AppFont = new Drawing.Font("Segoe UI", 9.5f);
        private static readonly Drawing.Color AccentColor = Drawing.Color.FromArgb(47, 84, 150);

        public ScheduleTypePickerForm(List<(string FileName, string DisplayName)> availableConfigs)
        {
            AutoScaleMode = WinForms.AutoScaleMode.Font;
            Font = AppFont;

            Text = "Choose Schedule Type";
            Width = 420;
            Height = 170;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
            BackColor = Drawing.Color.White;

            var titleLabel = new WinForms.Label
            {
                Text = "This file contains more than one matching schedule type.",
                Left = 16,
                Top = 16,
                Width = 380,
                Height = 20,
                Font = new Drawing.Font(AppFont, Drawing.FontStyle.Bold),
                ForeColor = AccentColor
            };

            var label = new WinForms.Label { Text = "Schedule type:", Left = 16, Top = 50, Width = 100 };

            _comboBox = new WinForms.ComboBox
            {
                Left = 16,
                Top = 74,
                Width = 370,
                Font = AppFont,
                DropDownStyle = WinForms.ComboBoxStyle.DropDownList
            };
            foreach (var config in availableConfigs)
                _comboBox.Items.Add(config.DisplayName);

            if (_comboBox.Items.Count > 0)
                _comboBox.SelectedIndex = 0;

            _comboBoxConfigs = availableConfigs;

            var okButton = new WinForms.Button
            {
                Text = "OK",
                Left = 222,
                Top = 108,
                Width = 80,
                Height = 30,
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Drawing.Color.White,
                Font = AppFont
            };
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Click += (s, e) =>
            {
                if (_comboBox.SelectedIndex >= 0)
                    SelectedConfigFileName = _comboBoxConfigs[_comboBox.SelectedIndex].FileName;

                DialogResult = WinForms.DialogResult.OK;
                Close();
            };

            var cancelButton = new WinForms.Button
            {
                Text = "Cancel",
                Left = 306,
                Top = 108,
                Width = 80,
                Height = 30,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = WinForms.DialogResult.Cancel;
                Close();
            };

            Controls.Add(titleLabel);
            Controls.Add(label);
            Controls.Add(_comboBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);

            AcceptButton = okButton;
            CancelButton = cancelButton;
        }
    }
}