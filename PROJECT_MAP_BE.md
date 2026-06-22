# Infotainmentify Backend Proje Haritasi

Son guncelleme: 2026-06-21

Bu dosya backend tarafinda calisirken hizli yon bulmak icin tutulur. Yeni moduller, pipeline adimlari veya kritik mimari kararlar eklendikce guncellenmelidir.

## Amac

Backend, YouTube/video icerik uretim otomasyonu icin .NET 8 tabanli API ve pipeline motorudur. Temel sorumluluklari:

- Kullanici, JWT auth, rol ve aktiflik yonetimi.
- AI baglantilari, sosyal kanal bilgileri ve sifreli gizli veri saklama.
- Konsept, prompt, topic, script ve preset yonetimi.
- Pipeline template/run modeli ile otomatik video uretim surecini kosma.
- Gorsel, TTS, STT, scene layout, render ve upload adimlarini orkestre etme.
- SignalR ile job ilerlemesi ve run loglarini frontend'e canli iletme.
- EF Core + SQL Server ile kalici veri, soft delete ve migration yonetimi.

## Ana Klasorler

```text
Infotainmentify-BE/
  Infotainmentify.slnx
  Core/
  Application/
  Infrastructure/
  WebAPI/
  Plugins/
```

## Katmanlar

### Core

Domain merkezidir. Mumkun oldugunca framework bagimsiz kalmalidir.

- `Entity/`: domain varliklari.
  - `User/`: `AppUser`, `UserAiConnection`.
  - `Pipeline/`: `Concept`, `ContentPipelineTemplate`, `ContentPipelineRun`, `StageConfig`, `StageExecution`. `ContentPipelineTemplate.ProductionProfile` workflow hedefini (`Generic`, `Shorts`, `LongForm`, `Podcast`), `WorkflowLayoutJson` FE workflow canvas pozisyonlarini saklar.
  - `Presets/`: topic, script, image, tts, stt, render, video presetleri.
  - Ana tablolar: `Prompt`, `Topic`, `Script`, `SocialChannel`, `AssetFile`, `JobSetting`, `JobExecution`.
- `Enums/`: `StageType`, `StageStatus`, `ContentPipelineStatus`, `JobStatus`, `AiProviderType`, sosyal kanal ve asset enumlari.
- `Contracts/`: generic repository ve unit of work sozlesmeleri.
- `Abstractions/`: cross-layer sozlesmeler (`ICurrentUserService`, `ICurrentJobContext`, `ISoftDelete` vb.).
- `Attributes/`: executor ve preset baglantilarinda kullanilan attribute'lar.
- `Security/`: parola hashleme ve JWT token factory sozlesmesi.

### Application

Is kurallari, servisler, DTO'lar ve pipeline mantigi burada yasar.

- `Contracts/`: API request/response DTO'lari.
- `Services/`: auth, kullanici, konsept, prompt, topic, script, asset, social channel, job ve pipeline servisleri.
- `Services/PresetService/`: preset CRUD servisleri.
- `Services/Pipeline/`: pipeline template ve content pipeline servisleri.
- `Pipeline/ContentPipelineRunner.cs`: pipeline run orkestrasyonu, resume, retry, manual approval ve loglama.
- `Pipeline/PipelineContext.cs`: stage ciktilarini pipeline boyunca tasir.
- `Executors/`: stage bazli is yapan siniflar.
- `AiLayer/`: AI provider arayuzleri, factory ve provider implementasyonlari.
- `SocialPlatform/YouTubeUploaderService.cs`: YouTube upload entegrasyonu.
- `Validators/`: FluentValidation kurallari.
- `Mappers/`: entity <-> DTO donusumleri.
- `Options/`: config baglama modelleri (`JwtOptions`).

### Infrastructure

Veritabani, repository, migration ve altyapi kayitlari burada.

- `Persistence/AppDbContext.cs`: EF Core DbContext, configuration yukleme, soft delete query filter, timestamp stamp.
- `Persistence/EfRepository.cs`, `EfUnitOfWork.cs`: generic persistence adaptoru.
- `Configurations/`: entity mapping ve kolon/iliski ayarlari.
- `Migrations/`: EF Core migration gecmisi.
- `DependencyInjection.cs`: repository, unit of work, seeder ve Quartz kayitlari.
- `Security/JwtTokenFactory.cs`: JWT uretimi.
- `Job/`: arka plan job runner/context altyapisi.

### WebAPI

HTTP, SignalR ve uygulama bootstrap katmanidir.

