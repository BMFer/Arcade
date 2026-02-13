namespace Arcade.Heist.Game.Models;

public class LevelInfo
{
    public int LevelNumber { get; init; }
    public required string Name { get; init; }
    public ulong CategoryId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong RoleId { get; set; }
    public int DifficultyTier { get; init; }

    public static readonly string[] LevelNames =
    [
        "Basement",
        "Ground Floor",
        "Vault Corridor",
        "Security Room",
        "Crown Room"
    ];
}
