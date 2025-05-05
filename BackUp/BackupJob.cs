namespace BackUp
{
    /// <summary>
    /// Описывает задачу резервного копирования.
    /// </summary>
    public class BackupJob
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        /// <summary>
        /// Интервал между полными бэкапами (минуты).
        /// </summary>
        public int IntervalMinutes { get; set; }
        /// <summary>
        /// Удалять бэкапы старше этого количества дней.
        /// </summary>
        public int RetentionDays { get; set; }
    }
}