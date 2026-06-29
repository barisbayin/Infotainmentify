# Infotainmentify Backend Proje Haritasi

Son guncelleme: 2026-06-28

Bu dosya backend tarafinda calisirken hizli yon bulmak icin tutulur. Yeni moduller, pipeline adimlari veya kritik mimari kararlar eklendikce guncellenmelidir.

Long-form konsept + brief + wizard donusum plani: `../LONG_FORM_SYSTEM_CONVERSION_PLAN.md`

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
  - `Pipeline/`: `Concept`, `ProductionConceptProfile`, `SavedProductionBrief`, `ContentPipelineTemplate`, `ContentPipelineRun`, `StageConfig`, `StageExecution`. `ProductionConceptProfile` konseptin kalici uretim kimligini; `SavedProductionBrief` uretimden once kaydedilen run brief'lerini tutar. `ContentPipelineTemplate.ProductionProfile` workflow hedefini (`Generic`, `Shorts`, `LongForm`, `Podcast`), `WorkflowLayoutJson` FE workflow canvas pozisyonlarini saklar.
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
- `Services/ProductionPromptContext.cs`: run snapshot'indeki concept profile'i prompt context bloklarina cevirir; Topic, CreativeDirector, Script, Storyboard, Image ve Thumbnail akislari bu helper ile konsept DNA'sini kullanir.
- `Services/ProductionTarget.cs`: production brief, concept default sure ve preset fallback'ini tek hedef kontratina cevirir; saniye, kelime butcesi ve ideal/min/max sahne sayisi Topic, CreativeDirector ve Script tarafinda ayni kaynaktan kullanilir.
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
  - `GET /api/concepts/{id}/profile`: konsept icin kalici uretim profili dondurur. Kayit yoksa long-form varsayilanlariyla taslak DTO dondurur.
  - `PUT /api/concepts/{id}/profile`: kitle, ton, kanal vaadi, gorsel stil bible, karakter bible, metin politikasi, default sure, default workflow ve review policy alanlarini upsert eder.
- Prompts: `/api/Prompts`
- Topics: `/api/Topics`
- Scripts: `/api/Scripts`
- Presets: `/api/topic-presets`, `/api/script-presets`, `/api/image-presets`, `/api/tts-presets`, `/api/stt-presets`, `/api/render-presets`, `/api/video-presets`
- Pipeline templates: `/api/pipeline-templates`
  - `GET /api/pipeline-templates/{id}/health`: workflow/preset health raporu. Stage sirasi, dependency, executor, preset, AI connection, upload target ve render ayarlarini runtime oncesi denetler.
  - `PUT /api/pipeline-templates/{id}/workflow-layout`: workflow editor node pozisyonlarini `WorkflowLayoutJson` olarak kaydeder. Bu alan UI state'idir; pipeline execution sirasini degistirmez.
- Pipeline runs: `/api/pipeline-runs`
  - `POST /api/pipeline-runs`: `CreatePipelineRunRequest.Brief` alani opsiyonel production brief tasir. `SavedBriefId` verilirse kayitli brief okunur; `Brief` doluysa run snapshot'i icin formdaki anlik degerler onceliklidir. Long-form akista kullanici ana baslik, aci/tez, hedef izleyici, hedef sure, mutlaka islenecekler, kacinilacaklar ve not/kaynak bilgisi verebilir.
  - `POST /api/pipeline-runs/{id}/cancel`: aktif/bekleyen run'i `Cancelled` durumuna alir ve process icindeki cancellation token'i iptal eder.
- Production wizard: `/api/production-wizard`
  - `GET /api/production-wizard/bootstrap`: concepts, selected concept profile, concept templates, saved briefs, selected template health ve ilk preflight sonucunu tek payload'da dondurur.
  - `POST /api/production-wizard/preflight`: concept/template/brief/render gate secimini hata-warning-info olarak denetler.
  - `POST /api/production-wizard/start`: preflight temizse mevcut `CreatePipelineRunRequest` modeline map edip run olusturur veya baslatir.
- Production briefs: `/api/production-briefs`
  - Kayitli brief CRUD yuzeyidir. Brief kullaniciya aittir, konsept opsiyoneldir.
  - Run baslatirken bu kayitlardan biri secilebilir; run icinde yine `InputBriefJson` snapshot olarak saklanir, sonradan brief degisirse eski uretim bozulmaz.
