using Application.AiLayer.Abstract;
using Application.AiLayer.Concrete;
using Application.Attributes;
using Application.Executors;
using Application.Services.Interfaces;
using Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Application.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfotainmentifyExecutors(this IServiceCollection services)
        {
            // 1. Çalışan Assembly'deki tüm tipleri tara
            var executors = Assembly.GetAssembly(typeof(BaseStageExecutor))! // Executorların olduğu assembly
                .GetTypes()
                .Where(t => t.GetCustomAttribute<StageExecutorAttribute>() != null && !t.IsAbstract)
                .ToList();

            // 2. Hepsini Scoped olarak kaydet
            foreach (var executorType in executors)
            {
                // Kendisi olarak kaydet (Factory bunu kullanacak)
                services.AddScoped(executorType);

                // Log atalım ki hangi modüller yüklendi görelim
                Console.WriteLine($"[System] Executor Registered: {executorType.Name}");
            }

            // 3. Factory'yi de kaydet
            services.AddScoped<StageExecutorFactory>();

            return services;
        }

        public static IServiceCollection AddInfotainmentifyAiServices(this IServiceCollection services)
        {
            var aiServices = Assembly.GetAssembly(typeof(IAiGeneratorFactory))!
                .GetTypes()
                .Where(t => t.GetCustomAttribute<AiProviderAttribute>() != null && !t.IsAbstract);

            foreach (var type in aiServices)
            {
                // Kendisi olarak kaydet (Factory resolve edebilsin diye)
                services.AddScoped(type); // Scoped olması iyidir, her requestte yeni state
                Console.WriteLine($"[AI] Service Registered: {type.Name}");
            }

            // Factory'i de kaydet
            services.AddScoped<IAiGeneratorFactory, AiGeneratorFactory>();

            return services;
        }


        public static IServiceCollection AddInfotainmentifyUploadServices(this IServiceCollection services)
        {
            // ISocialPlatformService'in olduğu assembly'i al
            var assembly = Assembly.GetAssembly(typeof(ISocialPlatformService))!;

            // 1. FİLTRELEME MANTIĞINI DEĞİŞTİR
            // Attribute yerine, Interface implementasyonuna bakıyoruz.
            var uploadServices = assembly.GetTypes()
                .Where(t => typeof(ISocialPlatformService).IsAssignableFrom(t) // Interface'i miras almış mı?
                            && t.IsClass
                            && !t.IsAbstract);

            foreach (var type in uploadServices)
            {
                // 2. KAYIT MANTIĞINI DEĞİŞTİR
                // Sadece "type" değil, "Interface -> Type" şeklinde eşleştiriyoruz.
                // Böylece: services.AddScoped<ISocialPlatformService, YouTubeUploaderService>(); yapmış gibi oluyoruz.

                services.AddScoped(typeof(ISocialPlatformService), type);

                Console.WriteLine($"[Social] Uploader Registered: {type.Name}");
            }

            return services;
        }
    }
}
