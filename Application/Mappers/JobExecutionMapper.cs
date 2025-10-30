using Application.Contracts.Job;
using Core.Entity;

namespace Application.Mappers
{
    public static class JobExecutionMapper
    {
        public static JobExecutionListDto ToListDto(this JobExecution e)
        {
            return new JobExecutionListDto
            {
                Id = e.Id,
                JobId = e.JobId,
                JobName = e.Job?.Name ?? "",
                Status = e.Status,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt,
                ErrorMessage = e.ErrorMessage
            };
        }

        public static JobExecutionDetailDto ToDetailDto(this JobExecution e)
        {
            return new JobExecutionDetailDto
            {
                Id = e.Id,
                JobId = e.JobId,
                JobName = e.Job?.Name ?? "",
                Status = e.Status,
                ResultJson = e.ResultJson,
                ErrorMessage = e.ErrorMessage,
                StartedAt = e.StartedAt,
                CompletedAt = e.CompletedAt
            };
        }
    }
}
