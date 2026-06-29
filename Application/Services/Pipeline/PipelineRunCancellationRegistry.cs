using System.Collections.Concurrent;

namespace Application.Services.Pipeline
{
    public static class PipelineRunCancellationRegistry
    {
        private static readonly ConcurrentDictionary<int, CancellationTokenSource> Sources = new();

        public static CancellationTokenSource Register(int runId)
        {
            var next = new CancellationTokenSource();
            Sources.AddOrUpdate(
                runId,
                next,
                (_, previous) =>
                {
                    previous.Cancel();
                    previous.Dispose();
                    return next;
                });

            return next;
        }

        public static bool Cancel(int runId)
        {
            if (!Sources.TryGetValue(runId, out var source)) return false;

            source.Cancel();
            return true;
        }

        public static void Complete(int runId, CancellationTokenSource source)
        {
            if (Sources.TryGetValue(runId, out var current) && ReferenceEquals(current, source))
            {
                Sources.TryRemove(runId, out _);
            }

            source.Dispose();
        }
    }
}
