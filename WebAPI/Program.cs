using Application.Abstractions;
using Application.Options;
using Application.Services;
using Core.Abstractions;
using Core.Contracts;
using Core.Entity;
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
using System.Reflection;
using System.Text;
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


            builder.Services.AddInfrastructure(builder.Configuration);

            builder.Services.AddScoped<PromptService>();
            builder.Services.AddScoped<TopicService>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<AppUserService>();


            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

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

            builder.Services.AddSingleton<IUserDirectoryService, UserDirectoryService>();

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
                });

            const string CorsPolicy = "InfotainmentifyCors";

            // 1) CORS'u kaydet
            builder.Services.AddCors(opt =>
            {
                opt.AddPolicy(CorsPolicy, p =>
                    p.WithOrigins(
                         "http://localhost:5173",  // Vite default
                         "https://localhost:5173",
                         "http://127.0.0.1:5173",
                         "https://127.0.0.1:5173"
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    // .WithExposedHeaders("X-Total-Count") // sayfalama vs. gerekiyorsa
                    .SetPreflightMaxAge(TimeSpan.FromHours(12))
                );
            });


            var app = builder.Build();

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

            app.UseCors(CorsPolicy);

            app.UseHttpsRedirection();

            app.UseAuthentication();                // (varsa)
            app.UseAuthorization();
  


            app.MapControllers();

            app.Run();
        }
    }
}
