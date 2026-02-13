using Arcade.Heist.Configuration;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Tests.Game.Engine;

[TestFixture]
public class PlayerManagerTests
{
    private PlayerManager _manager = null!;
    private HeistOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new HeistOptions();
        _manager = new PlayerManager(
            Options.Create(_options),
            NullLogger<PlayerManager>.Instance);
    }

    // --- CreatePlayer ---

    [Test]
    public void CreatePlayer_SetsUserId()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.UserId, Is.EqualTo(42));
    }

    [Test]
    public void CreatePlayer_SetsDisplayName()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.DisplayName, Is.EqualTo("Alice"));
    }

    [Test]
    public void CreatePlayer_StartsAtLevel1()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
    }

    [Test]
    public void CreatePlayer_StartsAtRoom1()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    [Test]
    public void CreatePlayer_EmptyCards()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.Cards, Is.Empty);
    }

    [Test]
    public void CreatePlayer_ZeroWrongGuesses()
    {
        var player = _manager.CreatePlayer(42, "Alice");
        Assert.That(player.WrongGuessCount, Is.EqualTo(0));
    }

    // --- AdvancePlayer ---

    [Test]
    public void AdvancePlayer_FirstRoom_AdvancesToRoom2()
    {
        var player = _manager.CreatePlayer(1, "Bob");
        player.CurrentRoom = 1;
        _manager.AdvancePlayer(player);
        Assert.That(player.CurrentRoom, Is.EqualTo(2));
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
    }

    [Test]
    public void AdvancePlayer_SecondRoom_AdvancesToRoom3()
    {
        var player = _manager.CreatePlayer(1, "Bob");
        player.CurrentRoom = 2;
        _manager.AdvancePlayer(player);
        Assert.That(player.CurrentRoom, Is.EqualTo(3));
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
    }

    [Test]
    public void AdvancePlayer_LastRoom_AdvancesToNextLevel()
    {
        var player = _manager.CreatePlayer(1, "Bob");
        player.CurrentRoom = 3; // RoomsPerLevel default is 3
        _manager.AdvancePlayer(player);
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
        Assert.That(player.CurrentLevel, Is.EqualTo(2));
    }

    [Test]
    public void AdvancePlayer_ResetsConsecutiveWrongCount()
    {
        var player = _manager.CreatePlayer(1, "Bob");
        player.ConsecutiveWrongCount = 1;
        _manager.AdvancePlayer(player);
        Assert.That(player.ConsecutiveWrongCount, Is.EqualTo(0));
    }

    [Test]
    public void AdvancePlayer_IncrementsRoomsCleared()
    {
        var player = _manager.CreatePlayer(1, "Bob");
        _manager.AdvancePlayer(player);
        Assert.That(player.RoomsCleared, Is.EqualTo(1));
    }

    // --- PenalizePlayer ---

    [Test]
    public void PenalizePlayer_FirstWrong_DropsOneLevel()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 3;
        player.CurrentRoom = 2;
        _manager.PenalizePlayer(player);
        Assert.That(player.CurrentLevel, Is.EqualTo(2));
    }

    [Test]
    public void PenalizePlayer_ResetsRoomTo1()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 3;
        player.CurrentRoom = 2;
        _manager.PenalizePlayer(player);
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    [Test]
    public void PenalizePlayer_SecondConsecutive_DropsTwoLevels()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 5;
        player.CurrentRoom = 3;
        player.ConsecutiveWrongCount = 1; // Already had one wrong
        _manager.PenalizePlayer(player);
        Assert.That(player.CurrentLevel, Is.EqualTo(3));
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    [Test]
    public void PenalizePlayer_NeverBelowLevel1()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 1;
        player.CurrentRoom = 2;
        _manager.PenalizePlayer(player);
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    [Test]
    public void PenalizePlayer_SetsCooldown()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 3;
        _manager.PenalizePlayer(player);
        Assert.That(player.IsOnCooldown, Is.True);
    }

    [Test]
    public void PenalizePlayer_IncrementsWrongGuessCount()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 3;
        _manager.PenalizePlayer(player);
        Assert.That(player.WrongGuessCount, Is.EqualTo(1));
    }

    [Test]
    public void PenalizePlayer_DoubleDropNeverBelowLevel1()
    {
        var player = _manager.CreatePlayer(1, "Carol");
        player.CurrentLevel = 2;
        player.ConsecutiveWrongCount = 1;
        _manager.PenalizePlayer(player);
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    // --- ShouldAwardCard ---

    [Test]
    public void ShouldAwardCard_AtInterval_ReturnsTrue()
    {
        var player = _manager.CreatePlayer(1, "Dave");
        player.RoomsCleared = _options.CardAwardInterval; // 3
        Assert.That(_manager.ShouldAwardCard(player), Is.True);
    }

    [Test]
    public void ShouldAwardCard_AtZero_ReturnsFalse()
    {
        var player = _manager.CreatePlayer(1, "Dave");
        player.RoomsCleared = 0;
        Assert.That(_manager.ShouldAwardCard(player), Is.False);
    }

    [Test]
    public void ShouldAwardCard_NotAtInterval_ReturnsFalse()
    {
        var player = _manager.CreatePlayer(1, "Dave");
        player.RoomsCleared = 2;
        Assert.That(_manager.ShouldAwardCard(player), Is.False);
    }

    // --- ApplyCooldown ---

    [Test]
    public void ApplyCooldown_SetsExpiry()
    {
        var player = _manager.CreatePlayer(1, "Eve");
        _manager.ApplyCooldown(player, 30);
        Assert.That(player.IsOnCooldown, Is.True);
    }

    // --- HasWon ---

    [Test]
    public void HasWon_AboveMax_ReturnsTrue()
    {
        var player = _manager.CreatePlayer(1, "Frank");
        player.CurrentLevel = 6;
        Assert.That(_manager.HasWon(player, 5), Is.True);
    }

    [Test]
    public void HasWon_AtMax_ReturnsFalse()
    {
        var player = _manager.CreatePlayer(1, "Frank");
        player.CurrentLevel = 5;
        Assert.That(_manager.HasWon(player, 5), Is.False);
    }
}
