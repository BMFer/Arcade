using Arcade.Heist.Configuration;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;
using Arcade.Heist.Game.Words;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Tests.Game.Engine;

[TestFixture]
public class CardManagerTests
{
    private CardManager _cardManager = null!;
    private PuzzleManager _puzzleManager = null!;
    private HeistOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new HeistOptions();
        _cardManager = new CardManager(Options.Create(_options));
        _puzzleManager = new PuzzleManager(new WordBank(), new WordScrambler());
    }

    // --- HasCard ---

    [Test]
    public void HasCard_WhenPresent_ReturnsTrue()
    {
        var player = MakePlayer();
        player.Cards.Add(PowerCard.Shield);
        Assert.That(_cardManager.HasCard(player, PowerCard.Shield), Is.True);
    }

    [Test]
    public void HasCard_WhenAbsent_ReturnsFalse()
    {
        var player = MakePlayer();
        Assert.That(_cardManager.HasCard(player, PowerCard.Shield), Is.False);
    }

    // --- RemoveCard ---

    [Test]
    public void RemoveCard_RemovesOnlyOneInstance()
    {
        var player = MakePlayer();
        player.Cards.Add(PowerCard.Shield);
        player.Cards.Add(PowerCard.Shield);
        _cardManager.RemoveCard(player, PowerCard.Shield);
        Assert.That(player.Cards.Count(c => c == PowerCard.Shield), Is.EqualTo(1));
    }

    // --- AwardRandomCard ---

    [Test]
    public void AwardRandomCard_ReturnsValidCard()
    {
        var card = _cardManager.AwardRandomCard();
        Assert.That(Enum.IsDefined(card), Is.True);
    }

    // --- UseKnockback ---

    [Test]
    public void UseKnockback_DropsTarget()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Knockback);
        var target = MakePlayer(2);
        target.CurrentLevel = 3;
        target.CurrentRoom = 2;

        var result = _cardManager.UseKnockback(user, target, 5);

        Assert.That(result.Success, Is.True);
        Assert.That(target.CurrentLevel, Is.EqualTo(2));
        Assert.That(target.CurrentRoom, Is.EqualTo(1));
        Assert.That(result.TargetMoved, Is.True);
    }

    [Test]
    public void UseKnockback_ShieldBlocks()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Knockback);
        var target = MakePlayer(2);
        target.CurrentLevel = 3;
        target.ShieldActive = true;

        var result = _cardManager.UseKnockback(user, target, 5);

        Assert.That(result.Blocked, Is.True);
        Assert.That(target.CurrentLevel, Is.EqualTo(3));
        Assert.That(target.ShieldActive, Is.False);
    }

    [Test]
    public void UseKnockback_NeverBelowLevel1()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Knockback);
        var target = MakePlayer(2);
        target.CurrentLevel = 1;
        target.CurrentRoom = 1;

        var result = _cardManager.UseKnockback(user, target, 5);

        Assert.That(target.CurrentLevel, Is.EqualTo(1));
        Assert.That(target.CurrentRoom, Is.EqualTo(1));
        Assert.That(result.TargetMoved, Is.False);
    }

    [Test]
    public void UseKnockback_AtLevel1Room2_ResetsToRoom1()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Knockback);
        var target = MakePlayer(2);
        target.CurrentLevel = 1;
        target.CurrentRoom = 2;

        var result = _cardManager.UseKnockback(user, target, 5);

        Assert.That(target.CurrentLevel, Is.EqualTo(1));
        Assert.That(target.CurrentRoom, Is.EqualTo(1));
        Assert.That(result.TargetMoved, Is.True);
    }

    [Test]
    public void UseKnockback_RemovesCardFromUser()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Knockback);
        var target = MakePlayer(2);
        target.CurrentLevel = 3;

        _cardManager.UseKnockback(user, target, 5);

        Assert.That(user.Cards, Does.Not.Contain(PowerCard.Knockback));
    }

    // --- UseShield ---

    [Test]
    public void UseShield_ActivatesShield()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Shield);

        var result = _cardManager.UseShield(user);

        Assert.That(result.Success, Is.True);
        Assert.That(user.ShieldActive, Is.True);
    }

    [Test]
    public void UseShield_RemovesCard()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Shield);
        _cardManager.UseShield(user);
        Assert.That(user.Cards, Does.Not.Contain(PowerCard.Shield));
    }

    // --- UseFreeze ---

    [Test]
    public void UseFreeze_SetsCooldownOnTarget()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Freeze);
        var target = MakePlayer(2);

        var result = _cardManager.UseFreeze(user, target);

        Assert.That(result.Success, Is.True);
        Assert.That(target.IsOnCooldown, Is.True);
    }

    [Test]
    public void UseFreeze_RemovesCard()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Freeze);
        var target = MakePlayer(2);
        _cardManager.UseFreeze(user, target);
        Assert.That(user.Cards, Does.Not.Contain(PowerCard.Freeze));
    }

    // --- UseSpy ---

    [Test]
    public void UseSpy_WithPuzzle_RevealsLetter()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Spy);
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };

        var result = _cardManager.UseSpy(user, puzzle, _puzzleManager);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("Spy reveals"));
        Assert.That(result.IsPrivate, Is.True);
    }

    [Test]
    public void UseSpy_NullPuzzle_Fails()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Spy);

        var result = _cardManager.UseSpy(user, null, _puzzleManager);

        Assert.That(result.Success, Is.False);
    }

    // --- UseChaos ---

    [Test]
    public void UseChaos_RescramblesPuzzle()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Chaos);
        var target = MakePlayer(2);
        target.CurrentLevel = 1;
        target.CurrentRoom = 1;
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };
        var puzzles = new Dictionary<(int Level, int Room), Puzzle> { [(1, 1)] = puzzle };

        var result = _cardManager.UseChaos(user, target, puzzles, _puzzleManager);

        Assert.That(result.Success, Is.True);
        Assert.That(result.NewPuzzle, Is.Not.Null);
        Assert.That(result.NewPuzzle!.OriginalWord, Is.EqualTo("treasure"));
    }

    [Test]
    public void UseChaos_NoPuzzle_Fails()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Chaos);
        var target = MakePlayer(2);
        target.CurrentLevel = 1;
        target.CurrentRoom = 1;
        var puzzles = new Dictionary<(int Level, int Room), Puzzle>();

        var result = _cardManager.UseChaos(user, target, puzzles, _puzzleManager);

        Assert.That(result.Success, Is.False);
    }

    // --- UseHint ---

    [Test]
    public void UseHint_WithPuzzle_ReturnsHint()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Hint);
        var puzzle = new Puzzle { OriginalWord = "treasure", ScrambledWord = "ertasure", DifficultyTier = 3 };

        var result = _cardManager.UseHint(user, puzzle, _puzzleManager);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("Hint"));
        Assert.That(result.IsPrivate, Is.True);
    }

    [Test]
    public void UseHint_NullPuzzle_Fails()
    {
        var user = MakePlayer();
        user.Cards.Add(PowerCard.Hint);

        var result = _cardManager.UseHint(user, null, _puzzleManager);

        Assert.That(result.Success, Is.False);
    }

    private static PlayerState MakePlayer(ulong id = 1) => new()
    {
        UserId = id,
        DisplayName = $"Player{id}",
        CurrentLevel = 1,
        CurrentRoom = 1
    };
}
