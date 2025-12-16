using Application;
using Application.Abstractions;
using Application.Extensions;
using Application.Options;
using Application.Pipeline;
using Application.Services;
using Application.Services.Interfaces;
using Application.Services.Pipeline;
using Application.Services.PresetService;
using Core.Abstractions;
using Core.Entity.User;
using Core.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using Infrastructure;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Quartz;
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

            // =========================================================
            // 1. DATABASE & CORE SERVICES
            // =========================================================
            builder.Services.AddDbContext<AppDbContext>(opts =>
            {
                opts.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
                // opts.AddInterceptors(new TimestampInterceptor()); // Eðer interceptor varsa aç
            });

            builder.Services.AddHttpClient();

            // Infrastructure katmanýndaki repository vs. kayýtlarý
            builder.Services.AddInfrastructure(builder.Configuration);

            // Quartz.NET (Zamanlanmýþ görevler için)
            builder.Services.AddAppQuartz();

            // =========================================================
            // 2. APPLICATION SERVICES (BUSINESS LOGIC)
            // =========================================================

            // --- Core Services ---
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AppUserService>();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
            builder.Services.AddScoped<IUserDirectoryService, UserDirectoryService>();
            builder.Services.AddScoped<IVideoRendererService, FFmpegVideoService>();
            builder.Services.AddScoped<IContentPipelineRunner, ContentPipelineRunner>();
            builder.Services.AddScoped<IContentPipelineService, ContentPipelineService>();
            builder.Services.AddScoped<INotifierService, SignalRNotifierService>();
            builder.Services.AddScoped<IAssetService, AssetService>();

            // --- Content Services (BaseService Türevleri) ---
            builder.Services.AddScoped<ConceptService>();
            builder.Services.AddScoped<PromptService>();
            builder.Services.AddScoped<TopicService>();
            builder.Services.AddScoped<ScriptService>();
            builder.Services.AddScoped<UserAiConnectionService>();
            builder.Services.AddScoped<SocialChannelService>();

            // --- Preset Services ---
            builder.Services.AddScoped<TopicPresetService>();
            builder.Services.AddScoped<ScriptPresetService>();
            builder.Services.AddScoped<ImagePresetService>();
            builder.Services.AddScoped<TtsPresetService>();
            builder.Services.AddScoped<SttPresetService>();
            builder.Services.AddScoped<RenderPresetService>();
            builder.Services.AddScoped<VideoPresetService>();


            // --- Pipeline Engine ---
            builder.Services.AddScoped<ContentPipelineRunner>(); // Orkestra Þefi
            builder.Services.AddScoped<PipelineTemplateService>();

            // =========================================================
            // 3. AI & EXECUTOR LAYER (OTOMATÝK KAYIT)
            // =========================================================

            // Extension method ile tüm [AiProvider] servislerini kaydet
            // (ServiceCollectionExtensions.cs dosyasýnda tanýmlamýþtýk)
            builder.Services.AddInfotainmentifyAiServices();

            // Extension method ile tüm [StageExecutor] sýnýflarýný kaydet
            builder.Services.AddInfotainmentifyExecutors();

            // =========================================================
            // 4. API & SECURITY & VALIDATION
            // =========================================================
            builder.Services.AddScoped<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
            builder.Services.AddSingleton<IJwtTokenFactory, JwtTokenFactory>();

            builder.Services.AddDataProtection();
            builder.Services.AddScoped<ISecretStore, DataProtectionSecretStore>();

            // SignalR (Notifications)
            builder.Services.AddSignalR();
            // builder.Services.AddScoped<INotifierService, SignalRNotifierService>();

            // Controllers & JSON Config
            builder.Services.AddControllers()
                .AddJsonOptions(o =>
                {
                    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            // FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddFluentValidationClientsideAdapters();
            // Application katmanýndaki validatorlarý bul
            builder.Services.AddValidatorsFromAssemblyContaining<Application.Validators.SaveTopicValidator>();

            // Swagger Config (JWT Destekli)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Infotainmentify API", Version = "v1" });

                // Authorize butonu ekle
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new string[] {}
                    }
                });
            });

            // JWT Auth
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

                    // SignalR Auth Hook
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;
                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notify"))
                            {
                                context.Token = accessToken;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

            // CORS Config
            const string CorsPolicy = "InfotainmentifyCors";
            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CorsPolicy, policy =>
                {
                    var origins = builder.Environment.IsDevelopment()
                        ? new[] { "http://localhost:5173", "https://localhost:5173", "http://localhost:5174", "https://localhost:5174" }
                        : new[] { "https://moduleer.com", "https://www.moduleer.com" };

                    policy.WithOrigins(origins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .SetPreflightMaxAge(TimeSpan.FromHours(1));
                });
            });

            // =========================================================
            // 5. BUILD & RUN
            // =========================================================
            var app = builder.Build();

            // Seed Data (Admin vs)
            using (var scope = app.Services.CreateScope())
            {
                // Veritabaný yoksa oluþtur (Migration uygula)
                // var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // db.Database.Migrate();

                // Admin Seed
                var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
                await seeder.SeedAdminAsync();
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles(); // Standart wwwroot için

            var allFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ALL_FILES");
            if (!Directory.Exists(allFilesPath)) Directory.CreateDirectory(allFilesPath);

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(allFilesPath),
                RequestPath = ""
            });

            app.UseHttpsRedirection();

            app.UseRouting(); // Sýralama önemli: Routing -> Cors -> Auth -> Endpoints
            app.UseCors(CorsPolicy);

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<NotifyHub>("/hubs/notify"); // Hub endpointi

            app.Run();
        }
    }
}
