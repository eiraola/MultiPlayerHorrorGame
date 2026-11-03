namespace ProjectScare.Core
{
    public readonly struct JoinGameRequest
    {
        public string Code { get; }
        public string Password { get; }
        public string PlayerName { get; }

        private JoinGameRequest(string code, string password, string playerName)
        {
            Code = code;
            Password = password;
            PlayerName = playerName;
        }

        public static JoinGameRequest Create(string rawCode, string password, string playerName) =>
            new JoinGameRequest(
                rawCode?.Trim().ToUpperInvariant() ?? string.Empty,
                string.IsNullOrEmpty(password) ? null : password,
                Core.PlayerName.Normalize(playerName));

        // Bus logging prints this text, so it only says whether there is a password.
        public override string ToString() =>
            $"{Code} (player: {PlayerName}, has password: {Password != null})";
    }
}
