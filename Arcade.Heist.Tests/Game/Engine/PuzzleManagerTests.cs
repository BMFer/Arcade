using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;
using Arcade.Heist.Game.Words;

namespace Arcade.Heist.Tests.Game.Engine;

[TestFixture]
public class PuzzleManagerTests
{
    private PuzzleManager _manager = null!;

    [SetUp]
    public void Setup()
    {
        _manager = new PuzzleManager(new WordBank(), new WordScrambler());
    }

    // --- CheckAnswer ---

    [Test]
    public void CheckAnswer_Correct_CaseInsensitive()
    {
        var puzzle = new Puzzle { OriginalWord = "gold", ScrambledWord = "dlgo", DifficultyTier = 1 };
        Assert.That(_manager.CheckAnswer(puzzle, "GOLD"), Is.True);
    }

    [Test]
    public void CheckAnswer_Correct_WhitespaceTrimmed()
    {
        var puzzle = new Puzzle { OriginalWord = "gold", ScrambledWord = "dlgo", DifficultyTier = 1 };
        Assert.That(_manager.CheckAnswer(puzzle, "  gold  "), Is.True);
    }

    [Test]
    public void CheckAnswer_Wrong_ReturnsFalse()
    {
        var puzzle = new Puzzle { OriginalWord = "gold", ScrambledWord = "dlgo", DifficultyTier = 1 };
        Assert.That(_manager.CheckAnswer(puzzle, "silver"), Is.False);
    }

    // --- GetHint ---

    [Test]
    public void GetHint_LongWord_RevealsFirstAndLast()
    {
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };
        var hint = _manager.GetHint(puzzle);
        Assert.That(hint, Does.Contain("t"));
        Assert.That(hint, Does.Contain("e"));
        Assert.That(hint, Does.Contain("8 letters"));
    }

    [Test]
    public void GetHint_ShortWord_RevealsFirstLetter()
    {
        var puzzle = new Puzzle { OriginalWord = "cat", ScrambledWord = "tac", DifficultyTier = 1 };
        var hint = _manager.GetHint(puzzle);
        Assert.That(hint, Does.Contain("c"));
    }

    // --- GeneratePuzzle ---

    [Test]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void GeneratePuzzle_Tiers1To4_SingleWord(int level)
    {
        var puzzle = _manager.GeneratePuzzle(level);
        Assert.That(puzzle.OriginalWord, Does.Not.Contain(" "));
        Assert.That(puzzle.DifficultyTier, Is.EqualTo(level));
    }

    [Test]
    public void GeneratePuzzle_Tier5_DoubleWord()
    {
        var puzzle = _manager.GeneratePuzzle(5);
        Assert.That(puzzle.OriginalWord, Does.Contain(" "));
        Assert.That(puzzle.DifficultyTier, Is.EqualTo(5));
    }

    // --- Rescramble ---

    [Test]
    public void Rescramble_SameOriginalWord()
    {
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };
        var rescrambled = _manager.Rescramble(puzzle);
        Assert.That(rescrambled.OriginalWord, Is.EqualTo("treasure"));
    }

    [Test]
    public void Rescramble_Tier5_DoubleScramble()
    {
        var puzzle = new Puzzle { OriginalWord = "crown jewels", ScrambledWord = "nwocr slweje", DifficultyTier = 5 };
        var rescrambled = _manager.Rescramble(puzzle);
        Assert.That(rescrambled.OriginalWord, Is.EqualTo("crown jewels"));
        Assert.That(rescrambled.ScrambledWord, Does.Contain(" "));
    }

    // --- RevealLetter ---

    [Test]
    public void RevealLetter_ContainsSpyReveals()
    {
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };
        var reveal = _manager.RevealLetter(puzzle);
        Assert.That(reveal, Does.Contain("Spy reveals"));
    }
}
