using Core.Enums;

namespace Application.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class AiProviderAttribute : Attribute
    {
        public AiProviderType Provider { get; }

        public AiProviderAttribute(AiProviderType provider)
        {
            Provider = provider;
        }
    }
}
