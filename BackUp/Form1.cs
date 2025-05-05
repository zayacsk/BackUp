namespace BackUp
{
    public partial class Form1 : Form
    {
        private Label lblSource, lblDestination, lblInterval, lblRetention, lblStatus;
        private TextBox txtSource, txtDestination;
        private NumericUpDown numInterval, numRetention;
        private Button btnBrowseSource, btnBrowseDestination, btnStart, btnStop, btnRestore;

        private BackupService _service;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            Text   = "Backup Planner";
            Width  = 520;
            Height = 320;

            new ToolTip { AutoPopDelay = 5000, InitialDelay = 1000, ReshowDelay = 500, ShowAlways = true };

            // Source
            lblSource = new Label { Text = "Исходная папка:", Left = 20, Top = 20, AutoSize = true };
            Controls.Add(lblSource);
            txtSource = new TextBox { Left = 20, Top = 45, Width = 360 };
            Controls.Add(txtSource);
            btnBrowseSource = new Button { Text = "📁", Left = 390, Top = 43, Width = 30 };
            btnBrowseSource.Click += BtnBrowseSource_Click;
            Controls.Add(btnBrowseSource);

            // Destination
            lblDestination = new Label { Text = "Целевая папка:", Left = 20, Top = 85, AutoSize = true };
            Controls.Add(lblDestination);
            txtDestination = new TextBox { Left = 20, Top = 110, Width = 360 };
            Controls.Add(txtDestination);
            btnBrowseDestination = new Button { Text = "📁", Left = 390, Top = 108, Width = 30 };
            btnBrowseDestination.Click += BtnBrowseDestination_Click;
            Controls.Add(btnBrowseDestination);

            // Interval
            lblInterval = new Label { Text = "Интервал (мин):", Left = 20, Top = 150, AutoSize = true };
            Controls.Add(lblInterval);
            numInterval = new NumericUpDown { Left = 25, Top = 175, Width = 60, Minimum = 1, Maximum = 1440, Value = 60 };
            Controls.Add(numInterval);

            // Retention
            lblRetention = new Label { Text = "Срок хранения (дней):", Left = 200, Top = 150, AutoSize = true };
            Controls.Add(lblRetention);
            numRetention = new NumericUpDown { Left = 205, Top = 175, Width = 60, Minimum = 1, Maximum = 365, Value = 7 };
            Controls.Add(numRetention);

            // Status
            lblStatus = new Label { Text = "Статус: Остановлен", Left = 20, Top = 215, AutoSize = true };
            Controls.Add(lblStatus);

            // Start / Stop / Restore buttons
            btnStart = new Button { Text = "Старт", Left = 20, Top = 240, Width = 100 };
            btnStart.Click += BtnStart_Click;
            Controls.Add(btnStart);

            btnStop = new Button { Text = "Стоп", Left = 140, Top = 240, Width = 100, Enabled = false };
            btnStop.Click += BtnStop_Click;
            Controls.Add(btnStop);

            btnRestore = new Button { Text = "Восстановить", Left = 260, Top = 240, Width = 120 };
            btnRestore.Click += BtnRestore_Click;
            Controls.Add(btnRestore);
        }

        private void BtnBrowseSource_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                txtSource.Text = dlg.SelectedPath;
        }

        private void BtnBrowseDestination_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog();
            if (dlg.ShowDialog() == DialogResult.OK)
                txtDestination.Text = dlg.SelectedPath;
        }

        private void BtnStart_Click(object sender, EventArgs e)
        {
            var job = new BackupJob
            {
                SourcePath      = txtSource.Text,
                DestinationPath = txtDestination.Text,
                IntervalMinutes = (int)numInterval.Value,
                RetentionDays   = (int)numRetention.Value
            };

            _service = new BackupService(job);
            _service.Start();

            lblStatus.Text    = "Статус: Работает";
            btnStart.Enabled  = false;
            btnStop.Enabled   = true;
            MessageBox.Show("Резервное копирование запущено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _service?.Stop();
            lblStatus.Text    = "Статус: Остановлен";
            btnStart.Enabled  = true;
            btnStop.Enabled   = false;
            MessageBox.Show("Резервное копирование остановлено.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            // Получаем список папок бэкапов
            var backupDirs = Directory.GetDirectories(txtDestination.Text, "Backup_*")
                                      .OrderByDescending(d => d)
                                      .ToArray();

            if (backupDirs.Length == 0)
            {
                MessageBox.Show("Бэкапы не найдены.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Форма выбора точки восстановления
            using var dlg = new Form { Text = "Выберите точку восстановления", Width = 450, Height = 300 };
            var list = new ListBox { Dock = DockStyle.Top, Height = 200 };
            list.Items.AddRange(backupDirs);
            dlg.Controls.Add(list);

            var btnOK = new Button { Text = "Восстановить", Dock = DockStyle.Bottom, Height = 30 };
            btnOK.Click += (_, _) => dlg.DialogResult = DialogResult.OK;
            dlg.Controls.Add(btnOK);

            if (dlg.ShowDialog() == DialogResult.OK && list.SelectedItem is string selectedBackup)
            {
                // Восстанавливаем каждый файл из выбранного бэкапа
                foreach (var srcPath in Directory.GetFiles(selectedBackup))
                {
                    var fileName = Path.GetFileName(srcPath);
                    var dstPath  = Path.Combine(txtSource.Text, fileName);
                    File.Copy(srcPath, dstPath, overwrite: true);
                }

                MessageBox.Show("Файлы восстановлены.", "Готово", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}