namespace ProjectScare.Core
{
    public readonly struct HostGameRequest
    {
        private const int MinPasswordLength = 8;
        private const int MaxPasswordLength = 64;

        public string Name { get; }
        public bool IsPublic { get; }
        public string Region { get; }
        public string Password { get; }
        public string PlayerName { get; }

        private HostGameRequest(string name, bool isPublic, string region, string password, string playerName)
        {
            Name = name;
            IsPublic = isPublic;
            Region = region;
            Password = password;
            PlayerName = playerName;
        }

        public static HostGameRequest Create(string rawName, bool isPublic, string region, string password, string playerName) =>
            new HostGameRequest(
                rawName?.Trim() ?? string.Empty,
                isPublic,
                NullIfEmpty(region),
                NullIfEmpty(password),
                Core.PlayerName.Normalize(playerName));

        public static bool IsValidPassword(string password) =>
            password != null && password.Length >= MinPasswordLength && password.Length <= MaxPasswordLength;

        // Bus logging prints this text, so it only says whether there is a password.
        public override string ToString() =>
            $"{Name} (public: {IsPublic}, region: {Region ?? "automatic"}, player: {PlayerName}, has password: {Password != null})";

        private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
