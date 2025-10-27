namespace Core.Abstractions
{
    public interface ISoftDelete
    {
        bool Removed { get; set; }
    }
}
