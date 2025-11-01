namespace Core.Abstractions
{
    public interface ISoftDelete
    {
        bool Removed { get; set; }
        bool IsActive { get; set; }
    }
}
