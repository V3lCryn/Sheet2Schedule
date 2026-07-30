using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
using Autodesk.Revit.DB;
using Sheet2Schedule.Services;

namespace Sheet2Schedule.UI
{
    public class ManageDataLinksForm : WinForms.Form
    {
        private readonly Document _doc;
        private WinForms.ListView _listView;
        private WinForms.Button _reloadButton;
        private WinForms.Button _reloadFromButton;
        private WinForms.Button _closeButton;

        private List<ViewSchedule> _schedules;

        private static readonly Drawing.Font AppFont = new Drawing.Font("Segoe UI", 9.5f);
        private static readonly Drawing.Color AccentColor = Drawing.Color.FromArgb(47, 84, 150); // brand accent blue

        public ManageDataLinksForm(Document doc, List<ViewSchedule> schedules)
        {
            _doc = doc;
            _schedules = schedules;
            AutoScaleMode = WinForms.AutoScaleMode.Font;
            Font = AppFont;
            BuildUi();
            PopulateList();
        }

        private void BuildUi()
        {
            Text = "Manage Data Links";
            Width = 780;
            Height = 420;
            StartPosition = WinForms.FormStartPosition.CenterScreen;
            MinimizeBox = false;
            MaximizeBox = false;
            Padding = new WinForms.Padding(16);
            BackColor = Drawing.Color.White;

            var titleLabel = new WinForms.Label
            {
                Text = "Schedules created by Sheet2Schedule",
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
                Width = 730,
                Height = 260,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Top | WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Left | WinForms.AnchorStyles.Right
            };
            _listView.Columns.Add("Schedule Name", 210);
            _listView.Columns.Add("Source Excel File", 290);
            _listView.Columns.Add("Last Updated (UTC)", 120);
            _listView.Columns.Add("Status", 180);

            _reloadButton = new WinForms.Button
            {
                Text = "Reload",
                Left = 16,
                Top = 316,
                Width = 110,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                BackColor = AccentColor,
                ForeColor = Drawing.Color.White,
                Font = AppFont
            };
            _reloadButton.FlatAppearance.BorderSize = 0;
            _reloadButton.Click += (s, e) => Reload(useNewFile: false);

            _reloadFromButton = new WinForms.Button
            {
                Text = "Reload From...",
                Left = 134,
                Top = 316,
                Width = 130,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont
            };
            _reloadFromButton.FlatAppearance.BorderColor = AccentColor;
            _reloadFromButton.Click += (s, e) => Reload(useNewFile: true);

            _closeButton = new WinForms.Button
            {
                Text = "Close",
                Left = 654,
                Top = 316,
                Width = 92,
                Height = 32,
                FlatStyle = WinForms.FlatStyle.Flat,
                Font = AppFont,
                Anchor = WinForms.AnchorStyles.Bottom | WinForms.AnchorStyles.Right
            };
            _closeButton.Click += (s, e) => Close();

            Controls.Add(titleLabel);
            Controls.Add(_listView);
            Controls.Add(_reloadButton);
            Controls.Add(_reloadFromButton);
            Controls.Add(_closeButton);
        }

