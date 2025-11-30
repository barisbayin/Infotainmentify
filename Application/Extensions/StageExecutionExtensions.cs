using Core.Entity.Pipeline;
using Core.Enums;
using System.Text.Json;

namespace Application.Extensions
{
    public static class StageExecutionExtensions
    {
        // ------------------------------
        // LOG EKLEME
        // ------------------------------
        public static void AddLog(this StageExecution exec, string message)
        {
            List<string> logs;

            if (string.IsNullOrWhiteSpace(exec.LogsJson))
                logs = new();
            else
                logs = JsonSerializer.Deserialize<List<string>>(exec.LogsJson)
                       ?? new List<string>();

            logs.Add($"{DateTime.Now:O} - {message}");
            exec.LogsJson = JsonSerializer.Serialize(logs);
        }

        // ------------------------------
        // STATUS DEĞİŞTİRME
        // ------------------------------
        public static void SetStatus(this StageExecution exec, StageStatus status)
        {
            exec.Status = status;
            exec.AddLog($"Status changed to {status}");
        }

        // ------------------------------
        // BAŞLADI
        // ------------------------------
        public static void MarkStarted(this StageExecution exec)
        {
            exec.StartedAt = DateTime.Now;
            exec.Status = StageStatus.Running;
            exec.AddLog("Stage started");
        }

        // ------------------------------
        // BAŞARIYLA BİTTİ
        // ------------------------------
        public static void MarkCompleted(this StageExecution exec, object? output, int? cpuTimeMs = null)
        {
            exec.FinishedAt = DateTime.Now;
            exec.Status = StageStatus.Completed;

            // Süre hesaplama
            if (exec.StartedAt.HasValue)
            {
                exec.DurationMs = (int)(exec.FinishedAt.Value - exec.StartedAt.Value).TotalMilliseconds;
            }

            // CPU Time (opsiyonel)
            exec.CpuTimeMs = cpuTimeMs;

            // Output kaydı
            exec.OutputJson = JsonSerializer.Serialize(output);

            exec.AddLog("Stage completed successfully");
        }

        // ------------------------------
        // HATA
        // ------------------------------
        public static void MarkFailed(this StageExecution exec, string error)
        {
            exec.FinishedAt = DateTime.Now;
            exec.Status = StageStatus.Failed;
            exec.Error = error;

            if (exec.StartedAt.HasValue)
            {
                exec.DurationMs = (int)(exec.FinishedAt.Value - exec.StartedAt.Value).TotalMilliseconds;
            }

            exec.AddLog($"Stage failed: {error}");
        }

        // ------------------------------
        // RETRY OLUYOR
        // ------------------------------
        public static void MarkRetrying(this StageExecution exec)
        {
            exec.Status = StageStatus.Retrying;
            exec.RetryCount++;
            exec.AddLog($"Retrying stage... RetryCount = {exec.RetryCount}");
        }

        // ------------------------------
        // SONSUZA KADAR BAŞARISIZ
        // (tüm retry limitleri doldu)
        // ------------------------------
        public static void MarkPermanentlyFailed(this StageExecution exec, string error)
        {
            exec.FinishedAt = DateTime.Now;
            exec.Status = StageStatus.PermanentlyFailed;
            exec.Error = error;

            if (exec.StartedAt.HasValue)
            {
                exec.DurationMs = (int)(exec.FinishedAt.Value - exec.StartedAt.Value).TotalMilliseconds;
            }

            exec.AddLog($"Stage permanently failed: {error}");
        }
    }
}
