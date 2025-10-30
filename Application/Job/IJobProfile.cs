using Core.Enums;

namespace Application.Job
{
    public interface IJobProfile
    {
        int Id { get; set; }
        JobType JobType { get; }
        void Validate();
        IDictionary<string, object> ToParameters();
    }
}
