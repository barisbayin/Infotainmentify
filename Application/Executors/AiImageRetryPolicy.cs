using Application.AiLayer.Abstract;
using Application.Pipeline;

namespace Application.Executors
{
    internal static class AiImageRetryPolicy
    {
        private const int MaxAttempts = 5;
        private const int BaseDelaySeconds = 8;
        private const int MaxDelaySeconds = 75;

        public static async Task<byte[]> GenerateImageAsync(
            IImageGenerator aiClient,
            string operationLabel,
            string prompt,
            string? negativePrompt,
            string size,
            string? style,
            string? model,
            Func<string, Task> logAsync,
            CancellationToken ct)
        {
            for (var attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                try
                {
                    if (attempt > 1)
                    {
                        await logAsync($"{operationLabel} yeniden deneniyor. Deneme {attempt}/{MaxAttempts}.");
                    }

                    return await aiClient.GenerateImageAsync(
                        prompt: prompt,
                        negativePrompt: negativePrompt,
                        size: size,
                        style: style,
                        model: model,
                        ct: ct);
                }
                catch (Exception ex) when (IsRetryable(ex) && attempt < MaxAttempts)
                {
                    var delay = CalculateDelay(attempt);
                    await logAsync(PipelineLiveLog.Warning(
                        $"{operationLabel} geçici kota/rate limit hatası aldı. {delay.TotalSeconds:F0} sn beklenip tekrar denenecek. " +
                        $"Sıradaki deneme: {attempt + 1}/{MaxAttempts}. Hata: {PipelineLiveLog.Shorten(ex.Message, 260)}"));

                    await Task.Delay(delay, ct);
                }
            }

            throw new InvalidOperationException($"{operationLabel} için görsel üretimi tamamlanamadı.");
        }

        private static bool IsRetryable(Exception ex)
        {
            var message = ex.ToString();

            if (ContainsAny(message,
                    "429",
                    "TooManyRequests",
                    "RESOURCE_EXHAUSTED",
                    "Resource exhausted",
                    "rate limit",
                    "quota",
                    "temporarily unavailable",
                    "503",
                    "504",
                    "500",
                    "ServiceUnavailable",
                    "InternalServerError",
                    "DeadlineExceeded",
                    "timeout"))
            {
                return true;
            }

            return false;
        }

        private static TimeSpan CalculateDelay(int failedAttempt)
        {
            var exponentialSeconds = BaseDelaySeconds * Math.Pow(2, failedAttempt - 1);
            var cappedSeconds = Math.Min(MaxDelaySeconds, exponentialSeconds);
            var jitterSeconds = Random.Shared.NextDouble() * 4;

            return TimeSpan.FromSeconds(cappedSeconds + jitterSeconds);
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }
    }
}
