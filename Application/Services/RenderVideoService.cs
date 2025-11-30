using Application.Abstractions;
using Application.Contracts.Script;
using Application.Models;
using Core.Contracts;
using Core.Entity;
using Core.Entity.User;
using Core.Enums;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Application.Services
{
    public class RenderVideoService
    {
        private readonly IFFmpegService _ffmpeg;
        private readonly IRepository<ContentPipelineRun_> _pipelineRepo;
        private readonly IRepository<AutoVideoAssetFile> _assetFileRepo;
        private readonly IRepository<VideoGenerationProfile> _videoGenerationProfileRepo;
        private readonly IUserDirectoryService _dir;
        private readonly INotifierService _notifier;
        private readonly IUnitOfWork _uow;
        private readonly IRepository<AppUser> _appUser;

        public RenderVideoService(
            IFFmpegService ffmpeg,
            IRepository<ContentPipelineRun_> pipelineRepo,
            IRepository<AutoVideoAssetFile> assetFileRepo,
            IRepository<VideoGenerationProfile> videoGenerationProfileRepo,
            IUserDirectoryService dir,
            INotifierService notifier,
            IUnitOfWork uow,
            IRepository<AppUser> appUser)
        {
            _ffmpeg = ffmpeg;
            _pipelineRepo = pipelineRepo;
            _assetFileRepo = assetFileRepo;
            _videoGenerationProfileRepo = videoGenerationProfileRepo;
            _dir = dir;
            _notifier = notifier;
            _uow = uow;
            _appUser = appUser;
        }

        // ====================================================================
        //                          RENDER PIPELINE
        // ====================================================================
        public async Task<string> RenderVideoAsync(int pipelineId, CancellationToken ct = default)
        {
            // ---------------------------------------
            // LOAD PIPELINE / USER
            // ---------------------------------------
            var pipeline = await _pipelineRepo.FirstOrDefaultAsync(
                x => x.Id == pipelineId ,
                include: q => q
                    .Include(x => x.Profile)
                        .ThenInclude(p => p.AutoVideoRenderProfile)
                    .Include(x => x.Profile)
                        .ThenInclude(p => p.ScriptGenerationProfile)
                    .Include(x => x.Profile)
                    .Include(x => x.Script),
                asNoTracking: true,
                ct: ct
            )
                ?? throw new InvalidOperationException("Pipeline bulunamadı.");


            var user = await _appUser.GetByIdAsync(pipeline.AppUserId, false, ct)
                ?? throw new InvalidOperationException("Kullanıcı bulunamadı.");

            if (pipeline.Script == null)
                throw new InvalidOperationException("Script bulunamadı.");

            var dto = JsonSerializer.Deserialize<ScriptContentDto>(pipeline.Script.Content)
                ?? throw new InvalidOperationException("Script ContentDto parse edilemedi.");

            // ---------------------------------------
            // ASSET ROOTS
            // ---------------------------------------
            var root = _dir.GetVideoPipelineRoot(user, pipeline.Id);
            var audioPath = Path.Combine(root, "audio/narration.mp3");
            var sttWordsPath = Path.Combine(root, "stt/words.json");
            var imageDir = Path.Combine(root, "images");
            var videoDir = Path.Combine(root, "videos");
            var finalDir = _dir.GetPipelineFinalRoot(user, pipeline.Id);

            Directory.CreateDirectory(finalDir);

            // ---------------------------------------
            // STT LOAD (word timestamps)
            // ---------------------------------------
            var words = JsonSerializer.Deserialize<List<WordTimestamp>>(
                await File.ReadAllTextAsync(sttWordsPath, ct))
                ?? new List<WordTimestamp>();

            // ---------------------------------------
            // DURATION
            // ---------------------------------------
            var duration = await _ffmpeg.GetAudioDurationAsync(audioPath, ct)
                           ?? throw new Exception("Ses süresi alınamadı.");

            // ---------------------------------------
            // VISUAL SEQUENCE BUILD
            // ---------------------------------------
            var visualPaths = new List<string>();

            visualPaths.AddRange(Directory.GetFiles(imageDir).OrderBy(x => x));
            visualPaths.AddRange(Directory.GetFiles(videoDir).OrderBy(x => x));

            if (!visualPaths.Any())
                throw new Exception("Render için görsel/video asset bulunamadı.");

            double slot = duration / visualPaths.Count;

            // ---------------------------------------
            // CAPTION (ASS) FILE
            // ---------------------------------------
            var assPath = Path.Combine(finalDir, "captions.ass");
            await GenerateCaptionAssAsync(words, assPath, dto, pipeline.Profile.AutoVideoRenderProfile!);

            // ---------------------------------------
            // VIDEO RENDER (filter_complex)
            // ---------------------------------------
            var finalFile = Path.Combine(finalDir, $"final_{DateTime.Now:yyyyMMddHHmmss}.mp4");

            await _ffmpeg.RenderTimelineAsync(
                visualPaths,
                audioPath,
                assPath,
                pipeline.Profile.AutoVideoRenderProfile!,
                finalFile,
                ct
            );

            // ---------------------------------------
            // UPDATE DB
            // ---------------------------------------
            pipeline.VideoPath = finalFile.Replace("\\", "/");
            //pipeline.Status = ContentPipelineStatus.Rendered;

            await _uow.SaveChangesAsync(ct);

            await _notifier.JobCompletedAsync(user.Id, pipelineId, true, "🎉 Video render tamamlandı!");

            return finalFile;
        }

        private async Task GenerateCaptionAssAsync(
            List<WordTimestamp> words,
            string assPath,
            ScriptContentDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("[Script Info]");
            sb.AppendLine("PlayResX:1080");
            sb.AppendLine("PlayResY:1920");
            sb.AppendLine("ScaledBorderAndShadow:yes");

            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine("Format: Name, Fontname, Fontsize, PrimaryColour, OutlineColour, BackColour, BorderStyle, Outline, Shadow, Alignment, MarginL, MarginR, MarginV, Encoding");

            // Viral Caption Style
            sb.AppendLine("Style: Default,Arial,48,&H00FFFFFF,&H00000000,&H40000000,1,4,0,2,40,40,200,1");
            sb.AppendLine();

            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            foreach (var w in words)
            {
                var start = TimeSpan.FromSeconds(w.Start);
                var end = TimeSpan.FromSeconds(w.End);

                sb.AppendLine(
                    $"Dialogue: 0,{ToAss(start)},{ToAss(end)},Default,,0,0,80,,{{\\fad(80,80)\\bord4\\shad0}}{w.Word}"
                );
            }

            await File.WriteAllTextAsync(assPath, sb.ToString());
        }

        private async Task GenerateCaptionAssAsync(
      List<WordTimestamp> words,
      string assPath,
      ScriptContentDto dto,
      AutoVideoRenderProfile p)
        {
            var sb = new StringBuilder();

            // ---------------------------------------------------------
            // HEADER
            // ---------------------------------------------------------
            sb.AppendLine("[Script Info]");
            sb.AppendLine($"PlayResX:{p.Resolution.Split('x')[0]}");
            sb.AppendLine($"PlayResY:{p.Resolution.Split('x')[1]}");
            sb.AppendLine("ScaledBorderAndShadow:yes");
            sb.AppendLine();

            // ---------------------------------------------------------
            // STYLE SECTION
            // ---------------------------------------------------------
            sb.AppendLine("[V4+ Styles]");
            sb.AppendLine(
                "Format: Name, Fontname, Fontsize, PrimaryColour, SecondaryColour, "
                + "OutlineColour, BackColour, Bold, Italic, Underline, StrikeOut, ScaleX, "
                + "ScaleY, Spacing, Angle, BorderStyle, Outline, Shadow, Alignment, "
                + "MarginL, MarginR, MarginV, Encoding"
            );

            // -------- Colors --------
            string textColor = "&H00FFFFFF"; // white
            string highlightColor = ColorToAss(p.CaptionHighlightColor);

            // outline = stroke
            string outlineColor = "&H00000000";

            // background opacity → (ASS = AABBGGRR)
            int bgAlpha = (int)(p.CaptionBackgroundOpacity * 255);
            string bgColor = $"&H{bgAlpha:X2}000000";

            // align mapping
            int align = p.CaptionPosition switch
            {
                CaptionPositionTypes.Top => 8,
                CaptionPositionTypes.Center => 5,
                CaptionPositionTypes.Bottom => 2,
                _ => 2
            };

            // --- STYLE LINE ---
            sb.AppendLine(
                $"Style: SUB,{p.CaptionFont},{p.CaptionSize}," +
                $"{textColor},{highlightColor},{outlineColor},{bgColor}," +
                $"0,0,0,0,100,100,0,0,1," +
                $"{p.CaptionOutlineSize},{p.CaptionShadowSize}," +
                $"{align},20,20,{p.CaptionMarginV},1"
            );
            sb.AppendLine();


            // ---------------------------------------------------------
            // EVENTS HEADER
            // ---------------------------------------------------------
            sb.AppendLine("[Events]");
            sb.AppendLine("Format: Layer, Start, End, Style, Name, MarginL, MarginR, MarginV, Effect, Text");

            // ---------------------------------------------------------
            // CHUNKING (2 kelime – render profile)
            // ---------------------------------------------------------
            var chunks = ChunkWords(words, p.CaptionChunkSize);

            foreach (var chunk in chunks)
            {
                double start = chunk.Min(x => x.Start);
                double end = chunk.Max(x => x.End);

                var tsStart = TimeSpan.FromSeconds(start);
                var tsEnd = TimeSpan.FromSeconds(end);

                // CHUNK TEXT
                string text =
                    p.CaptionKaraoke
                    ? BuildKaraokeText(chunk, p)       // syllable highlight
                    : BuildNormalChunk(chunk, p);      // sadece glow highlight


                // ---------------------------------------------------------
                // ANIMATIONS
                // ---------------------------------------------------------
                var anim = new StringBuilder();

                // 🔥 GLOW
                if (p.CaptionGlow)
                {
                    string glow = ColorToAss(p.CaptionGlowColor);
                    anim.Append($"\\3c{glow}\\blur{p.CaptionGlowSize}");
                }

                // 🔥 TikTok bounce (0 → 200ms)
                anim.Append("\\t(0,200,\\fscx110\\fscy110)");

                // ---------------------------------------------------------
                // FINAL DIALOGUE LINE
                // ---------------------------------------------------------
                sb.AppendLine(
                    $"Dialogue: 0,{ToAss(tsStart)},{ToAss(tsEnd)},SUB,,0,0,{p.CaptionMarginV},," +
                    $"{{{anim}\\fs{p.CaptionSize}\\bord{p.CaptionOutlineSize}\\shad{p.CaptionShadowSize}}}{text}"
                );
            }

            await File.WriteAllTextAsync(assPath, sb.ToString());
        }



        private static string ToAss(TimeSpan t)
        {
            return $"{(int)t.TotalHours:00}:{t.Minutes:00}:{t.Seconds:00}.{t.Milliseconds / 10:00}";
        }

        private static string ColorToAss(string hex)
        {
            hex = hex.Replace("#", "");
            if (hex.Length != 6) return "&H00FFFFFF";

            string b = hex.Substring(4, 2);
            string g = hex.Substring(2, 2);
            string r = hex.Substring(0, 2);

            return $"&H00{b}{g}{r}";
        }

        private static List<List<WordTimestamp>> ChunkWords(List<WordTimestamp> words, int size)
        {
            var chunks = new List<List<WordTimestamp>>();
            for (int i = 0; i < words.Count; i += size)
                chunks.Add(words.Skip(i).Take(size).ToList());
            return chunks;
        }

        private static string BuildNormalChunk(List<WordTimestamp> chunk, AutoVideoRenderProfile p)
        {
            return string.Join(" ", chunk.Select(x => x.Word.ToUpper(CultureInfo.InvariantCulture)));
        }

        // karaoke-style progressive highlight
        private static string BuildKaraokeText(List<WordTimestamp> chunk, AutoVideoRenderProfile p)
        {
            var sb = new StringBuilder();

            foreach (var w in chunk)
            {
                double dur = w.End - w.Start;
                int k = (int)(dur * 100); // ASS \k units = centiseconds

                sb.Append($"{{\\1c{ColorToAss(p.CaptionHighlightColor)}\\k{k}}}{w.Word.ToUpper(CultureInfo.InvariantCulture)} ");
            }

            return sb.ToString().Trim();
        }

        private string BuildBounceAnimation()
        {
            return
                "\\t(0,120,\\fscx130\\fscy130)" +
                "\\t(120,200,\\fscx95\\fscy95)" +
                "\\t(200,260,\\fscx100\\fscy100)";
        }


    }
}