- Assets: `/api/assets`
- Files: `/api/files`
- Jobs: `/api/JobSettings`, `/api/JobExecutions`
- Notify test: `NotifyController`

## Pipeline Akisi

Pipeline genel olarak su modelle calisir:

1. `ContentPipelineTemplate` bir konsept icin stage tariflerini (`StageConfig`) tutar.
   - `WorkflowLayoutJson` sadece editor/canvas yerlesim bilgisidir. Runner bu alani okumaz; calisma sirasi yine `StageConfig.Order` uzerindendir.
2. `ContentPipelineRun` template'ten baslatilir.
    - Run seviyesinde `InputBriefJson` varsa bu alan uretimin ana brief'idir. Bu deger kullanicinin anlik formundan veya `SavedProductionBrief` kaydindan gelebilir. `RunContextTitle` brief ana basligindan doldurulur.
    - Run seviyesinde `InputConceptProfileJson` varsa bu alan konseptin baslangic anindaki uretim kimligi snapshot'idir. Sonradan Concept Studio degisse bile eski run ayni baglamla incelenebilir.
    - `ProductionPromptContext` bu snapshot'i Topic, CreativeDirector, Script, Storyboard, Image, Thumbnail ve manuel image regenerate promptlarina tasir.
3. `ContentPipelineRunner` stage'leri `Order` sirasina gore kosar.
4. Her stage icin `StageExecution` kaydi tutulur.
5. Stage ciktisi JSON olarak `OutputJson` alanina yazilir ve `PipelineContext` icine hydrate edilir.
6. Basarisiz stage maksimum 3 kez retry edilir.
7. Render sonrasi siradaki stage `Upload` ise:
   - `AutoPublish = true`: upload'a devam edilir.
   - `AutoPublish = false`: run `WaitingForApproval` durumunda durur.
8. Frontend onay verirse upload veya sonraki adim devam eder.
9. Kullanici run'i durdurursa run `Cancelled`, aktif stage `Cancelled` olur. Token destekleyen AI/render/bekleme islemleri iptal sinyali alir; uretilmis ara dosyalar silinmez.

Aktif attribute tabanli stage executorleri:

- `TopicStageExecutor` -> `StageType.Topic`
- `CreativeDirectorStageExecutor` -> `StageType.CreativeDirector`
- `ScriptStageExecutor` -> `StageType.Script`
- `StoryboardStageExecutor` -> `StageType.Storyboard`
- `ImageStageExecutor` -> `StageType.Image`
- `TtsStageExecutor` -> `StageType.Tts`
- `SttStageExecutor` -> `StageType.Stt`
- `EditPlanStageExecutor` -> `StageType.EditPlan`
- `SceneLayoutStageExecutor` -> `StageType.SceneLayout`
- `RenderStageExecutor` -> `StageType.Render`
- `ThumbnailStageExecutor` -> `StageType.Thumbnail`
- `UploadStageExecutor` -> `StageType.Upload`

`VideoStageExecutor` ve `ContentPlanStageExecutor` dosyalari var, fakat mevcut otomatik kayit akisi `[StageExecutor]` attribute'una baktigi icin aktif davranis kontrol edilmeden varsayilmamalidir.

Workflow health notu:

