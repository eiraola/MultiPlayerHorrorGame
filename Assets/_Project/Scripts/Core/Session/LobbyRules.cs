namespace ProjectScare.Core
{
    public static class LobbyRules
    {
        public const int NoCountdown = 0;
        public const int CountdownStart = 10;
        public const int CountdownFinished = -1;

        public static bool AllReady(LobbyPlayerEntry[] players)
        {
            if (players == null || players.Length == 0)
                return false;

            foreach (LobbyPlayerEntry player in players)
            {
                if (!player.IsReady)
                    return false;
            }

            return true;
        }

        public static bool CanPressStart(LobbyPlayerEntry[] players, int countdown) =>
            countdown == NoCountdown && AllReady(players);

        public static bool IsEveryoneRegistered(int playerCount, int connectedCount) => playerCount == connectedCount;
    }
}
