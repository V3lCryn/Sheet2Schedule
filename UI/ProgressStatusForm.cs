using WinForms = System.Windows.Forms;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// A simple non-modal status window with a marquee (indeterminate) progress bar,
    /// shown during longer operations so the user sees active feedback rather than
    /// wondering if Revit has frozen. Marquee style is used rather than a percentage
    /// bar because we have no reliable way to measure "% done" while ClosedXML parses
    /// a workbook - showing a fake percentage would be less honest than an
    /// indeterminate spinner.
    /// </summary>
    public class ProgressStatusForm : WinForms.Form
    {
        private WinForms.Label _statusLabel;
        private WinForms.ProgressBar _progressBar;

        public ProgressStatusForm(string initialMessage)
        {
            Text = "Sheet2Schedule";
            Width = 380;
            Height = 130;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            FormBorderStyle = WinForms.FormBorderStyle.FixedDialog;
            ControlBox = false;
            MinimizeBox = false;
            MaximizeBox = false;
            TopMost = true;

            _statusLabel = new WinForms.Label
            {
                Text = initialMessage,
                Left = 20,
                Top = 20,
                Width = 340,
                Height = 30,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };

            _progressBar = new WinForms.ProgressBar
            {
                Left = 20,
                Top = 55,
                Width = 340,
                Height = 20,
                Style = WinForms.ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30
            };

            Controls.Add(_statusLabel);
            Controls.Add(_progressBar);
        }

        public void UpdateStatus(string message)
        {
            _statusLabel.Text = message;
            Refresh();
            WinForms.Application.DoEvents();
        }
    }
}