- `PipelineTemplateService.GetHealthAsync` mevcut `ContentPipelineTemplate`, `StageConfig`, preset tablolar, `UserAiConnection` ve `SocialChannel` kayitlarini okur.
- Health sonucu `Error`, `Warning`, `Info` ve `Healthy` seviyeleriyle hem genel rapor hem stage bazli issue listesi dondurur.
- Dependency kurallari bugunku executor beklentilerine gore tanimlidir: `CreativeDirector -> Topic`, `Script -> Topic`, `Storyboard -> Script`, `Image -> Script`, `Thumbnail -> Script`, `Tts -> Script`, `Stt -> Tts`, `EditPlan -> Script/Image/Tts`, `SceneLayout -> Script/Image/Tts`, `Render -> SceneLayout`, `Upload -> Render`.
- `SceneLayout` stage'i config icin `RenderPreset` kullanir; `Render` preset olmadan cached layout stiliyle calisabilir ama explicit preset secmek daha okunurdur.
- `Thumbnail` stage'i `ImagePreset` kullanir; script title/ilk sahne fikrinden 16:9 kapak gorseli uretir ve `ThumbnailStagePayload` ile dosya path/URL dondurur.
- `Upload` stage health'i `OptionsJson` icindeki target'lari parse eder, sosyal kanal sahipligi ve token varligini kontrol eder.
- `ProductionProfile = LongForm` iken health ek long-video kontrolleri calistirir:
  - Topic, Script, Image, Tts, Stt, EditPlan, SceneLayout ve Render stage'leri beklenir.
  - CreativeDirector ve Storyboard stage'leri zorunlu degildir ama uzun videoda video vaadi, ana soru, bolum yapisi, shot cesitliligi ve gorsel akicilik icin warning seviyesinde onerilir.
  - Script preset hedef suresi 480 sn altindaysa warning uretir; prompt icinde chapter/section/intro/outro gibi bolumlu yapi ipucu arar.
  - Image preset portrait ise warning uretir.
  - Render/SceneLayout presetleri 16:9 degilse error, 720p alti/fps/bitrate dusukse warning uretir.
  - Thumbnail stage yoksa bilgi/warning uretir; Upload varsa kapak eksikligi daha onemli kabul edilir.

Workflow layout notu:

- `WorkflowLayoutJson` JSON parse edilerek normalize edilir; gecersiz JSON `PUT /workflow-layout` endpoint'inde `400` dondurur.
- Son migrationlar:
  - `20260620211926_WorkflowLayoutJson`: `ContentPipelineTemplates` tablosuna nullable `WorkflowLayoutJson` kolonu ekler.
  - `20260620215008_ProductionProfile`: `ContentPipelineTemplates` tablosuna default `Generic` degerli `ProductionProfile` kolonu ekler.
  - `20260627215013_ProductionConceptProfiles`: `ProductionConceptProfiles` tablosunu ve `ContentPipelineRuns.InputConceptProfileJson` snapshot kolonunu ekler.

Script stage notu:

- `CreatePipelineRunRequest.Brief` doluysa Topic ve Script stage'leri bu brief'e baglanir. Preset yaratici stratejiyi belirler; run brief'i videonun ne hakkinda oldugunu sabitler.
- Topic preset prompt placeholder'lari `{MainTitle}`, `{BriefTitle}`, `{Angle}`, `{Audience}`, `{TargetDuration}`, `{MustCover}`, `{Avoid}`, `{Notes}`, `{Language}`, `{ModelName}` ve concept token'lari (`{ConceptProfile}`, `{ConceptName}`, `{ChannelPromise}`, `{ConceptAudience}`, `{ConceptTone}`, `{StyleBible}`, `{CharacterBible}`, `{TextPolicy}`, `{ContentRules}`, `{DefaultDurationSec}`) ile beslenir.
- Topic stage brief'i `TopicStagePayload.TopicText` icinde production-ready topic document haline getirir: title, premise, audience promise, central question, angle, key points, chapter hints, visual direction ve avoid notes.
- Script stage prompt'u her zaman JSON object kontrati ekler ve `SCENE GENERATION CONTRACT` ile topic document + production brief + concept profile'i sahne uretimine tasir.
- `ProductionTarget` kontrati Script stage'e hedef sure, minimum/ideal/maksimum konusma kelimesi ve sahne araligi olarak eklenir; 10-12 dakika gibi brief girdileri tek merkezde normalize edilir.
- Script AI cevabi long-form hedefe gore kisa kalirsa `ScriptStageExecutor` ayni JSON kontratiyla tek otomatik repair/genisletme denemesi yapar. Repair sonrasi da kisa kalirsa stage erken hata verir.
- Script sahne `EstimatedDuration` degerleri audioText kelime sayisina gore normalize edilir; bu deger Storyboard'un uzun sahnelerde 2-3 visual beat uretebilmesine yardim eder.
- `ScriptStageExecutor` AI cevabinda hem metadata tasiyan JSON object (`title`, `description`, `tags`, `scenes`) hem de direkt scene array formatini kabul eder.
- Scene alanlari case-insensitive okunur; `audio`, `audioText`, `voiceover`, `narration`, `visual`, `visualPrompt`, `imagePrompt`, `durationSec` gibi yaygin varyasyonlar normalize edilir.
- Scene Direction V2 alanlari `ScriptSceneItem` uzerinde tasinir: `sceneRole`, `scenePurpose`, `viewerQuestion`, `emotionalBeat`, `visualType`, `cameraPlan`, `overlayText`, `sfxCue`, `transitionIntent`, `chapterTitle`.
- `ScriptStageExecutor` bu alanlari prompt kontratinda zorunlu ister; AI eksik donerse guvenli defaultlarla normalize eder.
- `VisualVarietyEngine`, Script parse edildikten sonra `visualType`, `visualVarietyRole` ve `visualVarietyReason` alanlarini normalize eder; arka arkaya ayni gorsel tipi kirar ve uzun videoda 30-45 sn bandinda map/timeline/diagram/comparison gibi bilgi gorselleri hedefler.
- Script preset hedef suresi artik 15-3600 sn araliginda kaydedilebilir; long video presetleri icin 480-900 sn ilk hedef olarak daha uygundur.
- Pipeline health, LongForm Topic/Script presetlerinde eski prompt risklerini yakalar: topic fikir listesi dili, brief placeholder eksigi, eski JSON shape'e kilitlenen script promptlari, cok az/cok fazla sahne zorlamasi ve script sahnesini gorsel beat'e karistiran talimatlar.

