namespace Core.Enums
{
    [Flags]
    public enum AiCapability { None = 0, Text = 1, Image = 2, Embedding = 4, Audio = 8, Video = 16 }
}
