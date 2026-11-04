namespace ProjectScare.Core
{
    // A request carries either a code typed by the player or the id of a room picked from the list.
    public readonly struct JoinGameRequest
    {
        public string Code { get; }
        public string RoomId { get; }
        public string Password { get; }
        public string PlayerName { get; }

        public bool IsByRoomId => RoomId != null;

        private JoinGameRequest(string code, string roomId, string password, string playerName)
        {
            Code = code;
            RoomId = roomId;
            Password = password;
            PlayerName = playerName;
        }

        public static JoinGameRequest Create(string rawCode, string password, string playerName) =>
            new JoinGameRequest(
                rawCode?.Trim().ToUpperInvariant() ?? string.Empty,
                null,
                NullIfEmpty(password),
                Core.PlayerName.Normalize(playerName));

        public static JoinGameRequest CreateForRoom(string roomId, string password, string playerName) =>
            new JoinGameRequest(
                string.Empty,
                NullIfEmpty(roomId),
                NullIfEmpty(password),
                Core.PlayerName.Normalize(playerName));

        // Bus logging prints this text, so it only says whether there is a password.
        public override string ToString() =>
            $"{(IsByRoomId ? $"room {RoomId}" : Code)} (player: {PlayerName}, has password: {Password != null})";

        private static string NullIfEmpty(string value) => string.IsNullOrEmpty(value) ? null : value;
    }
}
