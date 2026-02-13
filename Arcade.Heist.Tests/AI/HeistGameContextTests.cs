using Arcade.Heist.AI;
using Arcade.Heist.Game.Models;

namespace Arcade.Heist.Tests.AI;

[TestFixture]
public class HeistGameContextTests
{
    [Test]
    public void Format_IncludesLevelRoomAndCards()
    {
        var player = MakePlayer();
        player.Cards.Add(PowerCard.Shield);
        player.Cards.Add(PowerCard.Spy);

        var result = HeistGameContext.Format(player);

        Assert.That(result, Does.Contain("level 1"));
        Assert.That(result, Does.Contain("room 1"));
        Assert.That(result, Does.Contain("2 power card(s)"));
    }

    [Test]
    public void Format_IncludesWrongGuessesAndRoomsCleared()
    {
        var player = MakePlayer();
        player.WrongGuessCount = 3;
        player.RoomsCleared = 5;

        var result = HeistGameContext.Format(player);

        Assert.That(result, Does.Contain("Wrong guesses so far: 3"));
        Assert.That(result, Does.Contain("Rooms cleared: 5"));
    }

    [Test]
    public void Format_OnCooldown_AppendsCooldownInfo()
    {
        var player = MakePlayer();
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(30);

        var result = HeistGameContext.Format(player);

        Assert.That(result, Does.Contain("cooldown"));
    }

    [Test]
    public void Format_NotOnCooldown_NoCooldownSubstring()
    {
        var player = MakePlayer();

        var result = HeistGameContext.Format(player);

        Assert.That(result, Does.Not.Contain("cooldown"));
    }

    [Test]
    public void Format_WithShield_AppendsShieldInfo()
    {
        var player = MakePlayer();
        player.ShieldActive = true;

        var result = HeistGameContext.Format(player);

        Assert.That(result, Does.Contain("shield"));
    }

    private static PlayerState MakePlayer() => new()
    {
        UserId = 1,
        DisplayName = "TestPlayer",
        CurrentLevel = 1,
        CurrentRoom = 1
    };
}
