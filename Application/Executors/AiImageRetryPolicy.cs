using Application.AiLayer.Abstract;
using Application.Pipeline;

namespace Application.Executors
{
    internal static class AiImageRetryPolicy
    {
        private const int MaxAttempts = 5;
        private const int BaseDelaySeconds = 8;
        private const int MaxDelaySeconds = 90;
        private const int DefaultRequestSpacingSeconds = 12;
        private const int MaxRequestSpacingSeconds = 60;
        private const int RateLimitSpacingStepSeconds = 8;
        private const int SuccessesBeforeSpacingDecay = 4;
        private const int SpacingDecaySeconds = 3;
        private const int IdleResetMinutes = 10;

        private static readonly object StateLock = new();
        private static readonly SemaphoreSlim RequestGate = new(1, 1);
        private static DateTimeOffset _lastRequestFinishedAt = DateTimeOffset.MinValue;
        private static DateTimeOffset _cooldownUntil = DateTimeOffset.MinValue;
        private static double _adaptiveRequestSpacingSeconds = DefaultRequestSpacingSeconds;
        private static int _consecutiveRateLimits;
        private static int _successfulRequestsSinceRateLimit;

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

                    return await RunWithRequestThrottleAsync(
                        operationLabel,
                        logAsync,
                        () => aiClient.GenerateImageAsync(
                            prompt: prompt,
                            negativePrompt: negativePrompt,
                            size: size,
                            style: style,
                            model: model,
                            ct: ct),
                        ct);
                }
                catch (Exception ex) when (IsRetryable(ex) && attempt < MaxAttempts)
                {
                    var retry = RegisterRetryableFailure(ex, attempt);
                    await logAsync(PipelineLiveLog.Warning(
                        $"{operationLabel} gecici kota/rate limit hatasi aldi. {retry.Delay.TotalSeconds:F0} sn beklenip tekrar denenecek. " +
                        $"Siradaki deneme: {attempt + 1}/{MaxAttempts}. Kota koruma araligi: {retry.RequestSpacingSeconds:F0} sn. " +
                        $"Hata: {PipelineLiveLog.Shorten(ex.Message, 260)}"));

                    await Task.Delay(retry.Delay, ct);
                }
            }

            throw new InvalidOperationException($"{operationLabel} icin gorsel uretimi tamamlanamadi.");
        }

        private static bool IsRetryable(Exception ex)
        {
            var message = ex.ToString();

            return ContainsAny(message,
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
                "timeout");
        }

        private static RetryDecision RegisterRetryableFailure(Exception ex, int failedAttempt)
        {
            var isRateLimit = IsRateLimit(ex);
            var exponentialSeconds = BaseDelaySeconds * Math.Pow(2, failedAttempt - 1);
            double spacingSeconds;

            lock (StateLock)
            {
                if (isRateLimit)
                {
                    _consecutiveRateLimits++;
                    _successfulRequestsSinceRateLimit = 0;

                    var multiplied = _adaptiveRequestSpacingSeconds * (_consecutiveRateLimits >= 2 ? 1.35 : 1.2);
                    var stepped = _adaptiveRequestSpacingSeconds + RateLimitSpacingStepSeconds;
                    _adaptiveRequestSpacingSeconds = Math.Min(MaxRequestSpacingSeconds, Math.Max(multiplied, stepped));
                }

                spacingSeconds = _adaptiveRequestSpacingSeconds;
            }

            var delaySeconds = isRateLimit
                ? Math.Max(exponentialSeconds, spacingSeconds)
                : exponentialSeconds;

            var cappedSeconds = Math.Min(MaxDelaySeconds, delaySeconds);
            var jitterSeconds = Random.Shared.NextDouble() * 5;
            var delay = TimeSpan.FromSeconds(cappedSeconds + jitterSeconds);

            if (isRateLimit)
            {
                lock (StateLock)
                {
                    var cooldownUntil = DateTimeOffset.UtcNow.Add(delay);
                    if (cooldownUntil > _cooldownUntil)
                        _cooldownUntil = cooldownUntil;
                }
            }

            return new RetryDecision(delay, spacingSeconds);
        }

        private static async Task<byte[]> RunWithRequestThrottleAsync(
            string operationLabel,
            Func<string, Task> logAsync,
            Func<Task<byte[]>> generateAsync,
            CancellationToken ct)
        {
            await RequestGate.WaitAsync(ct);

            try
            {
                var wait = CalculateRequestSpacingDelay();
                if (wait > TimeSpan.Zero)
                {
                    await logAsync($"Gorsel kota korumasi: {operationLabel} istegi oncesi {wait.TotalSeconds:F0} sn bekleniyor.");
                    await Task.Delay(wait, ct);
                }

                try
                {
                    var result = await generateAsync();
                    RegisterSuccess();
                    return result;
                }
                finally
                {
                    _lastRequestFinishedAt = DateTimeOffset.UtcNow;
                }
            }
            finally
            {
                RequestGate.Release();
            }
        }

        private static TimeSpan CalculateRequestSpacingDelay()
        {
            TimeSpan cooldownDelay;
            double spacingSeconds;
            var now = DateTimeOffset.UtcNow;

            lock (StateLock)
            {
                if (_lastRequestFinishedAt != DateTimeOffset.MinValue
                    && now - _lastRequestFinishedAt > TimeSpan.FromMinutes(IdleResetMinutes)
                    && _cooldownUntil <= now)
                {
                    ResetAdaptiveState();
                }

                spacingSeconds = _adaptiveRequestSpacingSeconds;
                cooldownDelay = _cooldownUntil <= now
                    ? TimeSpan.Zero
                    : _cooldownUntil - now;
            }

            var spacingDelay = TimeSpan.Zero;
            if (_lastRequestFinishedAt != DateTimeOffset.MinValue)
            {
                var elapsed = now - _lastRequestFinishedAt;
                var minSpacing = TimeSpan.FromSeconds(spacingSeconds);
                spacingDelay = elapsed >= minSpacing ? TimeSpan.Zero : minSpacing - elapsed;
            }

            return spacingDelay >= cooldownDelay ? spacingDelay : cooldownDelay;
        }

        private static void RegisterSuccess()
        {
            lock (StateLock)
            {
                _consecutiveRateLimits = 0;

                if (_adaptiveRequestSpacingSeconds <= DefaultRequestSpacingSeconds)
                    return;

                _successfulRequestsSinceRateLimit++;
                if (_successfulRequestsSinceRateLimit < SuccessesBeforeSpacingDecay)
                    return;

                _adaptiveRequestSpacingSeconds = Math.Max(
                    DefaultRequestSpacingSeconds,
                    _adaptiveRequestSpacingSeconds - SpacingDecaySeconds);
                _successfulRequestsSinceRateLimit = 0;
            }
        }

        private static bool IsRateLimit(Exception ex)
        {
            var message = ex.ToString();

            return ContainsAny(message,
                "429",
                "TooManyRequests",
                "RESOURCE_EXHAUSTED",
                "Resource exhausted",
                "rate limit",
                "quota");
        }

        private static void ResetAdaptiveState()
        {
            _cooldownUntil = DateTimeOffset.MinValue;
            _adaptiveRequestSpacingSeconds = DefaultRequestSpacingSeconds;
            _consecutiveRateLimits = 0;
            _successfulRequestsSinceRateLimit = 0;
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
        }

        private readonly record struct RetryDecision(TimeSpan Delay, double RequestSpacingSeconds);
    }
}