CreativeDirector / Storyboard stage notu:

- `CreativeDirectorStageExecutor`, `TopicStagePayload` ve run brief'inden long-form video vaadi, ana soru, hook stratejisi, retention stratejisi, bolum plani, gorsel strateji ve payoff ciktisi uretir.
- CreativeDirector prompt'u concept profile'dan hedef kitle, kanal vaadi, ton, content rules, visual style bible, character bible ve text policy alir; fallback plan da ayni profil degerlerini kullanir.
- CreativeDirector ekstra preset istemez; Script stage'in secili script presetindeki AI text connection/model bilgisini kullanir. AI cevabi alinmazsa deterministik fallback plan uretir.
- `ScriptStageExecutor` ve `StoryboardStageExecutor`, context icinde `StageType.CreativeDirector` varsa bu plani prompt'a ekleyerek senaryo ve director layer kararlarini ayni ust stratejiye baglar.
- `StoryboardStageExecutor`, `ScriptStagePayload` uzerinden Director Layer v2 planini uretir: video mood, visual continuity bible, color palette, camera language, lighting style, editing principles, negative visual rules, chapter strategy ve sahne/beat kararlarini tek payload'da tasir.
- Storyboard prompt'u concept visual identity ile beslenir; AI veya fallback storyboard style bible/continuity/text policy eksiklerini concept profile'dan tamamlar.
- Sahne basina 1-3 arasi `visualBeats` uretir; her beat artik subject, composition, lens, lighting, color notes, continuity notes, negative prompt ve cut intent metadata'si tasir.
- AI storyboard basarisiz olursa deterministik fallback yonetmen plani uretir; workflow bu yuzden tamamen bloklanmaz.
- Storyboard ekstra preset istemez; Script stage'in secili script presetindeki AI text connection/model bilgisini kullanir.
- `ImageStageExecutor`, context icinde `StageType.Storyboard` varsa her sahnenin beat promptlarini ayri gorsellere cevirir; style bible, continuity bible, camera language, palette, lighting, concept visual identity ve beat negative prompt bilgilerini gorsel prompt/negative prompt'a ekler. Storyboard yoksa Scene Direction V2 ve Visual Variety V1 alanlarindan shot type, camera motion, composition, overlay ve director intent fallback'i uretir.
- `ThumbnailStageExecutor`, script title/ilk sahne fikrine ek olarak concept visual identity ve text policy ile 16:9 kapak prompt'u uretir.
- `SceneImageItem` beat role, visual type, variety role/reason, shot type, effect/transition yaninda director intent, continuity anchor, composition, lens, lighting, color notes, cut intent ve prompt-level `visualQualityScore` metadata'si tasir.
- `SceneLayoutStageExecutor`, ayni sahneye ait birden fazla beat gorselini sahne ses suresine yayarak timeline'a yerlestirir. Storyboard yoksa eski tek-gorsel akis korunur.
- Manuel image regenerate akisi hedef beat/path bulunamazsa yanlis ilk gorseli degistirmez; `BuildMissingSceneImageTarget` script/storyboard/concept baglamiyla eksik beat icin yeni hedef prompt uretir.

EditPlan stage notu:

