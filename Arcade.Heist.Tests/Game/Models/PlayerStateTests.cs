using Arcade.Heist.Game.Models;

namespace Arcade.Heist.Tests.Game.Models;

[TestFixture]
public class PlayerStateTests
{
    [Test]
    public void CurrentRoom_DefaultIs1()
    {
        var player = new PlayerState { UserId = 1, DisplayName = "Test" };
        Assert.That(player.CurrentRoom, Is.EqualTo(1));
    }

    [Test]
    public void CurrentLevel_DefaultIs1()
    {
        var player = new PlayerState { UserId = 1, DisplayName = "Test" };
        Assert.That(player.CurrentLevel, Is.EqualTo(1));
    }

    [Test]
    public void RoomsCleared_DefaultIs0()
    {
        var player = new PlayerState { UserId = 1, DisplayName = "Test" };
        Assert.That(player.RoomsCleared, Is.EqualTo(0));
    }

    [Test]
    public void IsOnCooldown_FutureExpiry_ReturnsTrue()
    {
        var player = new PlayerState
        {
            UserId = 1,
            DisplayName = "Test",
            CooldownExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };
        Assert.That(player.IsOnCooldown, Is.True);
    }

    [Test]
    public void IsOnCooldown_PastExpiry_ReturnsFalse()
    {
        var player = new PlayerState
        {
            UserId = 1,
            DisplayName = "Test",
            CooldownExpiry = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        Assert.That(player.IsOnCooldown, Is.False);
    }

    [Test]
    public void CooldownRemaining_WhenOnCooldown_Positive()
    {
        var player = new PlayerState
        {
            UserId = 1,
            DisplayName = "Test",
            CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(30)
        };
        Assert.That(player.CooldownRemaining.TotalSeconds, Is.GreaterThan(0));
    }

    [Test]
    public void CooldownRemaining_WhenNotOnCooldown_Zero()
    {
        var player = new PlayerState
        {
            UserId = 1,
            DisplayName = "Test",
            CooldownExpiry = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        Assert.That(player.CooldownRemaining, Is.EqualTo(TimeSpan.Zero));
    }
}
