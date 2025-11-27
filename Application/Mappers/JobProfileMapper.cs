using Core.Entity;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Mappers
{
    public static class JobProfileMapper
    {
        private static readonly Dictionary<JobType, Type> _map = new()
        {
            { JobType.TopicGeneration, typeof(TopicGenerationProfile) },
            { JobType.ScriptGeneration, typeof(ScriptGenerationProfile) },
            { JobType.AutoVideoGeneration, typeof(VideoGenerationProfile) },
            //{ JobType.ThumbnailRender, typeof(ThumbnailRenderProfile) }
        };

        public static Type? GetProfileType(JobType jobType)
            => _map.TryGetValue(jobType, out var t) ? t : null;

        public static string? GetProfileTypeName(JobType jobType)
            => _map.TryGetValue(jobType, out var t) ? t.Name : null;

        public static string? GetProfileAssemblyQualifiedName(JobType jobType)
            => _map.TryGetValue(jobType, out var t) ? t.AssemblyQualifiedName : null;
    }
}
