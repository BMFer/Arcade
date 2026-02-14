using Arcade.Heist.Discord.Commands;
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
    public void LevelInfo_HasEmptyRoomsList()
    {
        var level = new LevelInfo { LevelNumber = 1, Name = "Basement", DifficultyTier = 1 };
        Assert.That(level.Rooms, Is.Empty);
    }

    [Test]
    public void RoomInfo_Roundtrip()
    {
        var room = new RoomInfo { RoomNumber = 1, ChannelId = 123, RoleId = 456 };
        Assert.That(room.RoomNumber, Is.EqualTo(1));
        Assert.That(room.ChannelId, Is.EqualTo(123));
        Assert.That(room.RoleId, Is.EqualTo(456));
    }

    [Test]
    public void GameState_ActivePuzzles_DefaultEmpty()
    {
        var state = new GameState();
        Assert.That(state.ActivePuzzles, Is.Empty);
    }

    [Test]
    public void GameState_Players_DefaultEmpty()
    {
        var state = new GameState();
        Assert.That(state.Players, Is.Empty);
    }

    [Test]
    public void GameState_Levels_DefaultEmpty()
    {
        var state = new GameState();
        Assert.That(state.Levels, Is.Empty);
    }

    [Test]
    public void HostAction_HasGrantAndRevoke()
    {
        Assert.That(Enum.IsDefined(typeof(HostAction), HostAction.Grant), Is.True);
        Assert.That(Enum.IsDefined(typeof(HostAction), HostAction.Revoke), Is.True);
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
