using Core.Enums;

namespace Core.Abstractions 
{
    public interface IJobProfile
    {
        int Id { get; set; }
        JobType JobType { get; }
        void Validate();
        IDictionary<string, object> ToParameters();
    }
}
