using Timer = System.Threading.Timer;

namespace BackUp
{
    public class BackupService : IDisposable
    {
        private readonly BackupJob _job;
        private readonly CancellationTokenSource _cts = new();
        private Timer _timer;

        public BackupService(BackupJob job)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));
            if (string.IsNullOrWhiteSpace(_job.SourcePath))
                throw new ArgumentException("SourcePath must be set", nameof(job.SourcePath));
            if (string.IsNullOrWhiteSpace(_job.DestinationPath))
                throw new ArgumentException("DestinationPath must be set", nameof(job.DestinationPath));
        }

        public void Start()
        {
            _timer = new Timer(async _ => await RunAsync(_cts.Token),
                                null,
                                dueTime:    0,
                                period:     _job.IntervalMinutes * 60_000);
        }

        public void Stop()
        {
            _cts.Cancel();
            _timer?.Dispose();
        }

        private async Task RunAsync(CancellationToken token)
        {
            try
            {
                CleanupOldBackups();

                var lastBackup = GetLatestBackupFolder();

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var newFolder = Path.Combine(_job.DestinationPath, $"Backup_{timestamp}");
                Directory.CreateDirectory(newFolder);

                foreach (var srcPath in Directory.GetFiles(_job.SourcePath))
                {
                    token.ThrowIfCancellationRequested();

                    var fileName   = Path.GetFileName(srcPath);
                    var dstPath    = Path.Combine(newFolder, fileName);
                    var srcWrite   = File.GetLastWriteTimeUtc(srcPath);

                    if (lastBackup != null)
                    {
                        var prevPath  = Path.Combine(lastBackup, fileName);
                        if (File.Exists(prevPath))
                        {
                            var prevWrite = File.GetLastWriteTimeUtc(prevPath);
                            if (prevWrite >= srcWrite)
                                continue;
                        }
                    }

                    await using var src = new FileStream(
                        srcPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
                    await using var dst = new FileStream(
                        dstPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
                    await src.CopyToAsync(dst, 81920, token);
                }

                Console.WriteLine($"[BackupService] Backup completed at {DateTime.Now:T}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[BackupService] Backup canceled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BackupService] Error: {ex.Message}");
            }
        }

        private void CleanupOldBackups()
        {
            var cutoff = DateTime.UtcNow.AddDays(-_job.RetentionDays);
            foreach (var dir in Directory.GetDirectories(_job.DestinationPath, "Backup_*"))
            {
                var name = Path.GetFileName(dir);
                if (DateTime.TryParseExact(
                        name.Substring(7),
                        "yyyyMMdd_HHmmss",
                        null,
                        System.Globalization.DateTimeStyles.AssumeUniversal,
                        out var dt))
                {
                    if (dt.ToUniversalTime() < cutoff)
                        Directory.Delete(dir, recursive: true);
                }
            }
        }

        private string GetLatestBackupFolder()
        {
            var dirs = Directory.GetDirectories(_job.DestinationPath, "Backup_*");
            return dirs
                .Select(d => new { Path = d, Time = ParseTimestamp(d) })
                .Where(x => x.Time != null)
                .OrderByDescending(x => x.Time)
                .FirstOrDefault()
                ?.Path;
        }

        private DateTime? ParseTimestamp(string dir)
        {
            var name = Path.GetFileName(dir);
            if (DateTime.TryParseExact(
                    name.Substring(7),
                    "yyyyMMdd_HHmmss",
                    null,
                    System.Globalization.DateTimeStyles.AssumeUniversal,
                    out var dt))
                return dt;
            return null;
        }

        public void Dispose() => Stop();
    }
}