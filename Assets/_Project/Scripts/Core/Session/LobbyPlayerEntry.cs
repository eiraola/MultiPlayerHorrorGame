namespace ProjectScare.Core
{
    public readonly struct LobbyPlayerEntry
    {
        public string Name { get; }
        public bool IsReady { get; }

        public LobbyPlayerEntry(string name, bool isReady)
        {
            Name = name;
            IsReady = isReady;
        }

        public override string ToString() => $"{Name} (ready: {IsReady})";
    }
}
