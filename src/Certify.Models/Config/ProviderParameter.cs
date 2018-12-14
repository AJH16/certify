namespace Certify.Models.Config
{
    public abstract class ProviderParameter
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public virtual bool IsPassword { get => false; }
        public bool IsRequired { get; set; }
        public virtual string Value { get; set; }
        public bool IsCredential { get; set; } = true;
    }
}