- `Program.cs`: servis kayitlari, middleware, JWT, CORS, Swagger, SignalR ve static file setup.
- `Controllers/`: REST API yuzeyi.
- `Hubs/NotifyHub.cs`: SignalR hub, kullanici gruplari ve `run-{id}` log gruplari.
- `Service/SignalRNotifierService.cs`: `JobProgress`, `JobCompleted`, `ReceiveLog` event gonderimi.
- `Service/CurrentUserService.cs`: request kullanicisina erisim.
- `appsettings*.json`: connection string, JWT ve runtime config.
- `APP_FILES/` ve runtime dosya klasorleri: uretilen medya ve ara cikti dosyalari.

## Runtime Bootstrap

`WebAPI/Program.cs` uygulama girisidir.

Kaydedilen ana servis gruplari:

- SQL Server `AppDbContext`.
- Infrastructure: repository, unit of work, `DataSeeder`, Quartz.
- Application servisleri: auth, app user, presets, content services, pipeline runner/service, asset service.
- AI providerlari: `[AiProvider]` attribute'u ile otomatik taranir.
- Stage executorlari: `[StageExecutor]` attribute'u ile otomatik taranir.
- Upload servisleri: `ISocialPlatformService` implementasyonlari taranir.
- JWT bearer auth, DataProtection secret store, FluentValidation, Swagger.
- SignalR hub endpoint: `/hubs/notify`.
- CORS: development icin `localhost:5173/5174`, production icin `moduleer.com`.
- Development admin seed: `appsettings.Development.json` icindeki `Admin` bolumu `admin@local` / `admin` kullanicisini aktif Admin olarak onarir. `ResetPasswordOnStartup=true` oldugunda parola her acilista `ChangeMe!123` olarak yenilenir; kalici lokal sifre istenirse bu flag kapatilmali.

Not: `AppDbContext` hem `Program.cs` hem `Infrastructure.AddInfrastructure` icinde kayitli. DI davranisi degisirken bu cift kaydi akilda tutulmali.

## API Yuzeyi

Ana controller gruplari:

- Auth: `/api/auth`
- Users: `/api/app-users`
- AI connections: `/api/ai-connections`
- Social channels: `/api/social-channels`
- Concepts: `/api/Concepts`
- Prompts: `/api/Prompts`
- Topics: `/api/Topics`
- Scripts: `/api/Scripts`
- Presets: `/api/topic-presets`, `/api/script-presets`, `/api/image-presets`, `/api/tts-presets`, `/api/stt-presets`, `/api/render-presets`, `/api/video-presets`
- Pipeline templates: `/api/pipeline-templates`
  - `GET /api/pipeline-templates/{id}/health`: workflow/preset health raporu. Stage sirasi, dependency, executor, preset, AI connection, upload target ve render ayarlarini runtime oncesi denetler.
  - `PUT /api/pipeline-templates/{id}/workflow-layout`: workflow editor node pozisyonlarini `WorkflowLayoutJson` olarak kaydeder. Bu alan UI state'idir; pipeline execution sirasini degistirmez.
- Pipeline runs: `/api/pipeline-runs`
- Assets: `/api/assets`
- Files: `/api/files`
- Jobs: `/api/JobSettings`, `/api/JobExecutions`
- Notify test: `NotifyController`

## Pipeline Akisi

Pipeline genel olarak su modelle calisir:

1. `ContentPipelineTemplate` bir konsept icin stage tariflerini (`StageConfig`) tutar.
   - `WorkflowLayoutJson` sadece editor/canvas yerlesim bilgisidir. Runner bu alani okumaz; calisma sirasi yine `StageConfig.Order` uzerindendir.
2. `ContentPipelineRun` template'ten baslatilir.
3. `ContentPipelineRunner` stage'leri `Order` sirasina gore kosar.
4. Her stage icin `StageExecution` kaydi tutulur.
5. Stage ciktisi JSON olarak `OutputJson` alanina yazilir ve `PipelineContext` icine hydrate edilir.
6. Basarisiz stage maksimum 3 kez retry edilir.
7. Render sonrasi siradaki stage `Upload` ise:
   - `AutoPublish = true`: upload'a devam edilir.
   - `AutoPublish = false`: run `WaitingForApproval` durumunda durur.
8. Frontend onay verirse upload veya sonraki adim devam eder.

Aktif attribute tabanli stage executorleri:

