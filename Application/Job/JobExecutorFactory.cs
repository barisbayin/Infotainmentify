using Application.Job;
using Core.Enums;

namespace Application.Job
{
    public class JobExecutorFactory
    {
        private readonly IEnumerable<IJobExecutor> _executors;

        public JobExecutorFactory(IEnumerable<IJobExecutor> executors)
        {
            _executors = executors;
        }

        public IJobExecutor Resolve(JobType type)
        {
            var exec = _executors.FirstOrDefault(x => x.JobType == type);
            if (exec == null)
                throw new NotSupportedException($"Desteklenmeyen JobType: {type}");
            return exec;
        }
    }
}
