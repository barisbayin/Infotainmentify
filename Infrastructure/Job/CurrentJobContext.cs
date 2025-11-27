using Core.Abstractions;
using Core.Entity;

namespace Infrastructure.Job
{
    public class CurrentJobContext : ICurrentJobContext
    {
        public int UserId { get; set; }
        public JobSetting? Setting { get; set; }
    }
}
