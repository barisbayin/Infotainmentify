using Core.Enums;

namespace Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class StageExecutorAttribute : Attribute
    {
        public StageType Type { get; }

        public StageExecutorAttribute(StageType type)
        {
            Type = type;
        }
    }
}
