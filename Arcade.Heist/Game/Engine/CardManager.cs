using Arcade.Heist.Configuration;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Game.Engine;

public class CardManager
{
    private static readonly Random _rng = new();
    private readonly HeistOptions _options;

    public CardManager(IOptions<HeistOptions> options)
    {
        _options = options.Value;
    }

    public PowerCard AwardRandomCard()
    {
        var cards = Enum.GetValues<PowerCard>();
        return cards[_rng.Next(cards.Length)];
    }

    public bool HasCard(PlayerState player, PowerCard card)
    {
        return player.Cards.Contains(card);
    }

    public void RemoveCard(PlayerState player, PowerCard card)
    {
        player.Cards.Remove(card);
    }

    public CardResult UseKnockback(PlayerState user, PlayerState target, int maxLevels)
    {
        RemoveCard(user, PowerCard.Knockback);

        if (target.ShieldActive)
        {
            target.ShieldActive = false;
            return new CardResult
            {
                Success = true,
                Blocked = true,
                Message = $"**{user.DisplayName}** used Knockback on **{target.DisplayName}**, but their Shield absorbed it!"
            };
        }

        var oldLevel = target.CurrentLevel;
        var oldRoom = target.CurrentRoom;
        target.CurrentLevel = Math.Max(1, target.CurrentLevel - 1);
        target.CurrentRoom = 1;
        return new CardResult
        {
            Success = true,
            Message = $"**{user.DisplayName}** knocked **{target.DisplayName}** from level {oldLevel} down to level {target.CurrentLevel} room 1!",
            TargetMoved = oldLevel != target.CurrentLevel || oldRoom != target.CurrentRoom,
            TargetOldLevel = oldLevel,
            TargetOldRoom = oldRoom,
            TargetNewLevel = target.CurrentLevel,
            TargetNewRoom = target.CurrentRoom
        };
    }

    public CardResult UseShield(PlayerState user)
    {
        RemoveCard(user, PowerCard.Shield);
        user.ShieldActive = true;
        return new CardResult
        {
            Success = true,
            Message = $"**{user.DisplayName}** activated a Shield! Next knockback will be blocked."
        };
    }

    public CardResult UseFreeze(PlayerState user, PlayerState target)
    {
        RemoveCard(user, PowerCard.Freeze);
        target.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(_options.FreezeDurationSeconds);
        return new CardResult
        {
            Success = true,
            Message = $"**{user.DisplayName}** froze **{target.DisplayName}** for {_options.FreezeDurationSeconds} seconds!"
        };
    }

    public CardResult UseSpy(PlayerState user, Puzzle? puzzle, PuzzleManager puzzleManager)
    {
        RemoveCard(user, PowerCard.Spy);
        if (puzzle == null)
            return new CardResult { Success = false, Message = "No active puzzle to spy on." };

        var reveal = puzzleManager.RevealLetter(puzzle);
        return new CardResult
        {
            Success = true,
            Message = $"**{user.DisplayName}** used Spy! {reveal}",
            IsPrivate = true
        };
    }

    public CardResult UseChaos(PlayerState user, PlayerState target, Dictionary<(int Level, int Room), Puzzle> activePuzzles, PuzzleManager puzzleManager)
    {
        RemoveCard(user, PowerCard.Chaos);
        var key = (target.CurrentLevel, target.CurrentRoom);
        if (activePuzzles.TryGetValue(key, out var puzzle))
        {
            var newPuzzle = puzzleManager.Rescramble(puzzle);
            activePuzzles[key] = newPuzzle;
            return new CardResult
            {
                Success = true,
                Message = $"**{user.DisplayName}** caused Chaos for level {target.CurrentLevel} room {target.CurrentRoom}! The puzzle has been re-scrambled!",
                NewPuzzle = newPuzzle
            };
        }
        return new CardResult { Success = false, Message = "Target has no active puzzle to chaos." };
    }

    public CardResult UseHint(PlayerState user, Puzzle? puzzle, PuzzleManager puzzleManager)
    {
        RemoveCard(user, PowerCard.Hint);
        if (puzzle == null)
            return new CardResult { Success = false, Message = "No active puzzle for a hint." };

        var hint = puzzleManager.GetHint(puzzle);
        return new CardResult
        {
            Success = true,
            Message = $"**{user.DisplayName}** used a Hint! {hint}",
            IsPrivate = true
        };
    }
}

public class CardResult
{
    public bool Success { get; init; }
    public bool Blocked { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool TargetMoved { get; init; }
    public int TargetOldLevel { get; init; }
    public int TargetOldRoom { get; init; }
    public int TargetNewLevel { get; init; }
    public int TargetNewRoom { get; init; }
    public bool IsPrivate { get; init; }
    public Puzzle? NewPuzzle { get; init; }
}