- `EditPlanStageExecutor`, Script/Image/TTS ciktilarini ve varsa Storyboard/STT ciktilarini okuyarak EDL benzeri kurgu karar plani uretir; Director Layer metadata'sini edit kararlarina tasir.
- Ekstra preset istemez; Script stage'in secili script presetindeki AI text connection/model bilgisini kullanir.
- AI edit plan cevabi alinmazsa deterministik fallback plan uretir; workflow bu yuzden tamamen bloklanmaz.
- `EditPlanStagePayload` sahne bazinda intent, pacing, chapter title, retention goal, music energy, caption mode, continuity anchor, visual beat agirliklari, effect/transition tercihleri, caption cue ve audio cue bilgisi tasir. Her beat `segmentRole` ile establishing/detail/emphasis/transition/payoff gibi mini kurgu rolunu da tasir.
- `EditPlanStageExecutor`, gecisleri beat rolune ve scene intent'e gore normalize eder. Flash daha cok hook/reveal gibi vurgu anlarinda, dip black bolum/kirilma gecislerinde, crossfade ise yumusak akis noktalarinda tercih edilir; boylece her cumlede cut atilmaz.
- `SceneLayoutStageExecutor`, context icinde `StageType.EditPlan` varsa beat surelerini edit planindaki `durationWeight`, motion, transition, director intent, cut reason ve continuity kararlarina gore timeline'a uygular; yoksa mevcut Storyboard/auto B-roll akisi korunur.
- `SceneLayoutStagePayload.EditDecisionList`, render oncesi okunabilir kurgu plani/EDL ciktisidir. FE review ekraninda zaman, sahne, segment role, transition/effect, cut reason, director intent ve audio transition bilgileriyle gosterilir.
- `VisualEvent` artik shot type, director intent, chapter title, caption mode, music energy, continuity anchor, composition, segment role, cut reason, audio transition ve audio offset alanlarini tasir. Render motoru bu alanlarin bir kismini efekt/transition/J-cut-L-cut icin kullanir; kalanlari FE preview/debug ve sonraki render engine revizyonlari icin saklar.
- `AudioEvent` voice track icin duration, source trim, micro fade, edit transition ve edit offset metadata'si tasir. EditPlan aktifken SceneLayout emphasis/reveal gibi anlarda kucuk J-cut, setup/context gibi anlarda kucuk L-cut/visual lead uygular.
- Long-form hedefinde kullanici FE'den konsept + production brief verir; detay prompt kararlari preset sozlesmeleri, CreativeDirector, Storyboard ve EditPlan katmanlarinda yonetilir.

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
- `PipelineRunDetailDto` render metadata'sina ek olarak `ThumbnailUrl`, `ThumbnailWidth`, `ThumbnailHeight` alanlarini tasir; FE kapak preview'i bu alanlardan beslenir.
- Render servisi SceneLayout payload'inda eski/yanlis base path tasiyan image/audio dosyalarini render run klasorundeki `images` ve `audio` altindan dosya adiyla yeniden cozer; bu retry/re-render senaryolarinda path drift hatalarini azaltir.
- `ThumbnailStagePayload` kapak gorselinin fiziksel path, public URL, prompt ve boyut bilgisini tasir. Upload stage bu payload varsa `SocialMetadata.ThumbnailPath` alanina aktarir.
- Buyuk medya dosyalarina dokunmadan once kaynagin user-generated olup olmadigi kontrol edilmeli.

## Render Notlari