- `TopicStageExecutor` -> `StageType.Topic`
- `ScriptStageExecutor` -> `StageType.Script`
- `ImageStageExecutor` -> `StageType.Image`
- `TtsStageExecutor` -> `StageType.Tts`
- `SttStageExecutor` -> `StageType.Stt`
- `SceneLayoutStageExecutor` -> `StageType.SceneLayout`
- `RenderStageExecutor` -> `StageType.Render`
- `ThumbnailStageExecutor` -> `StageType.Thumbnail`
- `UploadStageExecutor` -> `StageType.Upload`

`VideoStageExecutor` ve `ContentPlanStageExecutor` dosyalari var, fakat mevcut otomatik kayit akisi `[StageExecutor]` attribute'una baktigi icin aktif davranis kontrol edilmeden varsayilmamalidir.

Workflow health notu:

- `PipelineTemplateService.GetHealthAsync` mevcut `ContentPipelineTemplate`, `StageConfig`, preset tablolar, `UserAiConnection` ve `SocialChannel` kayitlarini okur.
- Health sonucu `Error`, `Warning`, `Info` ve `Healthy` seviyeleriyle hem genel rapor hem stage bazli issue listesi dondurur.
- Dependency kurallari bugunku executor beklentilerine gore tanimlidir: `Script -> Topic`, `Image -> Script`, `Thumbnail -> Script`, `Tts -> Script`, `Stt -> Tts`, `SceneLayout -> Script/Image/Tts`, `Render -> SceneLayout`, `Upload -> Render`.
- `SceneLayout` stage'i config icin `RenderPreset` kullanir; `Render` preset olmadan cached layout stiliyle calisabilir ama explicit preset secmek daha okunurdur.
- `Thumbnail` stage'i `ImagePreset` kullanir; script title/ilk sahne fikrinden 16:9 kapak gorseli uretir ve `ThumbnailStagePayload` ile dosya path/URL dondurur.
- `Upload` stage health'i `OptionsJson` icindeki target'lari parse eder, sosyal kanal sahipligi ve token varligini kontrol eder.
- `ProductionProfile = LongForm` iken health ek long-video kontrolleri calistirir:
  - Topic, Script, Image, Tts, Stt, SceneLayout ve Render stage'leri beklenir.
  - Script preset hedef suresi 480 sn altindaysa warning uretir; prompt icinde chapter/section/intro/outro gibi bolumlu yapi ipucu arar.
  - Image preset portrait ise warning uretir.
  - Render/SceneLayout presetleri 16:9 degilse error, 720p alti/fps/bitrate dusukse warning uretir.
  - Thumbnail stage yoksa bilgi/warning uretir; Upload varsa kapak eksikligi daha onemli kabul edilir.

Workflow layout notu:

- `WorkflowLayoutJson` JSON parse edilerek normalize edilir; gecersiz JSON `PUT /workflow-layout` endpoint'inde `400` dondurur.
- Son migrationlar:
  - `20260620211926_WorkflowLayoutJson`: `ContentPipelineTemplates` tablosuna nullable `WorkflowLayoutJson` kolonu ekler.
  - `20260620215008_ProductionProfile`: `ContentPipelineTemplates` tablosuna default `Generic` degerli `ProductionProfile` kolonu ekler.

Script stage notu:

- `ScriptStageExecutor` AI cevabinda hem metadata tasiyan JSON object (`title`, `description`, `tags`, `scenes`) hem de direkt scene array formatini kabul eder.
- Scene alanlari case-insensitive okunur; `audio`, `audioText`, `voiceover`, `narration`, `visual`, `visualPrompt`, `imagePrompt`, `durationSec` gibi yaygin varyasyonlar normalize edilir.
- Script preset hedef suresi artik 15-3600 sn araliginda kaydedilebilir; long video presetleri icin 480-900 sn ilk hedef olarak daha uygundur.

## SignalR Akisi

- Hub: `/hubs/notify`
- Auth: JWT access token query string uzerinden de desteklenir.
- User group: `user-{userId}`
- Run log group: `run-{runId}`
- Eventler:
  - `JobProgress`
  - `JobCompleted`
  - `ReceiveLog`

`SignalRNotifierService.SendLogAsync(runId, message)` run grubuna canli log gonderir. Frontend `LiveLogViewer` bu gruba `JoinRunGroup` ile katilir.

## Dosya ve Medya Akisi

