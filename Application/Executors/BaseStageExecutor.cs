using Application.Abstractions;
using Application.Extensions;
using Application.Models;
using Application.Pipeline;
using Core.Attributes;
using Core.Contracts;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Executors
{
    public abstract class BaseStageExecutor : IStageExecutor
    {
        // ✅ 1. EKSİK OLAN KISIM: ServiceProvider'ı buraya tanımlamalıyız
        private readonly IServiceProvider _sp;

        protected BaseStageExecutor(IServiceProvider sp)
        {
            _sp = sp;
        }

        public abstract StageType StageType { get; }

        public abstract Task<object?> ProcessAsync(
            ContentPipelineRun pipeline,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            object? preset,
            CancellationToken ct);

        public async Task<StageResult> ExecuteAsync(
            ContentPipelineRun pipeline,
            StageConfig config,
            StageExecution exec,
            PipelineContext context,
            CancellationToken ct)
        {
            try
            {
                exec.MarkStarted();
                exec.AddLog($"Executor started: {GetType().Name}");

                // Preset çek
                var preset = await LoadPresetAsync(config);

                // Ana iş
                var result = await ProcessAsync(pipeline, config, exec, context, preset, ct);

                // Çıkan sonucu hafızaya (Context) at
                if (result != null)
                {
                    context.SetOutput(config.StageType, result);
                }

                // Stage tamamlandı
                exec.MarkCompleted(result);

                return new StageResult
                {
                    Success = true,
                    Output = result
                };
            }
            catch (Exception ex)
            {
                exec.MarkFailed(ex.Message);

                return new StageResult
                {
                    Success = false,
                    Error = ex.ToString()
                };
            }
        }

        // -------------------------------------
        // PRESET LOAD MEKANİZMASI (Reflection)
        // -------------------------------------
        private async Task<object?> LoadPresetAsync(StageConfig config)
        {
            if (config.PresetId == null)
                return null;

            // Bu Executor hangi Preset tipini kullanıyor? (Attribute'dan öğreniyoruz)
            var presetAttr = GetType().GetCustomAttribute<StagePresetAttribute>();
            if (presetAttr == null)
                return null;

            // IRepository<T> tipini oluştur
            // Örn: IRepository<TopicPreset>
            var repoType = typeof(IRepository<>).MakeGenericType(presetAttr.PresetEntityType);

            // _sp (ServiceProvider) üzerinden bu repository'i bul
            var repo = _sp.GetRequiredService(repoType);

            // ✅ DÜZELTME: Senin Repository yapında GetByIdAsync(int id, bool asNoTracking, CancellationToken ct) var.
            // Reflection ile doğru imzayı bulmamız lazım.
            var method = repoType.GetMethod("GetByIdAsync", new[]
            {
                typeof(int),
                typeof(bool),
                typeof(CancellationToken)
            });

            if (method == null)
                throw new InvalidOperationException($"Repository method 'GetByIdAsync' bulunamadı. Entity: {presetAttr.PresetEntityType.Name}");

            // Metodu çalıştır: GetByIdAsync(id, true, None) -> true = asNoTracking
            var task = (Task)method.Invoke(repo, new object[] { config.PresetId.Value, true, CancellationToken.None })!;

            await task.ConfigureAwait(false);

            // Task sonucunu (Result) al
            var resultProp = task.GetType().GetProperty("Result");
            return resultProp!.GetValue(task);
        }
    }
}
