namespace ProjectScare.Core
{
    public static class PlayerName
    {
        public static string Normalize(string rawName) => rawName?.Trim() ?? string.Empty;

        public static bool IsValid(string rawName) => !string.IsNullOrWhiteSpace(rawName);
    }
}
