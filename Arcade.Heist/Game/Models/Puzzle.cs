namespace Arcade.Heist.Game.Models;

public class Puzzle
{
    public required string OriginalWord { get; init; }
    public required string ScrambledWord { get; init; }
    public int DifficultyTier { get; init; }
    public bool Solved { get; set; }
    public ulong? SolvedByUserId { get; set; }
}
