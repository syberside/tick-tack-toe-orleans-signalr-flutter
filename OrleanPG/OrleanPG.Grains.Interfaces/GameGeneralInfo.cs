namespace OrleanPG.Grains.Interfaces
{
    /// <summary>
    /// NOTE:  `record` keyword can't be used as because this DTO is used in array
    /// </summary>
    public class GameGeneralInfo
    {
        public GameId Id { get; init; }
        public string? XPlayerName { get; init; }
        public string? OPlayerName { get; init; }
        public bool IsRunning => XPlayerName != null && OPlayerName != null;
    }
}