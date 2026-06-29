using Application.Contracts.Pipeline;
using Application.Executors;
using Application.Models;
using Application.Services.Base;
using Core.Attributes;
using Core.Contracts;
using Core.Entity;
using Core.Entity.Pipeline;
using Core.Entity.Presets;
using Core.Entity.User;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Text.Json;

namespace Application.Services.Pipeline
{
    public class PipelineTemplateService : BaseService<ContentPipelineTemplate>
    {
        private readonly IRepository<StageConfig> _stageRepo;
        private readonly IServiceProvider _serviceProvider;

        public PipelineTemplateService(
            IRepository<ContentPipelineTemplate> repo,
            IRepository<StageConfig> stageRepo,
            IUnitOfWork uow,
            IServiceProvider serviceProvider) : base(repo, uow)
        {
            _stageRepo = stageRepo;
            _serviceProvider = serviceProvider;
        }

        public async Task<IReadOnlyList<ContentPipelineTemplate>> ListAsync(
                    int userId,
                    string? q,
                    int? conceptId, // 🔥 YENİ PARAMETRE
                    CancellationToken ct)
        {
            return await _repo.FindAsync(
                predicate: t =>
                    t.AppUserId == userId &&
                    (!conceptId.HasValue || t.ConceptId == conceptId) && // 🔥 FİLTRE
                    (string.IsNullOrWhiteSpace(q) || t.Name.Contains(q)),
                orderBy: t => t.CreatedAt,
                desc: true,
                include: src => src.Include(t => t.Concept).Include(t => t.StageConfigs),
                asNoTracking: true,
                ct: ct
            );
        }

