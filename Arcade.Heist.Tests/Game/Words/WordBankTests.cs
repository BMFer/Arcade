using Arcade.Heist.Game.Words;

namespace Arcade.Heist.Tests.Game.Words;

[TestFixture]
public class WordBankTests
{
    private WordBank _bank = null!;

    [SetUp]
    public void Setup()
    {
        _bank = new WordBank();
    }

    [Test]
    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(3, 3)]
    [TestCase(5, 5)]
    [TestCase(10, 5)]
    public void GetTierForLevel_ClampsTo1Through5(int level, int expected)
    {
        Assert.That(_bank.GetTierForLevel(level), Is.EqualTo(expected));
    }

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void GetWord_ReturnsNonEmptyString(int tier)
    {
        var word = _bank.GetWord(tier);
        Assert.That(word, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public void GetWordPair_ReturnsTwoElements()
    {
        var pair = _bank.GetWordPair();
        Assert.That(pair, Has.Length.EqualTo(2));
        Assert.That(pair[0], Is.Not.Null.And.Not.Empty);
        Assert.That(pair[1], Is.Not.Null.And.Not.Empty);
    }
}
