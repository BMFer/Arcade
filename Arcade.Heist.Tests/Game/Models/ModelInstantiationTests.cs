using Arcade.Heist.Game.Models;

namespace Arcade.Heist.Tests.Game.Models;

[TestFixture]
public class ModelInstantiationTests
{
    [Test]
    public void GameState_DefaultsToIdle()
    {
        var state = new GameState();
        Assert.That(state.Status, Is.EqualTo(GameStatus.Idle));
    }

    [Test]
    public void LevelInfo_LevelNames_Has5Entries()
    {
        Assert.That(LevelInfo.LevelNames, Has.Length.EqualTo(5));
    }

    [Test]
    public void Puzzle_Roundtrip()
    {
        var puzzle = new Puzzle
        {
            OriginalWord = "gold",
            ScrambledWord = "dlgo",
            DifficultyTier = 1
        };
        Assert.That(puzzle.OriginalWord, Is.EqualTo("gold"));
        Assert.That(puzzle.ScrambledWord, Is.EqualTo("dlgo"));
        Assert.That(puzzle.DifficultyTier, Is.EqualTo(1));
        Assert.That(puzzle.Solved, Is.False);
        Assert.That(puzzle.SolvedByUserId, Is.Null);
    }
}
