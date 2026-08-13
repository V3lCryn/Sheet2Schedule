using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Sheet2Schedule.Commands;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// Single-entry-point hub for Sheet2Schedule. Replaces separate ribbon buttons for
    /// Import by Range, Manage Data Links, and Import Log with one launcher. Stays open
    /// after each action so the user can chain actions (e.g. import, then immediately
    /// check the log) without re-clicking the ribbon each time.
    /// </summary>
    public class HubForm : Form
    {
        // Brand color pulled from the Sheet2Schedule logo (Revit blue).
        private static readonly Color AccentColor = Color.FromArgb(24, 90, 189);
        private static readonly Color BodyText = Color.FromArgb(45, 45, 48);
        private static readonly Color SubtleText = Color.FromArgb(120, 120, 125);
        private static readonly Color CardBorder = Color.FromArgb(225, 227, 230);

        private readonly Document _doc;

        public HubForm(Document doc)
        {
            _doc = doc;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Sheet2Schedule";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            Padding = new Padding(0);
            ClientSize = new Size(340, 340);

            SetWindowIcon();

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = Color.White
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            root.Controls.Add(BuildHeader(), 0, 0);

            var actionsPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 16, 0, 0)
            };
            for (int i = 0; i < 3; i++)
                actionsPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            actionsPanel.Controls.Add(BuildActionCard(
                "Import by Range",
                "Bring an Excel range into a new Revit schedule.",
                () => ImportByRange.Run(_doc)), 0, 0);

            actionsPanel.Controls.Add(BuildActionCard(
                "Manage Data Links",
                "Review and reload the Excel sources behind existing schedules.",
                () => ManageDataLinks.Run(_doc)), 0, 1);

            actionsPanel.Controls.Add(BuildActionCard(
                "Import Log",
                "See the history of every import and reload on this project.",
                () => ViewImportLog.Run(_doc)), 0, 2);

            root.Controls.Add(actionsPanel, 0, 1);

            var closeBtn = new Button
            {
                Text = "Close",
                AutoSize = false,
                Width = 90,
                Height = 30,
                Anchor = AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = SubtleText,
                Margin = new Padding(0, 12, 0, 0),
                DialogResult = DialogResult.Cancel
            };
            closeBtn.FlatAppearance.BorderColor = CardBorder;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 247);

            var footer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true
            };
            footer.Controls.Add(closeBtn);

            root.Controls.Add(footer, 0, 2);

            Controls.Add(root);
            CancelButton = closeBtn;
            AcceptButton = null;
        }

        private void SetWindowIcon()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string iconPath = Path.Combine(assemblyDir, "Resources", "logo.ico");
                if (File.Exists(iconPath))
                    Icon = new Icon(iconPath);
            }
            catch
            {
                // Missing icon shouldn't block the tool from opening.
            }
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string logoPath = Path.Combine(assemblyDir, "Resources", "logo_48.png");

            if (File.Exists(logoPath))
            {
                var logo = new PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Width = 36,
                    Height = 36,
                    Margin = new Padding(0, 0, 12, 0)
                };
                panel.Controls.Add(logo, 0, 0);
            }

            var titleStack = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2
            };
            titleStack.Controls.Add(new Label
            {
                Text = "Sheet2Schedule",
                Font = new Font("Segoe UI Semibold", 13F, FontStyle.Bold),
                ForeColor = BodyText,
                AutoSize = true
            }, 0, 0);
            titleStack.Controls.Add(new Label
            {
                Text = "Excel to Revit schedule import",
                Font = new Font("Segoe UI", 8.5F),
                ForeColor = SubtleText,
                AutoSize = true
            }, 0, 1);

            panel.Controls.Add(titleStack, 1, 0);
            return panel;
        }

        /// <summary>
        /// One clickable "card" per action: a bold title, a short description, and a
        /// left accent bar that lights up on hover for clear affordance.
        /// </summary>
        private Control BuildActionCard(string title, string description, Action onClick)
        {
            var card = new Panel
            {
                Width = 268,
                Height = 64,
                Margin = new Padding(0, 0, 0, 10),
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            card.Paint += (s, e) =>
            {
                using (var borderPen = new Pen(CardBorder))
                {
                    e.Graphics.DrawRectangle(borderPen, 0, 0, card.Width - 1, card.Height - 1);
                }
            };

            var accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(230, 232, 235)
            };

            var textPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(14, 10, 12, 10)
            };
            var titleLabel = new Label
            {
                Text = title,
                Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold),
                ForeColor = BodyText,
                AutoSize = true
            };
            var descLabel = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 8.25F),
                ForeColor = SubtleText,
                AutoSize = true,
                MaximumSize = new Size(230, 0)
            };
            textPanel.Controls.Add(titleLabel, 0, 0);
            textPanel.Controls.Add(descLabel, 0, 1);

            card.Controls.Add(textPanel);
            card.Controls.Add(accentBar);

            EventHandler enter = (s, e) =>
            {
                accentBar.BackColor = AccentColor;
                card.BackColor = Color.FromArgb(247, 249, 252);
                textPanel.BackColor = card.BackColor;
            };
            EventHandler leave = (s, e) =>
            {
                accentBar.BackColor = Color.FromArgb(230, 232, 235);
                card.BackColor = Color.White;
                textPanel.BackColor = Color.White;
            };
            EventHandler click = (s, e) => onClick();

            foreach (Control c in new Control[] { card, textPanel, titleLabel, descLabel })
            {
                c.MouseEnter += enter;
                c.MouseLeave += leave;
                c.Click += click;
                c.Cursor = Cursors.Hand;
            }

            return card;
        }
    }
}
