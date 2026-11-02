namespace ProjectScare.Core
{
    public readonly struct HostGameRequest
    {
        public string Name { get; }
        public bool IsPublic { get; }

        private HostGameRequest(string name, bool isPublic)
        {
            Name = name;
            IsPublic = isPublic;
        }

        public static HostGameRequest Create(string rawName, bool isPublic) =>
            new HostGameRequest(rawName?.Trim() ?? string.Empty, isPublic);

        public override string ToString() => $"{Name} (public: {IsPublic})";
    }
}
