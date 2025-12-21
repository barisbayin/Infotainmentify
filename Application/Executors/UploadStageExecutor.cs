using Application.Models;
using Application.Pipeline;
using Application.Services.Interfaces;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Application.Executors
{
    [StageExecutor(StageType.Upload)]
    public class UploadStageExecutor : BaseStageExecutor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IRepository<SocialChannel> _channelRepo;

        public UploadStageExecutor(IServiceProvider sp, IRepository<SocialChannel> channelRepo) : base(sp)
        {
            _serviceProvider = sp;
            _channelRepo = channelRepo;
        }

        public override StageType StageType => StageType.Upload;

        public override async Task<object?> ProcessAsync(
            ContentPipelineRun run, StageConfig config, StageExecution exec, PipelineContext context,
            object? presetObj, Func<string, Task> logAsync, CancellationToken ct)
        {
            await logAsync("🚀 Smart Multi-Platform Upload Started...");

            // 1. AYARLARI OKU
            if (string.IsNullOrEmpty(config.OptionsJson)) throw new InvalidOperationException("Upload options empty!");
            var options = JsonSerializer.Deserialize<UploadStageOptions>(config.OptionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (options == null || !options.Targets.Any()) throw new InvalidOperationException("No upload targets defined!");

            // 2. KAYNAK VERİLERİ (Video & Script)
            var renderOutput = context.GetOutput<RenderStagePayload>(StageType.Render);
            if (renderOutput == null || !File.Exists(renderOutput.VideoFilePath)) throw new FileNotFoundException("Video file not found!");

            var scriptOutput = context.GetOutput<ScriptStagePayload>(StageType.Script);

            // AI Verileri (Script'ten gelen ham veriler)
            string aiTitle = scriptOutput?.Title ?? "New Video";
            string aiDesc = scriptOutput?.Description ?? "";
            List<string> aiTags = scriptOutput?.Tags ?? new List<string>();

            // 3. SERVİSLERİ HAZIRLA
            var platformServices = _serviceProvider.GetServices<ISocialPlatformService>();
            var results = new UploadStagePayload();

            // 4. 🔥 HEDEF DÖNGÜSÜ (TARGET LOOP) 🔥
            foreach (var target in options.Targets)
            {
                // A) Kanalı Getir
                var channel = await _channelRepo.GetByIdAsync(target.SocialChannelId);
                if (channel == null)
                {
                    await logAsync($"⚠️ Channel ID {target.SocialChannelId} not found. Skipping.");
                    continue;
                }

                try
                {
                    await logAsync($"🛠️ Preparing metadata for {channel.ChannelName} ({channel.ChannelType})...");

                    // B) METADATA HARMANLAMA (TEMPLATE ENGINE)
                    // Şablon varsa işle, yoksa ham veriyi kullan

                    // -- Başlık --
                    string finalTitle = target.TitleTemplate ?? "{Title}";
                    finalTitle = finalTitle.Replace("{Title}", aiTitle)
                                           .Replace("{Date}", DateTime.Now.ToString("dd.MM.yyyy"));

                    // Platform limitlerine göre kırp (Örn: YouTube 100 karakter)
                    if (channel.ChannelType == SocialChannelType.YouTube && finalTitle.Length > 100)
                        finalTitle = finalTitle.Substring(0, 97) + "...";

                    // -- Açıklama --
                    string finalDesc = target.DescriptionTemplate ?? "{Description}";
                    finalDesc = finalDesc.Replace("{Description}", aiDesc)
                                         .Replace("{Title}", aiTitle);

                    // -- Etiketler --
                    // AI Tagleri + Platforma Özel Tagler birleşiyor
                    var finalTags = new List<string>(aiTags);
                    if (target.PlatformTags != null) finalTags.AddRange(target.PlatformTags);
                    finalTags = finalTags.Distinct().Take(30).ToList(); // Spam olmasın diye limit

                    // Metadata Paketi
                    var metadata = new SocialMetadata
                    {
                        Title = finalTitle,
                        Description = finalDesc,
                        Tags = finalTags,
                        PrivacyStatus = target.PrivacyStatus ?? options.DefaultPrivacy,
                        ThumbnailPath = null // İleride custom thumbnail seçilirse buraya gelir
                    };

                    // C) UPLOAD SERVİSİNİ BUL VE ÇALIŞTIR
                    var uploader = platformServices.FirstOrDefault(x => x.Type == channel.ChannelType);
                    if (uploader == null) throw new NotSupportedException($"No service for {channel.ChannelType}");

                    string url = await uploader.UploadAsync(channel, renderOutput.VideoFilePath, metadata, ct);

                    await logAsync($"✅ UPLOADED: {channel.ChannelName} -> {url}");

                    results.Uploads.Add(new UploadResultItem
                    {
                        Platform = channel.ChannelType.ToString(),
                        ChannelName = channel.ChannelName!,
                        VideoUrl = url,
                        IsSuccess = true
                    });
                }
                catch (Exception ex)
                {
                    await logAsync($"❌ FAIL: {channel.ChannelName} - {ex.Message}");
                    results.Uploads.Add(new UploadResultItem
                    {
                        Platform = channel.ChannelType.ToString(),
                        ChannelName = channel.ChannelName!,
                        IsSuccess = false,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return results;
        }
    }
}
