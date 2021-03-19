namespace OrleanPG.Grains.Interfaces
{
    public record GameGeneralInfo(GameId Id, string? XPlayerName, string? OPlayerName)
    {
        public bool IsRunning => XPlayerName != null && OPlayerName != null;
    }
}