- Render motoru `Application/Services/FFmpegVideoService.cs` uzerinden FFmpeg komutu uretir.
- ASS altyazi canvas'i render presetinden gelen layout width/height degerlerine gore uretilir; 1080x1920 sabitine bagli kalmamali.
- Subtitle fontlari video klasoru yerine render'a ozel gecici font klasorunden verilir; FFmpeg'in `.txt`, `.ass`, `.mp4` dosyalarini font gibi taramasi engellenir.
- Video encoder once `h264_nvenc` dener; donanim/driver destegi yoksa `libx264` ile CPU fallback yapar.
- Uzun video render'inda sahne sayisi veya FFmpeg komut uzunlugu riskli seviyeye cikarsa render otomatik chunk moduna gecer: sahneler parca MP4'ler halinde render edilir, sonra concat ile final video olusturulur.
- `RenderVisualEffectsSettings` icinde `EnableAutoBroll`, `MinSceneDurationForBrollSec`, `BrollSegmentDurationSec`, `MaxBrollCutsPerScene` ve `EnableOverlayText` alanlari vardir.
- Auto B-roll v1 gercek ekstra AI gorsel uretmez; `SceneLayoutStageExecutor` uzun sahneleri ayni gorsel uzerinde 8-10 sn civari visual beat'lere boler. `FFmpegVideoService` bu beat'lere `zoom_in`, `zoom_out`, `pan_left/right/up/down` hareketleri uygular.
- Storyboard v1 aktifse gercek ekstra AI gorseller beat bazinda uretilir; layout bu beat gorsellerini sahne icine dagitir. Bundan sonraki B-roll v2 hedefi, beat'leri anlatim kelime zamanlari ve vurgu noktalarina gore daha akilli hizalamaktir.
- Render compiler v1, `VisualEvent.TransitionType` degerlerinden `crossfade`, `dip_black` ve `flash` icin FFmpeg `xfade` zinciri kurar; `cut`/desteklenmeyen gecislerde concat davranisi korunur.
- Transition bulunan clip'lerde sureler overlap kadar uzatilir; boylece xfade toplam video suresini kisaltmaz ve audio/caption senkronu korunur.
- Editorial audio timing v1, voice track'te J-cut/L-cut ofseti varsa concat yerine FFmpeg timeline mix kullanir. Voice dosyalari ayri input olarak `atrim`, `afade`, `adelay` ve `amix` zincirinden gecer; offset yoksa eski concat voice akisi korunur.
- Render audio mix ayarlarinda `EnableEditorAudioCuts`, `MaxEditorAudioOffsetSec` ve `VoiceMicroFadeSec` vardir. FE bu alanlari gondermese bile backend default olarak muhafazakar J/L-cut V1'i acik kullanir.
- `zoompan` motion expression'lari FFmpeg filtergraph icinde `,` karakterini filter ayirici sanmasin diye `EscapeFilterExpression` ile kacirilir. `min(...)`, `max(...)`, `if(...)` gibi expression'larda bu kritik.
- `VisualEvent.OverlayText` doluysa ve render preset `VisualEffectsSettings.EnableOverlayText=true` ise render clip'i uzerine `drawtext` ile kisa vurgu metni basilir. Kapaliyken overlay metinleri layout metadata'sinda kalir ama final videoya yazilmaz.
- `EditPlanStagePayload.AudioCues` sahne icinde `hit`, `whoosh`, `low_boom` gibi cue kararlarini tasir. `SceneLayoutStageExecutor` bunlari `AudioTrack` icine `sfx` event olarak yazar.
- Generic cue degerleri (`sfx`, `sound_effect`, `effect`, `music`) bilincli SFX olmadigi icin `none` kabul edilir; sadece `hit`, `whoosh`, `low_boom` gibi net cue'lar timeline'a girer.
- SFX dosyasi `ALL_FILES/Assets/sfx` veya ilgili asset klasorlerinde bulunursa gercek dosya kullanilir; bulunamazsa `synth://...` fallback ile FFmpeg lavfi uzerinden basit sentetik cue uretilir.
- Audio mix katmani voice/music/sfx tracklerini miksler; music varsa mevcut ducking korunur, sfx eventleri sahne zamanina gore `adelay` ile yerlestirilir.
- Image/Thumbnail/manuel image regenerate istekleri `AiImageRetryPolicy` uzerinden gecer. Bu policy 429/RESOURCE_EXHAUSTED ve gecici 5xx hatalarinda exponential retry yapar; ayrica process genelinde tek image istegi gecirir ve iki image istegi arasinda en az 12 sn bekleme uygular.
- `RenderStageExecutor` render sonucunda `RenderStagePayload` icine width, height, fps, aspect ratio, sure ve dosya boyutu yazar.
- `CreatePipelineRunRequest.PauseBeforeRender` default `true` gelir. Bu modda Render stage execution'i `StageStatus.WaitingForApproval` olarak olusur; runner SceneLayout/gorsel akisini tamamlayinca render'a baslamadan run'i `ContentPipelineStatus.WaitingForApproval` durumunda durdurur. `ApproveRunAsync` bekleyen stage'i tekrar `Pending` yapar ve render kaldigi yerden baslar.

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
