using Arcade.Heist.Game.Words;

namespace Arcade.Heist.Tests.Game.Words;

[TestFixture]
public class WordScramblerTests
{
    private WordScrambler _scrambler = null!;

    [SetUp]
    public void Setup()
    {
        _scrambler = new WordScrambler();
    }

    [Test]
    public void Scramble_SameLength()
    {
        var result = _scrambler.Scramble("treasure");
        Assert.That(result, Has.Length.EqualTo(8));
    }

    [Test]
    public void Scramble_SameCharactersSorted()
    {
        var word = "treasure";
        var result = _scrambler.Scramble(word);
        Assert.That(
            string.Concat(result.OrderBy(c => c)),
            Is.EqualTo(string.Concat(word.ToLowerInvariant().OrderBy(c => c))));
    }

    [Test]
    public void Scramble_DiffersFromOriginal()
    {
        var word = "treasure";
        var result = _scrambler.Scramble(word);
        Assert.That(result, Is.Not.EqualTo(word.ToLowerInvariant()));
    }

    [Test]
    public void ScrambleDouble_TwoWordsSeparatedBySpace()
    {
        var result = _scrambler.ScrambleDouble(["crown", "jewels"]);
        var parts = result.Split(' ');
        Assert.That(parts, Has.Length.EqualTo(2));
        Assert.That(
            string.Concat(parts[0].OrderBy(c => c)),
            Is.EqualTo(string.Concat("crown".OrderBy(c => c))));
        Assert.That(
            string.Concat(parts[1].OrderBy(c => c)),
            Is.EqualTo(string.Concat("jewels".OrderBy(c => c))));
    }
}
