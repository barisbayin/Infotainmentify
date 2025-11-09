using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Job
{
    public class BackgroundJobRunner
    {
        private readonly IServiceProvider _provider;

        public BackgroundJobRunner(IServiceProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Bir işi arka planda başlatır ve async olarak ilerler.
        /// FE beklemez, SignalR üzerinden takip eder.
        /// </summary>
        public void Run(Func<IServiceProvider, CancellationToken, Task> jobFunc)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _provider.CreateScope();
                    var ct = CancellationToken.None;
                    await jobFunc(scope.ServiceProvider, ct);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ BackgroundJobRunner hata: {ex.Message}");
                }
            });
        }
    }
}
