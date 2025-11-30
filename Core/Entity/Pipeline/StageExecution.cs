using Core.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Core.Entity.Pipeline
{
    public class StageExecution : BaseEntity
    {
        [Required]
        public int ContentPipelineRunId { get; set; }
        // Navigation
        public ContentPipelineRun Run { get; set; } = null!;

        [Required]
        public int StageConfigId { get; set; }
        // Navigation: Hangi ayarlarla çalıştığını bilmemiz lazım
        public StageConfig StageConfig { get; set; } = null!;

        [Required]
        public StageStatus Status { get; set; } = StageStatus.Pending;

        public int RetryCount { get; set; } = 0;

        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }

        public int? DurationMs { get; set; }
        public int? CpuTimeMs { get; set; }

        // ==========================================
        // VERİ AMBARI (Data Warehouse)
        // ==========================================

        // GİRDİ: Bu aşama başlarken hafızada ne vardı? (Snapshot)
        // Render aşamasında burası script, image, audio path'leriyle dolu olacak.
        public string? InputJson { get; set; }

        // ÇIKTI: Bu aşama ne üretti?
        public string? OutputJson { get; set; }

        public string? Error { get; set; }

        // LOGLAR: JSON Array olarak tutuyoruz.
        public string? LogsJson { get; set; }

        // ==========================================
        // HELPER METHODS (Kodun temizliği için)
        // ==========================================

        private List<string> _runtimeLogs = new();

        /// <summary>
        /// Executor içinden kolayca log eklemek için.
        /// EF Core bunu veritabanına kaydederken LogsJson'a çevirecek.
        /// </summary>
        public void AddLog(string message)
        {
            var logEntry = $"{DateTime.UtcNow:HH:mm:ss} - {message}";
            _runtimeLogs.Add(logEntry);

            // Basit bir serialization (Production'da daha optimize yapılabilir)
            LogsJson = JsonSerializer.Serialize(_runtimeLogs);
        }

        public void MarkStarted()
        {
            Status = StageStatus.Running;
            StartedAt = DateTime.UtcNow;
            AddLog("Stage Started.");
        }

        public void MarkCompleted(object? outputData = null)
        {
            Status = StageStatus.Completed;
            FinishedAt = DateTime.UtcNow;

            if (StartedAt.HasValue)
            {
                DurationMs = (int)(FinishedAt.Value - StartedAt.Value).TotalMilliseconds;
            }

            if (outputData != null)
            {
                // Çıktıyı JSON yapıp sakla
                OutputJson = JsonSerializer.Serialize(outputData);
            }

            AddLog($"Stage Completed in {DurationMs}ms.");
        }

        public void MarkFailed(string errorMessage)
        {
            Status = StageStatus.Failed;
            FinishedAt = DateTime.UtcNow;
            Error = errorMessage;
            AddLog($"FATAL ERROR: {errorMessage}");
        }
    }
}
