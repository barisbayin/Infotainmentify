using Core.Entity;

namespace Core.Abstractions
{
    public interface ICurrentJobContext
    {
        int UserId { get; set; }     // 🔥 setter eklendi
        JobSetting? Setting { get; set; }
    }
}
