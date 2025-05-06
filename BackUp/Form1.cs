using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BackUp
{
    public partial class Form1 : Form
    {
        private Label _lblSource, _lblDestination, _lblInterval, _lblRetention, _lblStatus;
        private TextBox _txtSource, _txtDestination;
        private NumericUpDown _numInterval, _numRetention;
        private Button _btnBrowseSource, _btnBrowseDestination, _btnStart, _btnStop, _btnRestore;
        private BackupService _service;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            Text = "Backup Planner";
            Width = 520;
            Height = 320;

            _lblSource = new Label { Text = "Исходная папка:", Left = 20, Top = 20, AutoSize = true };
            Controls.Add(_lblSource);
            _txtSource = new TextBox { Left = 20, Top = 45, Width = 360 };
            Controls.Add(_txtSource);
            _btnBrowseSource = new Button { Text = "📁", Left = 390, Top = 43, Width = 30 };
            _btnBrowseSource.Click += BtnBrowseSource_Click;
            Controls.Add(_btnBrowseSource);

            _lblDestination = new Label { Text = "Целевая папка:", Left = 20, Top = 85, AutoSize = true };
            Controls.Add(_lblDestination);
            _txtDestination = new TextBox { Left = 20, Top = 110, Width = 360 };
            Controls.Add(_txtDestination);
            _btnBrowseDestination = new Button { Text = "📁", Left = 390, Top = 108, Width = 30 };
            _btnBrowseDestination.Click += BtnBrowseDestination_Click;
            Controls.Add(_btnBrowseDestination);

            _lblInterval = new Label { Text = "Интервал (мин):", Left = 20, Top = 150, AutoSize = true };
            Controls.Add(_lblInterval);
            _numInterval = new NumericUpDown { Left = 25, Top = 175, Width = 60, Minimum = 1, Maximum = 1440, Value = 60 };
            Controls.Add(_numInterval);

            _lblRetention = new Label { Text = "Срок хранения (дней):", Left = 200, Top = 150, AutoSize = true };
            Controls.Add(_lblRetention);
            _numRetention = new NumericUpDown { Left = 205, Top = 175, Width = 60, Minimum = 1, Maximum = 365, Value = 7 };
            Controls.Add(_numRetention);

            _lblStatus = new Label { Text = "Статус: Остановлен", Left = 20, Top = 215, AutoSize = true };
            Controls.Add(_lblStatus);

            _btnStart = new Button { Text = "Старт", Left = 20, Top = 240, Width = 100 };
            _btnStart.Click += BtnStart_Click;
            Controls.Add(_btnStart);

            _btnStop = new Button { Text = "Стоп", Left = 140, Top = 240, Width = 100, Enabled = false };
            _btnStop.Click += BtnStop_Click;
            Controls.Add(_btnStop);

            _btnRestore = new Button { Text = "Восстановить", Left = 260, Top = 240, Width = 120 };
            _btnRestore.Click += BtnRestore_Click;
            Controls.Add(_btnRestore);
        }

        private void BtnBrowseSource_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtSource.Text = dlg.SelectedPath;
        }

        private void BtnBrowseDestination_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                _txtDestination.Text = dlg.SelectedPath;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            var job = new BackupJob
            {
                SourcePath = _txtSource.Text,
                DestinationPath = _txtDestination.Text,
                IntervalMinutes = (int)_numInterval.Value,
                RetentionDays = (int)_numRetention.Value
            };

            _service = new BackupService(job);
            _service.Start();

            _lblStatus.Text = "Статус: Работает";
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;

            MessageBox.Show("Резервное копирование запущено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _service?.Stop();
            _lblStatus.Text = "Статус: Остановлен";
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;

            MessageBox.Show("Резервное копирование остановлено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            var backupDirs = Directory.GetDirectories(_txtDestination.Text, "Backup_*")
                                      .OrderByDescending(d => d)
                                      .ToArray();

            if (backupDirs.Length == 0)
            {
                MessageBox.Show("Бэкапы не найдены.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var dlg = new Form { Text = "Выберите точку восстановления", Width = 450, Height = 300 };
            var list = new ListBox { Dock = DockStyle.Top, Height = 200 };
            list.Items.AddRange(backupDirs);
            dlg.Controls.Add(list);

            var btnOK = new Button { Text = "Восстановить", Dock = DockStyle.Bottom, Height = 30 };
            btnOK.Click += (_, _) => dlg.DialogResult = DialogResult.OK;
            dlg.Controls.Add(btnOK);

            if (dlg.ShowDialog() == DialogResult.OK && list.SelectedItem is string selectedBackup)
            {
                foreach (var srcPath in Directory.GetFiles(selectedBackup))
                {
                    var fileName = Path.GetFileName(srcPath);
                    var dstPath = Path.Combine(_txtSource.Text, fileName);
                    File.Copy(srcPath, dstPath, overwrite: true);
                }

                MessageBox.Show("Файлы восстановлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}