namespace Core.Enums
{
    public enum ContentPipelineStatus
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