        // GET DETAIL (Stages Include Edilmeli)
        public override async Task<ContentPipelineTemplate?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            // Base metodu override ediyoruz çünkü Include lazım
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.StageConfigs),
                asNoTracking: true,
                ct: ct);

            if (entity == null || entity.AppUserId != userId) return null;
            return entity;
        }

        // CREATE
        public async Task<int> CreateAsync(SavePipelineTemplateDto dto, int userId, CancellationToken ct)
        {
            // İsim Çakışması
            if (await _repo.AnyAsync(t => t.AppUserId == userId && t.Name == dto.Name, ct))
                throw new InvalidOperationException("Bu isimde bir şablon zaten var.");

            var entity = new ContentPipelineTemplate
            {
                Name = dto.Name,
                Description = dto.Description,
                ConceptId = dto.ConceptId,
                ProductionProfile = NormalizeProductionProfile(dto.ProductionProfile),
                AutoPublish = dto.AutoPublish,
                WorkflowLayoutJson = NormalizeWorkflowLayoutJson(dto.WorkflowLayoutJson),
                // AppUserId BaseService'de set edilecek
            };

            // Stage'leri ekle
            if (dto.Stages != null)
            {
                int order = 1;
                foreach (var stageDto in dto.Stages)
                {
                    entity.StageConfigs.Add(new StageConfig
                    {
                        StageType = stageDto.StageType,
                        PresetId = stageDto.PresetId,
                        Order = order++, // Otomatik sıra numarası
                        OptionsJson = stageDto.OptionsJson
                    });
                }
            }

            await base.AddAsync(entity, userId, ct);
            return entity.Id;
        }

        // UPDATE
        public async Task UpdateAsync(int id, SavePipelineTemplateDto dto, int userId, CancellationToken ct)
        {
            // 1. Template'i ve Mevcut Stage'lerini ÇEK (Tracking AÇIK)
            // Include yapıyoruz çünkü var olan satırları güncelleyeceğiz.
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.StageConfigs),
                asNoTracking: false,
                ct: ct);

            if (entity == null || entity.AppUserId != userId)
                throw new KeyNotFoundException("Şablon bulunamadı.");

            // 2. Ana bilgileri güncelle
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.ConceptId = dto.ConceptId;
            entity.ProductionProfile = NormalizeProductionProfile(dto.ProductionProfile);
            entity.AutoPublish = dto.AutoPublish;
            entity.WorkflowLayoutJson = NormalizeWorkflowLayoutJson(dto.WorkflowLayoutJson);

            // =================================================================
            // 🔥 AKILLI EŞİTLEME (SMART SYNC)
            // Silip yeniden eklemek yerine, eldekileri güncelliyoruz.
            // =================================================================

            var newStages = dto.Stages ?? new List<SaveStageConfigDto>();
            var existingStages = entity.StageConfigs.OrderBy(x => x.Order).ToList();

            // Döngü ile eşleştirme yapıyoruz
            int maxCount = Math.Max(newStages.Count, existingStages.Count);

            for (int i = 0; i < maxCount; i++)
            {
                if (i < newStages.Count && i < existingStages.Count)
                {
                    // A) İKİSİ DE VAR -> GÜNCELLE (Recycle)
                    // Mevcut satırı al, yeni verilerle güncelle. ID ve Log bağlantısı korunur.
                    var existing = existingStages[i];
                    var newVal = newStages[i];

                    existing.StageType = newVal.StageType;
                    existing.PresetId = newVal.PresetId == 0 ? null : newVal.PresetId;
                    existing.Order = i + 1;
                    existing.OptionsJson = newVal.OptionsJson;

                    // Soft delete olmuşsa geri getir (Eğer sistemde varsa)
                    // existing.IsDeleted = false; 
                }
                else if (i < newStages.Count)
                {
                    // B) YENİSİ FAZLA -> EKLE (Insert)
                    var newVal = newStages[i];
                    entity.StageConfigs.Add(new StageConfig
                    {
                        StageType = newVal.StageType,
                        PresetId = newVal.PresetId == 0 ? null : newVal.PresetId,
                        Order = i + 1,
                        OptionsJson = newVal.OptionsJson
                        // TemplateId otomatik set edilir
                    });
                }
                else
                {
                    // C) ESKİSİ FAZLA -> SİL (Soft Delete)
                    // Fazlalık olan satırı Repo üzerinden siliyoruz.
                    // Repo'da Soft Delete varsa "Removed=1" yapar, fiziksel silmez. Hata vermez.
                    var extra = existingStages[i];
                    _stageRepo.Delete(extra);
                }
            }

            entity.UpdatedAt = DateTime.UtcNow;

            // 3. Kaydet
            await _uow.SaveChangesAsync(ct);
        }

        public async Task UpdateWorkflowLayoutAsync(int id, UpdateWorkflowLayoutDto dto, int userId, CancellationToken ct)
        {
            var entity = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                asNoTracking: false,
                ct: ct);

            if (entity == null || entity.AppUserId != userId)
                throw new KeyNotFoundException("Şablon bulunamadı.");

            entity.WorkflowLayoutJson = NormalizeWorkflowLayoutJson(dto.WorkflowLayoutJson);
            entity.UpdatedAt = DateTime.UtcNow;

            await _uow.SaveChangesAsync(ct);
        }

        private static string? NormalizeWorkflowLayoutJson(string? layoutJson)
        {
            if (string.IsNullOrWhiteSpace(layoutJson))
                return null;

            using var document = JsonDocument.Parse(layoutJson);
            return document.RootElement.GetRawText();
        }

        private static string NormalizeProductionProfile(string? profile)
        {
            return profile?.Trim() switch
            {
                "Shorts" => "Shorts",
                "LongForm" => "LongForm",
                "Podcast" => "Podcast",
                _ => "Generic"
            };
        }

        public async Task<PipelineTemplateHealthDto?> GetHealthAsync(int id, int userId, CancellationToken ct)
        {
            var template = await _repo.FirstOrDefaultAsync(
                t => t.Id == id,
                include: src => src.Include(t => t.Concept).Include(t => t.StageConfigs),
                asNoTracking: true,
                ct: ct);

            if (template == null || template.AppUserId != userId) return null;

            var stages = template.StageConfigs.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList();
            var report = new PipelineTemplateHealthDto
            {
                TemplateId = template.Id,
                TemplateName = template.Name,
                ProductionProfile = NormalizeProductionProfile(template.ProductionProfile)
            };

            var executorMap = BuildExecutorMap();

            if (!stages.Any())
            {
                AddIssue(report, null, "Error", "workflow.empty", "Bu workflow icinde hic stage yok.");
            }

            foreach (var duplicateOrder in stages.GroupBy(x => x.Order).Where(x => x.Count() > 1))
            {
                AddIssue(report, null, "Error", "workflow.duplicate_order", $"Order {duplicateOrder.Key} birden fazla stage tarafindan kullaniliyor.");
            }

            for (var i = 0; i < stages.Count; i++)
            {
                var expectedOrder = i + 1;
                if (stages[i].Order != expectedOrder)
                {
                    AddIssue(report, null, "Warning", "workflow.order_gap", $"Stage sirasi {expectedOrder} beklenirken {stages[i].Order} geldi.");
                    break;
                }
            }

            var seenStageTypes = new Dictionary<StageType, int>();

            foreach (var stage in stages)
            {
                var stageHealth = new PipelineTemplateHealthStageDto
                {
                    StageConfigId = stage.Id,
                    Order = stage.Order,
                    StageType = stage.StageType.ToString(),
                    PresetId = stage.PresetId
                };

                report.Stages.Add(stageHealth);

                if (seenStageTypes.TryGetValue(stage.StageType, out var firstOrder))
                {
                    AddIssue(report, stageHealth, "Warning", "stage.duplicate_type", $"{stage.StageType} stage tipi workflow icinde birden fazla kullaniliyor. Ilk kullanim: #{firstOrder}.");
                }
                else
                {
                    seenStageTypes[stage.StageType] = stage.Order;
                }

                if (!executorMap.TryGetValue(stage.StageType, out var executor))
                {
                    AddIssue(report, stageHealth, "Error", "stage.executor_missing", $"{stage.StageType} icin calisabilir executor kaydi yok.");
                }
                else
                {
                    stageHealth.ExecutorName = executor.ExecutorType.Name;
                }

                CheckDependencies(report, stageHealth, stages, stage);
                ValidateOptionsJson(report, stageHealth, stage);

                var expectedPresetType = GetExpectedPresetType(stage.StageType, executor?.ExecutorType);
                stageHealth.PresetEntityType = expectedPresetType?.Name;

                if (expectedPresetType != null)
                {
                    var presetRequired = IsPresetRequired(stage.StageType);
                    if (!stage.PresetId.HasValue)
                    {
                        AddIssue(
                            report,
                            stageHealth,
                            presetRequired ? "Error" : "Warning",
                            "stage.preset_missing",
                            presetRequired
                                ? $"{stage.StageType} stage'i calismak icin {expectedPresetType.Name} preset'i ister."
                                : $"{stage.StageType} stage'i preset olmadan calisabilir ama explicit {expectedPresetType.Name} secmek daha okunur olur.");
                    }
                    else
                    {
                        var preset = await LoadEntityAsync(expectedPresetType, stage.PresetId.Value, ct);
                        if (preset == null)
                        {
                            AddIssue(report, stageHealth, "Error", "stage.preset_not_found", $"{expectedPresetType.Name} #{stage.PresetId} bulunamadi.");
                        }
                        else
                        {
                            stageHealth.PresetName = ReadString(preset, "Name") ?? $"#{stage.PresetId}";
                            ValidatePresetOwnership(report, stageHealth, preset, userId);
                            await ValidatePresetConnectionAsync(report, stageHealth, preset, userId, ct);
                            ValidatePresetShape(report, stageHealth, preset);
                            ReadPresetFacts(stageHealth, preset);
                            ValidateProductionProfilePreset(report, stageHealth, preset, report.ProductionProfile);
                            ReadRenderDimensions(stageHealth, preset);
                        }
                    }
                }
                else if (stage.PresetId.HasValue)
                {
                    AddIssue(report, stageHealth, "Warning", "stage.preset_ignored", $"{stage.StageType} icin preset entity mapping yok. Preset #{stage.PresetId} runtime tarafinda kullanilmayabilir.");
                }

                if (stage.StageType == StageType.Upload)
                {
                    await ValidateUploadOptionsAsync(report, stageHealth, stage, userId, ct);
                }
            }

            AddWorkflowLevelHints(report, stages, template);
            AddProductionProfileWorkflowHints(report, stages, template);
            AddRenderConsistencyHints(report);
            FinalizeHealth(report);
            return report;
        }

        private static Dictionary<StageType, ExecutorDescriptor> BuildExecutorMap()
        {
            return Assembly.GetAssembly(typeof(BaseStageExecutor))!
                .GetTypes()
                .Where(t => !t.IsAbstract && t.GetCustomAttribute<StageExecutorAttribute>() != null)
                .Select(t => new
                {
                    Type = t.GetCustomAttribute<StageExecutorAttribute>()!.Type,
                    Descriptor = new ExecutorDescriptor(t)
                })
                .ToDictionary(x => x.Type, x => x.Descriptor);
        }

        private static Type? GetExpectedPresetType(StageType stageType, Type? executorType)
        {
            if (stageType == StageType.SceneLayout) return typeof(RenderPreset);
            return executorType?.GetCustomAttribute<StagePresetAttribute>()?.PresetEntityType;
        }

        private static bool IsPresetRequired(StageType stageType)
        {
            return stageType is StageType.Topic
                or StageType.Script
                or StageType.Image
                or StageType.Thumbnail
                or StageType.Tts
                or StageType.Stt
                or StageType.SceneLayout;
        }

        private static void CheckDependencies(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            IReadOnlyList<StageConfig> allStages,
            StageConfig stage)
        {
            var requiredInputs = GetRequiredInputs(stage.StageType);
            stageHealth.RequiredInputs = requiredInputs.Select(x => x.ToString()).ToList();

            if (!requiredInputs.Any()) return;

            var previousStageTypes = allStages
                .Where(x => x.Order < stage.Order)
                .Select(x => x.StageType)
                .ToHashSet();

            stageHealth.SatisfiedInputs = requiredInputs
                .Where(previousStageTypes.Contains)
                .Select(x => x.ToString())
                .ToList();

            foreach (var required in requiredInputs)
            {
                if (previousStageTypes.Contains(required)) continue;

                var laterStage = allStages.FirstOrDefault(x => x.StageType == required && x.Order > stage.Order);
                if (laterStage != null)
                {
                    AddIssue(report, stageHealth, "Error", "stage.dependency_order", $"{stage.StageType}, {required} ciktisini bekliyor ama {required} stage'i daha sonra geliyor.");
                    continue;
                }

                AddIssue(report, stageHealth, "Error", "stage.dependency_missing", $"{stage.StageType}, {required} stage ciktisina ihtiyac duyuyor.");
            }
        }

        private static StageType[] GetRequiredInputs(StageType stageType)
        {
            return stageType switch
            {
                StageType.CreativeDirector => new[] { StageType.Topic },
                StageType.Script => new[] { StageType.Topic },
                StageType.Storyboard => new[] { StageType.Script },
                StageType.Image => new[] { StageType.Script },
                StageType.Thumbnail => new[] { StageType.Script },
                StageType.VideoAI => new[] { StageType.Script, StageType.Image },
                StageType.Tts => new[] { StageType.Script },
                StageType.Stt => new[] { StageType.Tts },
                StageType.EditPlan => new[] { StageType.Script, StageType.Image, StageType.Tts },
                StageType.SceneLayout => new[] { StageType.Script, StageType.Image, StageType.Tts },
                StageType.Render => new[] { StageType.SceneLayout },
                StageType.Upload => new[] { StageType.Render },
                _ => Array.Empty<StageType>()
            };
        }

        private static void ValidateOptionsJson(PipelineTemplateHealthDto report, PipelineTemplateHealthStageDto stageHealth, StageConfig stage)
        {
            if (stage.StageType == StageType.Upload || string.IsNullOrWhiteSpace(stage.OptionsJson)) return;

            try
            {
                using var _ = JsonDocument.Parse(stage.OptionsJson);
            }
            catch (Exception ex)
            {
                AddIssue(report, stageHealth, "Error", "stage.options_json_invalid", $"OptionsJson gecersiz JSON: {ex.Message}");
            }
        }

        private async Task ValidateUploadOptionsAsync(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            StageConfig stage,
            int userId,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(stage.OptionsJson))
            {
                AddIssue(report, stageHealth, "Error", "upload.options_missing", "Upload stage icin hedef kanal ayarlari bos.");
                return;
            }

            UploadStageOptions? options;
            try
            {
                options = JsonSerializer.Deserialize<UploadStageOptions>(
                    stage.OptionsJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (Exception ex)
            {
                AddIssue(report, stageHealth, "Error", "upload.options_invalid", $"Upload OptionsJson okunamadi: {ex.Message}");
                return;
            }

            if (options == null || options.Targets.Count == 0)
            {
                AddIssue(report, stageHealth, "Error", "upload.targets_missing", "Upload stage icinde en az bir target olmali.");
                return;
            }

            foreach (var target in options.Targets)
            {
                var targetDto = new PipelineTemplateHealthUploadTargetDto
                {
                    SocialChannelId = target.SocialChannelId
                };
                stageHealth.UploadTargets.Add(targetDto);

                var channel = await LoadEntityAsync(typeof(SocialChannel), target.SocialChannelId, ct);
                if (channel == null)
                {
                    targetDto.Severity = "Error";
                    targetDto.Message = "Kanal bulunamadi.";
                    AddIssue(report, stageHealth, "Error", "upload.channel_not_found", $"SocialChannel #{target.SocialChannelId} bulunamadi.");
                    continue;
                }

                targetDto.ChannelName = ReadString(channel, "ChannelName") ?? $"Channel #{target.SocialChannelId}";
                targetDto.ChannelType = ReadEnumString(channel, "ChannelType");

                if (ReadInt(channel, "AppUserId") != userId)
                {
                    targetDto.Severity = "Error";
                    targetDto.Message = "Bu kanal baska kullaniciya ait.";
                    AddIssue(report, stageHealth, "Error", "upload.channel_owner_mismatch", $"{targetDto.ChannelName} bu kullaniciya ait degil.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(ReadString(channel, "EncryptedTokensJson")))
                {
                    targetDto.Severity = "Warning";
                    targetDto.Message = "OAuth token bos gorunuyor.";
                    AddIssue(report, stageHealth, "Warning", "upload.channel_tokens_missing", $"{targetDto.ChannelName} icin token bilgisi bos gorunuyor.");
                }
                else
                {
                    targetDto.Severity = "Info";
                    targetDto.Message = "Kanal ayari okunabilir.";
                }
            }
        }

        private static void ValidatePresetOwnership(PipelineTemplateHealthDto report, PipelineTemplateHealthStageDto stageHealth, object preset, int userId)
        {
            var ownerId = ReadInt(preset, "AppUserId");
            if (ownerId.HasValue && ownerId.Value != userId)
            {
                AddIssue(report, stageHealth, "Error", "preset.owner_mismatch", $"{stageHealth.PresetName} baska bir kullaniciya ait gorunuyor.");
            }
        }

        private async Task ValidatePresetConnectionAsync(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            object preset,
            int userId,
            CancellationToken ct)
        {
            var connectionId = ReadInt(preset, "UserAiConnectionId");
            if (!connectionId.HasValue) return;

            var connection = await LoadEntityAsync(typeof(UserAiConnection), connectionId.Value, ct);
            if (connection == null)
            {
                AddIssue(report, stageHealth, "Error", "preset.connection_missing", $"AI connection #{connectionId.Value} bulunamadi.");
                return;
            }

            var ownerId = ReadInt(connection, "AppUserId");
            if (ownerId.HasValue && ownerId.Value != userId)
            {
                AddIssue(report, stageHealth, "Error", "preset.connection_owner_mismatch", $"AI connection #{connectionId.Value} baska kullaniciya ait.");
            }

            if (string.IsNullOrWhiteSpace(ReadString(connection, "EncryptedApiKey")))
            {
                AddIssue(report, stageHealth, "Error", "preset.connection_secret_missing", $"{ReadString(connection, "Name") ?? "AI connection"} icin API key bos.");
            }
        }

        private static void ValidatePresetShape(PipelineTemplateHealthDto report, PipelineTemplateHealthStageDto stageHealth, object preset)
        {
            ValidateRequiredText(report, stageHealth, preset, "Name", "preset.name_missing", "Preset adi bos.");
            ValidateRequiredText(report, stageHealth, preset, "ModelName", "preset.model_missing", "ModelName bos.");
            ValidateRequiredText(report, stageHealth, preset, "PromptTemplate", "preset.prompt_missing", "PromptTemplate bos.");
            ValidateRequiredText(report, stageHealth, preset, "VoiceId", "preset.voice_missing", "VoiceId bos.");

            foreach (var propName in new[]
            {
                "CaptionSettingsJson",
                "AudioMixSettingsJson",
                "VisualEffectsSettingsJson",
                "BrandingSettingsJson",
                "CameraControlSettingsJson",
                "AdvancedSettingsJson",
                "ContextKeywordsJson"
            })
            {
                ValidateOptionalJson(report, stageHealth, preset, propName);
            }

            if (preset is RenderPreset render)
            {
                if (render.OutputWidth <= 0 || render.OutputHeight <= 0)
                    AddIssue(report, stageHealth, "Error", "render.size_invalid", "Render output width/height 0'dan buyuk olmali.");
                if (render.Fps <= 0)
                    AddIssue(report, stageHealth, "Error", "render.fps_invalid", "Render FPS 0'dan buyuk olmali.");
                if (render.BitrateKbps <= 0)
                    AddIssue(report, stageHealth, "Warning", "render.bitrate_invalid", "Render bitrate 0'dan buyuk olmali.");
            }

            if (preset is ImagePreset image && !TryParseSize(image.Size, out _, out _))
            {
                AddIssue(report, stageHealth, "Warning", "image.size_unknown", $"Image size '{image.Size}' parse edilemedi.");
            }
        }

        private static void ValidateRequiredText(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            object preset,
            string propertyName,
            string code,
            string message)
        {
            if (preset.GetType().GetProperty(propertyName) == null) return;
            if (string.IsNullOrWhiteSpace(ReadString(preset, propertyName)))
                AddIssue(report, stageHealth, "Error", code, message);
        }

        private static void ValidateOptionalJson(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            object preset,
            string propertyName)
        {
            var value = ReadString(preset, propertyName);
            if (string.IsNullOrWhiteSpace(value)) return;

            try
            {
                using var _ = JsonDocument.Parse(value);
            }
            catch (Exception ex)
            {
                AddIssue(report, stageHealth, "Error", "preset.json_invalid", $"{propertyName} gecersiz JSON: {ex.Message}");
            }
        }

        private static void ReadRenderDimensions(PipelineTemplateHealthStageDto stageHealth, object preset)
        {
            if (preset is not RenderPreset render) return;
            stageHealth.OutputWidth = render.OutputWidth;
            stageHealth.OutputHeight = render.OutputHeight;
            stageHealth.Fps = render.Fps;
            stageHealth.AspectRatio = BuildAspectRatio(render.OutputWidth, render.OutputHeight);
        }

        private static void ReadPresetFacts(PipelineTemplateHealthStageDto stageHealth, object preset)
        {
            if (preset is ScriptPreset script)
            {
                stageHealth.TargetDurationSec = script.TargetDurationSec;
            }

            if (preset is ImagePreset image)
            {
                stageHealth.ImageSize = image.Size;
            }
        }

        private static void ValidateProductionProfilePreset(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto stageHealth,
            object preset,
            string productionProfile)
        {
            if (productionProfile != "LongForm") return;

            if (preset is TopicPreset topic)
            {
                var promptText = $"{topic.SystemInstruction} {topic.PromptTemplate}";
                if (ContainsAny(promptText, "topic ideas", "generate 1 long-form youtube topic idea", "generate 1 long-form youtube topic ideas"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.topic_idea_list_prompt", "Topic prompt'u fikir listesi gibi gorunuyor. Long Form akista brief'i production-ready topic document'a cevirmeli.");
                }

                if (!ContainsAny(promptText, "{MainTitle}", "{BriefTitle}"))
                {
                    AddIssue(report, stageHealth, "Info", "profile.longform.topic_brief_anchor_missing", "Topic prompt'u {MainTitle}/{BriefTitle} placeholder'i kullanirsa brief disina kayma riski azalir.");
                }

                if (!ContainsAny(promptText, "{Angle}", "{Audience}", "{MustCover}", "{Avoid}", "{Notes}"))
                {
                    AddIssue(report, stageHealth, "Info", "profile.longform.topic_brief_detail_missing", "Topic prompt'u angle/audience/must-cover/avoid/notlar alanlarini kullanirsa sonraki stage'ler daha iyi beslenir.");
                }
            }

            if (preset is ScriptPreset script)
            {
                if (script.TargetDurationSec < 480)
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_duration_short", "Long Form icin script preset hedefi en az 8 dakika (480 sn) olmali.");
                }

                var promptText = $"{script.SystemInstruction} {script.PromptTemplate}";
                if (!ContainsAny(promptText, "chapter", "section", "outline", "intro", "outro", "bolum", "bölüm"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_structure_missing", "Long Form script prompt'u chapter/section/intro/outro gibi bolumlu bir yapiyi acikca istemeli.");
                }

                var locksOldJsonShape = ContainsAny(
                    promptText,
                    "do not add extra json fields",
                    "use only the exact json shape",
                    "output exactly this json shape",
                    "each scene must include: scene, audiotext, visualprompt, durationsec");
                var hasSceneDirectionFields = ContainsAny(
                    promptText,
                    "sceneRole",
                    "scenePurpose",
                    "viewerQuestion",
                    "emotionalBeat",
                    "visualType",
                    "cameraPlan",
                    "transitionIntent");

                if (locksOldJsonShape && !hasSceneDirectionFields)
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_contract_conflict", "Script prompt'u eski JSON shape'e kilitli gorunuyor. Scene Direction V2 alanlari zayif kalabilir.");
                }

                if (ContainsAny(promptText, "do not add extra json fields", "do not add chapter objects", "do not add visualbeats"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_contract_in_user_prompt", "JSON kontrati kullanici prompt'una fazla tasinmis. Kontrat backend tarafindan eklenir; preset yaratici niyeti anlatmali.");
                }

                if (script.TargetDurationSec >= 480 && ContainsAny(promptText, "12 to 18 scenes", "12-18 scenes"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_scene_count_low", "8+ dk long-form icin 12-18 script sahnesi ritmi zayiflatabilir. 45-80 anlatim sahnesi daha iyi baslangic.");
                }

                if (ContainsAny(promptText, "100 to 120 scenes", "100-120 scenes", "130 scenes", "approximately 130"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_scene_count_high", "Cok fazla script sahnesi image/TTS maliyetini ve rate-limit riskini artirir. Gorsel beatleri Storyboard/EditPlan yonetmeli.");
                }

                if (ContainsAny(promptText, "new visual should be generated every 4", "each scene represents exactly one visual generation"))
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.script_visualbeat_mixed", "Script sahnesi gorsel beat'e karismis gorunuyor. Gorsel ritmi Storyboard/Image/EditPlan katmanlari yonetmeli.");
                }
            }

            if (preset is ImagePreset image && TryParseSize(image.Size, out var imageWidth, out var imageHeight) && imageWidth < imageHeight)
            {
                AddIssue(report, stageHealth, "Warning", "profile.longform.image_portrait", "Long Form 16:9 video icin Image preset landscape/wide boyut kullanmali.");
            }

            if (preset is RenderPreset render)
            {
                var aspectRatio = BuildAspectRatio(render.OutputWidth, render.OutputHeight);
                if (aspectRatio != "16:9")
                {
                    AddIssue(report, stageHealth, "Error", "profile.longform.render_aspect_mismatch", $"Long Form profile 16:9 render ister; secili preset {aspectRatio}.");
                }

                if (render.OutputWidth < 1280 || render.OutputHeight < 720)
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.render_resolution_low", "Long Form icin en az 1280x720, tercihen 1920x1080 render kullan.");
                }

                if (render.Fps < 24)
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.render_fps_low", "Long Form icin 24 FPS altina inmek onerilmez.");
                }

                if (render.BitrateKbps < 6000)
                {
                    AddIssue(report, stageHealth, "Warning", "profile.longform.render_bitrate_low", "Long Form 1080p icin bitrate 6000 kbps altinda kalite dusurebilir.");
                }
            }
        }

        private static void AddWorkflowLevelHints(PipelineTemplateHealthDto report, IReadOnlyList<StageConfig> stages, ContentPipelineTemplate template)
        {
            if (template.AutoPublish && stages.All(x => x.StageType != StageType.Upload))
            {
                AddIssue(report, null, "Warning", "workflow.autopublish_without_upload", "AutoPublish acik ama workflow icinde Upload stage yok.");
            }

            if (!template.AutoPublish && stages.Any(x => x.StageType == StageType.Upload))
            {
                AddIssue(report, null, "Info", "workflow.manual_approval", "Upload stage var ama AutoPublish kapali. Render sonrasi manuel onayda duracak.");
            }

            if (stages.Any(x => x.StageType == StageType.Stt) && stages.All(x => x.StageType != StageType.SceneLayout))
            {
                AddIssue(report, null, "Warning", "workflow.stt_without_layout", "STT ciktisi genelde SceneLayout/Render caption icin kullanilir; layout stage yok.");
            }

            var creativeDirectorStage = stages.FirstOrDefault(x => x.StageType == StageType.CreativeDirector);
            var topicStage = stages.FirstOrDefault(x => x.StageType == StageType.Topic);
            var scriptStageForDirection = stages.FirstOrDefault(x => x.StageType == StageType.Script);
            if (creativeDirectorStage != null && topicStage != null && creativeDirectorStage.Order < topicStage.Order)
            {
                AddIssue(report, null, "Error", "workflow.creative_director_before_topic", "CreativeDirector, Topic'ten sonra gelmeli; video stratejisi konu ciktisina ihtiyac duyar.");
            }

            if (creativeDirectorStage != null && scriptStageForDirection != null && creativeDirectorStage.Order > scriptStageForDirection.Order)
            {
                AddIssue(report, null, "Warning", "workflow.creative_director_after_script", "CreativeDirector, Script'ten once gelirse senaryo prompt'una video stratejisi aktarilir.");
            }

            var editPlanStage = stages.FirstOrDefault(x => x.StageType == StageType.EditPlan);
            var sceneLayoutStage = stages.FirstOrDefault(x => x.StageType == StageType.SceneLayout);
            if (editPlanStage != null && sceneLayoutStage != null && editPlanStage.Order > sceneLayoutStage.Order)
            {
                AddIssue(report, null, "Error", "workflow.editplan_after_layout", "EditPlan, SceneLayout'tan once gelmeli; aksi halde kurgu kararları timeline'a uygulanamaz.");
            }

            if (editPlanStage != null && stages.All(x => x.StageType != StageType.Stt))
            {
                AddIssue(report, null, "Info", "workflow.editplan_without_stt", "EditPlan STT olmadan calisir; kelime zamanlari eklenirse vurgu ve caption kararları daha iyi olur.");
            }
        }

        private static void AddProductionProfileWorkflowHints(PipelineTemplateHealthDto report, IReadOnlyList<StageConfig> stages, ContentPipelineTemplate template)
        {
            var productionProfile = NormalizeProductionProfile(template.ProductionProfile);
            if (productionProfile != "LongForm") return;

            foreach (var requiredStage in new[]
            {
                StageType.Topic,
                StageType.Script,
                StageType.Image,
                StageType.Tts,
                StageType.Stt,
                StageType.EditPlan,
                StageType.SceneLayout,
                StageType.Render
            })
            {
                if (stages.All(x => x.StageType != requiredStage))
                {
                    AddIssue(report, null, "Error", "profile.longform.stage_missing", $"Long Form profile icin {requiredStage} stage'i gerekli.");
                }
            }

            if (stages.All(x => x.StageType != StageType.Storyboard))
            {
                AddIssue(report, null, "Warning", "profile.longform.storyboard_recommended", "Long Form icin Storyboard stage sahne ritmi, shot cesitliligi ve gorsel akicilik saglar.");
            }

            if (stages.All(x => x.StageType != StageType.CreativeDirector))
            {
                AddIssue(report, null, "Warning", "profile.longform.creative_director_recommended", "Long Form icin CreativeDirector stage video vaadi, ana soru, bolum yapisi ve retention stratejisini senaryoya aktarir.");
            }

            var scriptStage = report.Stages.FirstOrDefault(x => x.StageType == StageType.Script.ToString());
            if (scriptStage?.TargetDurationSec is int targetDuration && targetDuration >= 480 && targetDuration < 900)
            {
                AddIssue(report, scriptStage, "Info", "profile.longform.duration_v1", "Long Form v1 icin 8-15 dakika arasi iyi bir ilk hedef. 15+ dakika icin bolum sayisini ve gorsel cesitliligini artir.");
            }

            var renderStage = stages.FirstOrDefault(x => x.StageType == StageType.Render);
            if (renderStage != null && !renderStage.PresetId.HasValue)
            {
                AddIssue(report, report.Stages.FirstOrDefault(x => x.StageType == StageType.Render.ToString()), "Warning", "profile.longform.render_preset_missing", "Long Form profile icin Render stage'e explicit 16:9 preset secmek daha guvenli.");
            }

            if (stages.All(x => x.StageType != StageType.Upload))
            {
                AddIssue(report, null, "Info", "profile.longform.upload_optional", "Long Form v1 render odakli kurulabilir; Upload stage'i final kontrol sonrasi eklenebilir.");
            }

            if (stages.All(x => x.StageType != StageType.Thumbnail))
            {
                var severity = stages.Any(x => x.StageType == StageType.Upload) ? "Warning" : "Info";
                AddIssue(report, null, severity, "profile.longform.thumbnail_recommended", "Long Form icin Thumbnail stage kapak gorselini workflow ciktisi olarak netlestirir.");
            }
        }

        private static void AddRenderConsistencyHints(PipelineTemplateHealthDto report)
        {
            var layoutStage = report.Stages.FirstOrDefault(x => x.StageType == StageType.SceneLayout.ToString());
            var renderStage = report.Stages.FirstOrDefault(x => x.StageType == StageType.Render.ToString());

            if (layoutStage?.OutputWidth > 0 && renderStage?.OutputWidth > 0)
            {
                if (layoutStage.OutputWidth != renderStage.OutputWidth || layoutStage.OutputHeight != renderStage.OutputHeight)
                {
                    AddIssue(report, renderStage, "Warning", "render.layout_size_mismatch", "SceneLayout ve Render farkli output boyutlari kullaniyor. Render stage layout stilini override eder.");
                }
            }
        }

        private async Task<object?> LoadEntityAsync(Type entityType, int id, CancellationToken ct)
        {
            var repoType = typeof(IRepository<>).MakeGenericType(entityType);
            var repo = _serviceProvider.GetRequiredService(repoType);
            var method = repoType.GetMethod("GetByIdAsync", new[] { typeof(int), typeof(bool), typeof(CancellationToken) });
            if (method == null) return null;

            var task = (Task)method.Invoke(repo, new object[] { id, true, ct })!;
            await task.ConfigureAwait(false);
            return task.GetType().GetProperty("Result")?.GetValue(task);
        }

        private static void AddIssue(
            PipelineTemplateHealthDto report,
            PipelineTemplateHealthStageDto? stage,
            string severity,
            string code,
            string message,
            string? details = null)
        {
            var issue = new PipelineTemplateHealthItemDto
            {
                Severity = severity,
                Code = code,
                Message = message,
                Details = details,
                StageOrder = stage?.Order,
                StageType = stage?.StageType
            };

            report.Items.Add(issue);
            stage?.Issues.Add(issue);
        }

        private static void FinalizeHealth(PipelineTemplateHealthDto report)
        {
            report.ErrorCount = report.Items.Count(x => x.Severity == "Error");
            report.WarningCount = report.Items.Count(x => x.Severity == "Warning");
            report.InfoCount = report.Items.Count(x => x.Severity == "Info");
            report.IsRunnable = report.ErrorCount == 0;
            report.Status = report.ErrorCount > 0 ? "Error" : report.WarningCount > 0 ? "Warning" : "Healthy";

            foreach (var stage in report.Stages)
            {
                stage.Severity = stage.Issues.Any(x => x.Severity == "Error")
                    ? "Error"
                    : stage.Issues.Any(x => x.Severity == "Warning")
                        ? "Warning"
                        : "Healthy";
            }

            if (report.ErrorCount > 0)
                report.RecommendedNextSteps.Add("Once Error seviyesindeki stage/preset sorunlarini duzelt.");
            if (report.WarningCount > 0)
                report.RecommendedNextSteps.Add("Warning seviyesindeki uyumsuzluklari render almadan once gozden gecir.");
            if (report.IsRunnable)
                report.RecommendedNextSteps.Add("Workflow kosulabilir gorunuyor. Bir test run ile ciktiyi dogrula.");
        }

        private static int? ReadInt(object source, string propertyName)
        {
            var value = source.GetType().GetProperty(propertyName)?.GetValue(source);
            return value switch
            {
                int i => i,
                long l => (int)l,
                short s => s,
                _ => null
            };
        }

        private static string? ReadString(object source, string propertyName)
            => source.GetType().GetProperty(propertyName)?.GetValue(source)?.ToString();

        private static string? ReadEnumString(object source, string propertyName)
            => source.GetType().GetProperty(propertyName)?.GetValue(source)?.ToString();

        private static bool TryParseSize(string? size, out int width, out int height)
        {
            width = 0;
            height = 0;
            if (string.IsNullOrWhiteSpace(size)) return false;

            var parts = size.ToLowerInvariant().Split('x', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 2 && int.TryParse(parts[0], out width) && int.TryParse(parts[1], out height);
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            return values.Any(value => source.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        private static string BuildAspectRatio(int width, int height)
        {
            if (width <= 0 || height <= 0) return "unknown";
            var gcd = Gcd(width, height);
            return $"{width / gcd}:{height / gcd}";
        }

        private static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                var t = b;
                b = a % b;
                a = t;
            }

            return Math.Abs(a);
        }

        private sealed record ExecutorDescriptor(Type ExecutorType);
    }
}
