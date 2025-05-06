namespace BackUp
{
    public class BackupJob
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public int IntervalMinutes { get; set; }
        public int RetentionDays { get; set; }
    }
}