using Application.Abstractions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Reflection;

namespace Application.Executors
{
    public abstract class BaseStageExecutor : IStageExecutor
    {
        private readonly IServiceProvider _sp;

        protected BaseStageExecutor(IServiceProvider sp)
        {
            _sp = sp;
        }

        public abstract StageType StageType { get; }

        // 🔥 GÜNCELLEME 1: Abstract metoda 'logAsync' parametresi ekledik.
        // Artık miras alan sınıflar (ImageStageExecutor vb.) bu fonksiyonu kullanarak log atacak.
        public abstract Task<object?> ProcessAsync(
            ContentPipelineRun pipeline,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? preset,
            Func<string, Task> logAsync,
            CancellationToken ct);

        // 🔥 GÜNCELLEME 2: Interface'deki yeni imzayı uyguluyoruz
        public async Task<StageResult> ExecuteAsync(
            ContentPipelineRun pipeline,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            CancellationToken ct,
            Func<string, Task>? logCallback = null) // <--- Runner'dan gelen SignalR fonksiyonu
        {
            // ⚡ Merkezi Loglama Fonksiyonu
            // Miras alan sınıflar bunu çağırınca hem DB'ye hem SignalR'a gider.
            Func<string, Task> logAsync = async (message) =>
            {
                var normalizedMessage = PipelineLiveLog.Info(message);

                // 1. Veritabanı
                exec.AddLog(normalizedMessage);

                // 2. SignalR (Eğer Runner bir callback verdiyse)
                if (logCallback != null)
                {
                    await logCallback(normalizedMessage);
                }
            };

            try
            {
                var stageName = PipelineLiveLog.StageName(config.StageType);
                var watch = Stopwatch.StartNew();

                exec.MarkStarted();
                await logAsync($"Aşama başlatıldı: {stageName}.");

                // Preset çek
                if (config.PresetId.HasValue)
                {
                    await logAsync($"Preset yükleniyor. Preset ID: {config.PresetId.Value}.");
                }

                var preset = await LoadPresetAsync(config);

                if (config.PresetId.HasValue)
                {
                    await logAsync($"Preset yüklendi: {PipelineLiveLog.PresetName(preset)}.");
                }
                else
                {
                    await logAsync("Bu aşama preset kullanmadan çalışıyor.");
                }

                // Ana işi çalıştır (logAsync'i içeri paslıyoruz)
                var result = await ProcessAsync(pipeline, config, exec, context, preset, logAsync, ct);

                // Çıkan sonucu hafızaya (Context) at
                if (result != null)
                {
                    context.SetOutput(config.StageType, result);
                    await logAsync("Aşama çıktısı üretildi ve sonraki adımlar için hafızaya alındı.");
                }

                // Stage tamamlandı
                exec.MarkCompleted(result);
                watch.Stop();
                await logAsync(PipelineLiveLog.Success($"Aşama tamamlandı: {stageName}. Süre: {exec.DurationMs ?? (int)watch.ElapsedMilliseconds} ms."));

                return new StageResult
                {
                    Success = true,
                    Output = result
                };
            }
            catch (Exception ex)
            {
                exec.MarkFailed(ex.Message);

                // Hatayı da canlı terminale kırmızı basalım
                await logAsync(PipelineLiveLog.Error($"Aşama hata verdi: {PipelineLiveLog.StageName(config.StageType)}. Hata: {ex.Message}"));

                return new StageResult
                {
                    Success = false,
                    Error = ex.ToString()
                };
            }
        }

        // -------------------------------------
        // PRESET LOAD MEKANİZMASI (Değişmedi)
        // -------------------------------------
        private async Task<object?> LoadPresetAsync(StageConfig config)
        {
            if (config.PresetId == null) return null;

            var presetAttr = GetType().GetCustomAttribute<StagePresetAttribute>();
            if (presetAttr == null) return null;

            var repoType = typeof(IRepository<>).MakeGenericType(presetAttr.PresetEntityType);
            var repo = _sp.GetRequiredService(repoType);

            var method = repoType.GetMethod("GetByIdAsync", new[]
            {
                typeof(int), typeof(bool), typeof(CancellationToken)
            });

            if (method == null)
                throw new InvalidOperationException($"Repository method 'GetByIdAsync' bulunamadı.");

            var task = (Task)method.Invoke(repo, new object[] { config.PresetId.Value, true, CancellationToken.None })!;
            await task.ConfigureAwait(false);

            var resultProp = task.GetType().GetProperty("Result");
            return resultProp!.GetValue(task);
        }
    }
}
