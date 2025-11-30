namespace Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class StagePresetAttribute : Attribute
    {
        public Type PresetEntityType { get; }

        public StagePresetAttribute(Type presetEntityType)
        {
            PresetEntityType = presetEntityType;
        }
    }
}
