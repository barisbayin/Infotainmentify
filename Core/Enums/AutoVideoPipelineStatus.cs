namespace Core.Enums
{
    public enum AutoVideoPipelineStatus
    {
        Pending,
        SelectingTopic,
        TopicSelected,

        GeneratingScript,
        ScriptGenerated,

        GeneratingAssets,
        AssetsGenerated,

        Rendering,
        Rendered,

        Uploading,
        Uploaded,

        Completed,
        Failed
    }
}