        private void PopulateList()
        {
            _listView.Items.Clear();
            foreach (var schedule in _schedules)
            {
                var info = LinkStorage.GetLinkInfo(schedule);
                if (info == null) continue;

                string lastUpdatedDisplay = info.LastUpdatedUtc;
                DateTime? lastUpdatedUtc = null;
                if (DateTime.TryParse(info.LastUpdatedUtc, out var parsed))
                {
                    lastUpdatedUtc = parsed;
                    lastUpdatedDisplay = parsed.ToString("yyyy-MM-dd HH:mm");
                }

                string status = "Unknown";
                bool isStale = false;
                bool missing = false;

                if (info.SourceExcelPath != null && File.Exists(info.SourceExcelPath) && lastUpdatedUtc.HasValue)
                {
                    DateTime fileModifiedUtc = File.GetLastWriteTimeUtc(info.SourceExcelPath);
                    isStale = fileModifiedUtc > lastUpdatedUtc.Value;
                    status = isStale ? "Excel updated - reload recommended" : "Up to date";
                }
                else if (info.SourceExcelPath == null || !File.Exists(info.SourceExcelPath))
                {
                    status = "Source file not found";
                    missing = true;
                }

                var item = new WinForms.ListViewItem(schedule.Name);
                item.SubItems.Add(info.SourceExcelPath ?? "(unknown)");
                item.SubItems.Add(lastUpdatedDisplay ?? "");
                item.SubItems.Add(status);
                item.Tag = schedule;

                if (isStale)
                    item.BackColor = Drawing.Color.FromArgb(255, 248, 220);
                else if (missing)
                    item.BackColor = Drawing.Color.FromArgb(253, 227, 227);
                else if (_listView.Items.Count % 2 == 1)
                    item.BackColor = Drawing.Color.FromArgb(246, 247, 249); // subtle zebra striping

                _listView.Items.Add(item);
            }
        }

        private void Reload(bool useNewFile)
        {
            if (_listView.SelectedItems.Count == 0)
            {
                WinForms.MessageBox.Show(this, "Select a schedule from the list first.", "Sheet2Schedule",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);
                return;
            }

            var schedule = (ViewSchedule)_listView.SelectedItems[0].Tag;
            var info = LinkStorage.GetLinkInfo(schedule);
            if (info == null)
            {
                WinForms.MessageBox.Show(this, "This schedule has no stored link info.", "Sheet2Schedule",
                    WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Warning);
                return;
            }

            string excelPath = info.SourceExcelPath;

            if (useNewFile)
            {
                using (var dialog = new WinForms.OpenFileDialog
                {
                    Title = "Select the updated Excel workbook",
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx"
                })
                {
                    if (dialog.ShowDialog(this) != WinForms.DialogResult.OK)
                        return;

                    excelPath = dialog.FileName;
                }
            }

            if (!File.Exists(excelPath))
            {
                WinForms.MessageBox.Show(this, $"File not found:\n{excelPath}\n\nUse \"Reload From...\" to point this schedule at a valid file.",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(info.ConfigFileName))
                {
                    WinForms.MessageBox.Show(this, "This schedule has no associated config file and cannot be reloaded.",
                        "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
                    return;
                }

                string assemblyDir = Path.GetDirectoryName(typeof(ManageDataLinksForm).Assembly.Location);
                string configPath = Path.Combine(assemblyDir, "Config", info.ConfigFileName);
                var definition = ConfigLoader.Load(configPath);

                var reader = new ExcelReader();
                var rows = reader.ReadRows(excelPath, definition);

                using (Transaction t = new Transaction(_doc, "Reload Equipment Schedule (Data Sheet)"))
                {
                    t.Start();
                    try
                    {
                        var writer = new RevitDataWriter();
                        writer.RefreshScheduleTable(schedule, definition, rows);
                        LinkStorage.SetLinkInfo(schedule, excelPath, info.ConfigFileName);

                        ImportLogService.AddEntry(_doc, new Sheet2Schedule.Models.LogEntry
                        {
                            TimestampUtc = DateTime.UtcNow,
                            Action = useNewFile ? "Reload From" : "Reload",
                            ScheduleName = schedule.Name,
                            SourceExcelPath = excelPath,
                            RowCount = rows.Count,
                            UserName = _doc.Application.Username
                        });

                        t.Commit();
                    }
                    catch
                    {
                        if (t.HasStarted() && !t.HasEnded())
                            t.RollBack();
                        throw;
                    }
                }

                WinForms.MessageBox.Show(this, $"Reloaded successfully.\n\nRows imported: {rows.Count}",
                    "Sheet2Schedule", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Information);

                PopulateList();
            }
            catch (Exception ex)
            {
                WinForms.MessageBox.Show(this, $"Reload failed and was rolled back. No changes were made.\n\nDetails: {ex.Message}",
                    "Sheet2Schedule - Error", WinForms.MessageBoxButtons.OK, WinForms.MessageBoxIcon.Error);
            }
        }
    }
}