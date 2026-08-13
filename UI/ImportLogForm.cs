using System;
using System.IO;
using System.Linq;
using System.Reflection;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using Autodesk.Revit.DB;
using Sheet2Schedule.Services;

namespace Sheet2Schedule.UI
{
    /// <summary>
    /// Read-only viewer for the import/reload audit log stored on this project's
    /// ProjectInformation element.
    /// </summary>
    public class ImportLogForm : WinForms.Form
    {
        private WinForms.ListView _listView;
        private WinForms.Button _closeButton;

        private static readonly Drawing.Font AppFont = new Drawing.Font("Segoe UI", 9.5f);
        private static readonly Drawing.Color AccentColor = Drawing.Color.FromArgb(47, 84, 150);

        public ImportLogForm(Document doc)
        {
            AutoScaleMode = WinForms.AutoScaleMode.Font;
            Font = AppFont;
            SetStyle(WinForms.ControlStyles.AllPaintingInWmPaint | WinForms.ControlStyles.UserPaint | WinForms.ControlStyles.DoubleBuffer, true);
            BuildUi();
            PopulateList(doc);
        }

        private void SetWindowIcon()
        {
            try
            {
                string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string iconPath = Path.Combine(assemblyDir, "Resources", "logo.ico");
                if (File.Exists(iconPath))
                    Icon = new Drawing.Icon(iconPath);
            }
            catch
            {
                // Missing icon shouldn't block the tool from opening.
            }
        }

        private void BuildUi()
        {
            Text = "Import Log";
            ClientSize = new Drawing.Size(840, 440);
            MinimumSize = new Drawing.Size(680, 360);
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            BackColor = Drawing.Color.White;

            SetWindowIcon();

            var titleLabel = new WinForms.Label
            {
                Text = "Import / reload history for this project",
                Left = 16,
                Top = 14,
                Width = 500,
                Font = new Drawing.Font(AppFont, Drawing.FontStyle.Bold),
                ForeColor = AccentColor
            };

            _listView = new WinForms.ListView
            {
                View = WinForms.View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = false,
                Left = 16,
                Top = 44,
                Width = 790,
                Height = 320,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right
            };
            _listView.Columns.Add("Date/Time (UTC)", 130);
            _listView.Columns.Add("Action", 90);
            _listView.Columns.Add("Schedule", 170);
            _listView.Columns.Add("Source File", 260);
            _listView.Columns.Add("Rows", 50);
            _listView.Columns.Add("User", 80);

            _closeButton = new WinForms.Button
            {
                Text = "Close",
                Left = 714,
                Top = 372,
                Width = 92,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            _closeButton.Click += (s, e) => Close();

            Controls.Add(titleLabel);
            Controls.Add(_listView);
            Controls.Add(_closeButton);
        }

        private void PopulateList(Document doc)
        {
            var entries = ImportLogService.GetEntries(doc)
                .OrderByDescending(e => e.TimestampUtc)
                .ToList();

            if (entries.Count == 0)
            {
                var emptyItem = new WinForms.ListViewItem("No import activity recorded yet for this project.");
                _listView.Items.Add(emptyItem);
                return;
            }

            int i = 0;
            foreach (var entry in entries)
            {
                var item = new WinForms.ListViewItem(entry.TimestampUtc.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(entry.Action);
                item.SubItems.Add(entry.ScheduleName);
                item.SubItems.Add(entry.SourceExcelPath);
                item.SubItems.Add(entry.RowCount.ToString());
                item.SubItems.Add(entry.UserName);

                if (i % 2 == 1)
                    item.BackColor = Drawing.Color.FromArgb(246, 247, 249);

                _listView.Items.Add(item);
                i++;
            }
        }
    }
}