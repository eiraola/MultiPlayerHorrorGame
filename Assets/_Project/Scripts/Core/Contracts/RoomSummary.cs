namespace ProjectScare.Core
{
    public readonly struct RoomSummary
    {
        public string Id { get; }
        public string Name { get; }
        public string OwnerName { get; }
        public string Region { get; }
        public int Players { get; }
        public int MaxPlayers { get; }
        public bool HasPassword { get; }

        public bool IsFull => Players >= MaxPlayers;

        public RoomSummary(string id, string name, string ownerName, string region, int players, int maxPlayers, bool hasPassword)
        {
            Id = id;
            Name = name;
            OwnerName = ownerName;
            Region = region;
            Players = players;
            MaxPlayers = maxPlayers;
            HasPassword = hasPassword;
        }

        public override string ToString() => $"{Name} ({Players}/{MaxPlayers}, owner: {OwnerName}, region: {Region})";
    }
}
