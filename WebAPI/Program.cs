using Application;
using Application.Abstractions;
using Application.AiLayer;
using Application.Executors;
using Application.Job;
using Application.Options;
using Application.Services;
using Application.SocialPlatform;
using Core.Abstractions;
using Core.Entity;
using Core.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Google.Api;
using Infrastructure;
using Infrastructure.Job;
using Infrastructure.Job.JobExecutors;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Quartz;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using WebAPI.Hubs;
using WebAPI.Service;

namespace WebAPI
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();

            builder.Services.AddDbContext<AppDbContext>(opts =>
            {
                opts.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
                opts.AddInterceptors(new TimestampInterceptor());
            });

            builder.Services.AddHttpClient();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddAppQuartz();



            builder.Services.AddScoped<PromptService>();
            builder.Services.AddScoped<TopicService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AppUserService>();
            builder.Services.AddScoped<UserAiConnectionService>();
            builder.Services.AddScoped<UserSocialChannelService>();
            builder.Services.AddScoped<TopicGenerationProfileService>();
            builder.Services.AddScoped<JobSettingService>();
            builder.Services.AddScoped<JobExecutionService>();
            builder.Services.AddScoped<TopicGenerationService>();
            builder.Services.AddScoped<ScriptGenerationService>();
            builder.Services.AddScoped<ScriptService>();
            builder.Services.AddScoped<ScriptGenerationProfileService>();
            builder.Services.AddScoped<JobExecutorFactory>();
            builder.Services.AddScoped<BackgroundJobRunner>();
            builder.Services.AddScoped<VideoAssetService>();
            builder.Services.AddScoped<AssetGenerationService>();
            builder.Services.AddScoped<VideoAssetService>();
            builder.Services.AddScoped<AutoVideoPipelineService>();
            builder.Services.AddScoped<UploadVideoService>();
            builder.Services.AddScoped<YouTubeUploader>();
            builder.Services.AddScoped<VideoGenerationProfileService>();
            builder.Services.AddScoped<AutoVideoGenerationService>();
            builder.Services.AddScoped<AutoVideoAssetFileService>();
            builder.Services.AddScoped<RenderVideoService>();
            builder.Services.AddScoped<RenderProfileService>();

            builder.Services.AddSingleton<StageExecutorFactory>();

            // Executors (transient olmalý)
            builder.Services.AddTransient<TopicStageExecutor>();
            builder.Services.AddTransient<ContentPlanStageExecutor>();
            builder.Services.AddTransient<ImageStageExecutor>();
            builder.Services.AddTransient<TtsStageExecutor>();
            builder.Services.AddTransient<VideoStageExecutor>();
            builder.Services.AddTransient<RenderStageExecutor>();
            builder.Services.AddTransient<UploadStageExecutor>();

            // Ýhtiyaç olursa
            //builder.Services.AddTransient<VideoAIStageExecutor>();
            //builder.Services.AddTransient<SttStageExecutor>();
            //builder.Services.AddTransient<ImageVariationStageExecutor>();
            //builder.Services.AddTransient<BRollStageExecutor>();
            //builder.Services.AddTransient<VideoClipStageExecutor>();
            //services.AddScoped<TikTokUploader>();
            //services.AddScoped<InstagramUploader>();
            //builder.Services.AddScoped<TopicGenerationService>();

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<ICurrentJobContext , CurrentJobContext>();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // FluentValidation (otomatik MVC entegrasyonu)
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            // Validator’larý tara (WebAPI projesindelerse):
            builder.Services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));

            builder.Services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();
            builder.Services.AddDataProtection(); // <-- bu DataProtection key ring'i hazýrlar
            builder.Services.AddScoped<ISecretStore, DataProtectionSecretStore>();
            builder.Services.AddSingleton<IUserDirectoryService, UserDirectoryService>();

            builder.Services.AddScoped<IJobExecutor, TopicGenerationJobExecutor>();
            builder.Services.AddScoped<IJobExecutor, ScriptGenerationJobExecutor>();
            builder.Services.AddScoped<IJobExecutor, AutoVideoGenerationJobExecutor>();
            builder.Services.AddScoped<ISocialUploaderFactory, SocialUploaderFactory>();

            builder.Services.AddScoped<IFFmpegService, FFmpegService>();


            //builder.Services.AddScoped<IJobExecutor, StoryGenerationJobExecutor>();

            builder.Services.AddScoped<IAiGeneratorFactory, AiGeneratorFactory>();
            //builder.Services.AddScoped<GeminiAiClient>();
            //builder.Services.AddScoped<OpenAiClient>();

            builder.Services.AddHttpClient<GeminiAiClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2); // güvenli timeout
            });

            builder.Services.AddHttpClient<OpenAiClient>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            });


            builder.Services.AddSignalR();
            builder.Services.AddScoped<INotifierService, SignalRNotifierService>();

            builder.Services.AddControllers()
              .AddJsonOptions(o =>
              {
                  o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
              });

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/api/hubs/notify", StringComparison.OrdinalIgnoreCase))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            const string CorsPolicy = "InfotainmentifyCors";

            builder.Services.AddCors(options =>
            {
                if (builder.Environment.IsDevelopment())
                {
                    // ?? Development ortamý (localhost)
                    options.AddPolicy(CorsPolicy, policy =>
                        policy.WithOrigins(
                            "http://localhost:5173",
                            "https://localhost:5173",
                            "http://127.0.0.1:5173",
                            "https://127.0.0.1:5173"
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromHours(12))
                    );
                }
                else
                {
                    // ?? Production ortamý (canlý)
                    options.AddPolicy(CorsPolicy, policy =>
                        policy.WithOrigins(
                            "https://moduleer.com",           // frontend ana domain
                            "https://www.moduleer.com"        // www varsa
                                                              // ,"https://test.moduleer.com"    // staging varsa
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .SetPreflightMaxAge(TimeSpan.FromHours(12))
                    );
                }
            });


            //builder.Services.AddDataProtection().ProtectKeysWithCertificate("");


            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                await Application.Job.JobBootstrapper.InitializeAsync(scope.ServiceProvider);
            }

            using (var scope = app.Services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAdminAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }


            app.UseRouting();
            app.UseCors(CorsPolicy);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseWebSockets();
            app.UseHttpsRedirection();
            app.MapControllers();
            app.MapHub<NotifyHub>("/hubs/notify");

            app.Run();
        }
    }
}
