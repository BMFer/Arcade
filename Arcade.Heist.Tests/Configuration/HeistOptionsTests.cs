using Arcade.Heist.Configuration;

namespace Arcade.Heist.Tests.Configuration;

[TestFixture]
public class HeistOptionsTests
{
    private HeistOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new HeistOptions();
    }

    [Test]
    public void LobbyCountdownSeconds_Default10()
    {
        Assert.That(_options.LobbyCountdownSeconds, Is.EqualTo(10));
    }

    [Test]
    public void WrongGuessCooldownSeconds_Default10()
    {
        Assert.That(_options.WrongGuessCooldownSeconds, Is.EqualTo(10));
    }

    [Test]
    public void FreezeDurationSeconds_Default60()
    {
        Assert.That(_options.FreezeDurationSeconds, Is.EqualTo(60));
    }

    [Test]
    public void CrownHoldSeconds_Default60()
    {
        Assert.That(_options.CrownHoldSeconds, Is.EqualTo(60));
    }

    [Test]
    public void CardAwardInterval_Default3()
    {
        Assert.That(_options.CardAwardInterval, Is.EqualTo(3));
    }

    [Test]
    public void MaxLevels_Default5()
    {
        Assert.That(_options.MaxLevels, Is.EqualTo(5));
    }

    [Test]
    public void TowerDataPath_DefaultDataTowerJson()
    {
        Assert.That(_options.TowerDataPath, Is.EqualTo("data/tower.json"));
    }
}
