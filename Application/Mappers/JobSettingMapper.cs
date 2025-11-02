using Application.Contracts.Job;
using Core.Entity;

namespace Application.Mappers
{
    public static class JobSettingMapper
    {
        public static JobSettingListDto ToListDto(this JobSetting e)
            => new()
            {
                Id = e.Id,
                Name = e.Name,
                JobType = e.JobType.ToString(),
                IsAutoRunEnabled = e.IsAutoRunEnabled,
                PeriodHours = e.PeriodHours,
                Status = e.Status.ToString(),
                LastRunAt = e.LastRunAt,
                LastError = e.LastError,
                LastErrorAt = e.LastErrorAt
            };

        public static JobSettingDetailDto ToDetailDto(this JobSetting e)
            => new()
            {
                Id = e.Id,
                Name = e.Name,
                JobType = e.JobType.ToString(),
                ProfileId = e.ProfileId,
                ProfileType = e.ProfileType,
                IsAutoRunEnabled = e.IsAutoRunEnabled,
                PeriodHours = e.PeriodHours,
                Status = e.Status.ToString(),
                LastRunAt = e.LastRunAt,
                LastError = e.LastError
            };

        public static void UpdateFromDto(this JobSetting e, JobSettingDetailDto dto)
        {
            e.Name = dto.Name.Trim();
            e.ProfileId = dto.ProfileId;
            e.ProfileType = dto.ProfileType;
            e.IsAutoRunEnabled = dto.IsAutoRunEnabled;
            e.PeriodHours = dto.PeriodHours;
            e.LastError = dto.LastError;
            e.LastRunAt = dto.LastRunAt;
        }
    }
}
