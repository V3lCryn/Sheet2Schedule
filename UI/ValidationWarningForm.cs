using System.Collections.Generic;
using WinForms = System.Windows.Forms;
using Sheet2Schedule.Models;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// Shown before writing to Revit if RowValidator found any issues. Lets the user
    /// see exactly what's being skipped and choose whether to proceed with the
    /// remaining valid rows, or cancel entirely to go fix the Excel file first.
    /// </summary>
    public class ValidationWarningForm : WinForms.Form
    {
        public bool UserChoseProceed { get; private set; }

        public ValidationWarningForm(List<ValidationIssue> issues, int validRowCount)
        {
            Text = "Sheet2Schedule - Data Issues Found";
            Width = 560;
            Height = 400;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;

            var label = new WinForms.Label
            {
                Text = $"{issues.Count} row(s) had issues and will be skipped.\n{validRowCount} row(s) will be imported normally.",
                Left = 12,
                Top = 12,
                Width = 520,
                Height = 40
            };

            var listBox = new WinForms.ListBox
            {
                Left = 12,
                Top = 56,
                Width = 520,
                Height = 250,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right
            };
            foreach (var issue in issues)
                listBox.Items.Add($"Row {issue.RowNumber}: {issue.Message}");

            var proceedButton = new WinForms.Button
            {
                Text = "Proceed (skip flagged rows)",
                Left = 12,
                Top = 318,
                Width = 220,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left
            };
            proceedButton.Click += (s, e) =>
            {
                UserChoseProceed = true;
                Close();
            };

            var cancelButton = new WinForms.Button
            {
                Text = "Cancel Import",
                Left = 400,
                Top = 318,
                Width = 130,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            cancelButton.Click += (s, e) =>
            {
                UserChoseProceed = false;
                Close();
            };

            Controls.Add(label);
            Controls.Add(listBox);
            Controls.Add(proceedButton);
            Controls.Add(cancelButton);
        }
    }
}