- Runtime static dosyalar `ALL_FILES` path'i uzerinden servis edilir.
- `WebAPI/APP_FILES/` altinda mevcut uretilmis/user dosyalari bulunuyor.
- Asset ve render akislari dosya path/URL bilgilerini stage payload'lari icinde tasir.
- `RenderStagePayload` final video URL/path yaninda width, height, fps ve aspect ratio metadata'si tasir; FE preview bu bilgiyle 9:16 ve 16:9 formatlari ayirir.
- Render servisi SceneLayout payload'inda eski/yanlis base path tasiyan image/audio dosyalarini render run klasorundeki `images` ve `audio` altindan dosya adiyla yeniden cozer; bu retry/re-render senaryolarinda path drift hatalarini azaltir.
- `ThumbnailStagePayload` kapak gorselinin fiziksel path, public URL, prompt ve boyut bilgisini tasir. Upload stage bu payload varsa `SocialMetadata.ThumbnailPath` alanina aktarir.
- Buyuk medya dosyalarina dokunmadan once kaynagin user-generated olup olmadigi kontrol edilmeli.

## Render Notlari

- Render motoru `Application/Services/FFmpegVideoService.cs` uzerinden FFmpeg komutu uretir.
- ASS altyazi canvas'i render presetinden gelen layout width/height degerlerine gore uretilir; 1080x1920 sabitine bagli kalmamali.
- Subtitle fontlari video klasoru yerine render'a ozel gecici font klasorunden verilir; FFmpeg'in `.txt`, `.ass`, `.mp4` dosyalarini font gibi taramasi engellenir.
- Video encoder once `h264_nvenc` dener; donanim/driver destegi yoksa `libx264` ile CPU fallback yapar.
- Uzun video render'inda sahne sayisi veya FFmpeg komut uzunlugu riskli seviyeye cikarsa render otomatik chunk moduna gecer: sahneler parca MP4'ler halinde render edilir, sonra concat ile final video olusturulur.
- `RenderVisualEffectsSettings` icinde `EnableAutoBroll`, `MinSceneDurationForBrollSec`, `BrollSegmentDurationSec`, `MaxBrollCutsPerScene` alanlari vardir.
- Auto B-roll v1 gercek ekstra AI gorsel uretmez; `SceneLayoutStageExecutor` uzun sahneleri ayni gorsel uzerinde 8-10 sn civari visual beat'lere boler. `FFmpegVideoService` bu beat'lere `zoom_in`, `zoom_out`, `pan_left/right/up/down` hareketleri uygular.
- Gercek B-roll v2 icin script parser'in sahne basina ek `brollPrompts` okumasina, Image stage'in opsiyonel ek gorseller uretmesine ve layout'un bu gorselleri primary gorsellerle karistirmasina ihtiyac var.
- `RenderStageExecutor` render sonucunda `RenderStagePayload` icine width, height, fps, aspect ratio, sure ve dosya boyutu yazar.

## Gelistirme Komutlari

Backend kokunden:

```powershell
dotnet restore
dotnet build Infotainmentify.slnx
dotnet run --project WebAPI/WebAPI.csproj
```

Migration komutlari:

```powershell
dotnet ef migrations add MigrationName -p Infrastructure -s WebAPI
dotnet ef database update -p Infrastructure -s WebAPI
```

## Yeni Ozellik Eklerken Rota

- Yeni domain kavrami: once `Core/Entity`, `Core/Enums` ve gerekirse abstraction/contract.
- Veri saklama: `Infrastructure/Configurations`, migration ve repository servisleri.
- API DTO: `Application/Contracts`.
- Is mantigi: `Application/Services`.
- Mapping: `Application/Mappers`.
- Validasyon: `Application/Validators`.
- Endpoint: `WebAPI/Controllers`.
- Pipeline adimi: `Core.Enums.StageType`, payload model, executor, preset/config ve FE stage listesi beraber guncellenmeli.
- Yeni AI provider: `[AiProvider]` attribute'u ve `IAiGenerator` ailesiyle uyumlu implementasyon.
- Yeni upload platformu: `ISocialPlatformService` implementasyonu.

## Dikkat Notlari

- Auth ve SignalR ayni JWT mekanizmasina bagli.
- Soft delete `BaseEntity` + `ISoftDelete` uzerinden merkezi uygulanir.
- Stage output JSON isimleri `ContentPipelineRunner.HydrateContext` icindeki convention'a bagli: `{StageType}StagePayload`.
- `DataProtectionSecretStore` gizli bilgileri korur; AI/social credential alanlarinda plain text kalmamali.
- Frontend enum stringleri backend enumlariyla birebir uyumlu kalmali.
- Kodda bazi eski yorumlarda encoding bozulmalari var; yeni dokuman ve yorumlarda mumkunse temiz ASCII veya tutarli UTF-8 tercih edilmeli.
