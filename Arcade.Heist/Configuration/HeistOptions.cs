namespace Arcade.Heist.Configuration;

public class HeistOptions
{
    public const string SectionName = "Heist";

    public int LobbyCountdownSeconds { get; set; } = 10;
    public int WrongGuessCooldownSeconds { get; set; } = 10;
    public int FreezeDurationSeconds { get; set; } = 60;
    public int CrownHoldSeconds { get; set; } = 60;
    public int CardAwardInterval { get; set; } = 3;
    public int MaxLevels { get; set; } = 5;
    public string TowerDataPath { get; set; } = "data/tower.json";
}
