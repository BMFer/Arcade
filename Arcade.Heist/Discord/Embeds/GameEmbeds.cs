using Arcade.Core.Models;
using Discord;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;

namespace Arcade.Heist.Discord.Embeds;

public static class GameEmbeds
{
    public static Embed PuzzleEmbed(Puzzle puzzle, int level, int room)
    {
        var builder = new EmbedBuilder()
            .WithTitle($"Level {level} — Room {room} — Unscramble!")
            .WithDescription($"```\n{puzzle.ScrambledWord.ToUpperInvariant()}\n```")
            .WithColor(GetLevelColor(level))
            .WithFooter($"Tier {puzzle.DifficultyTier} | Type your answer in chat");

        if (puzzle.DifficultyTier == 5)
            builder.AddField("Mode", "Double Scramble — Two words!", true);

        return builder.Build();
    }

    public static Embed CorrectAnswerEmbed(PlayerState player, Puzzle puzzle, int previousLevel, int previousRoom, PowerCard? awardedCard)
    {
        var fromText = $"L{previousLevel} Room {previousRoom}";
        var toText = $"L{player.CurrentLevel} Room {player.CurrentRoom}";

        var builder = new EmbedBuilder()
            .WithTitle("Correct!")
            .WithDescription($"**{player.DisplayName}** solved it! The answer was **{puzzle.OriginalWord}**.")
            .WithColor(Color.Green)
            .AddField("Advancing", $"{fromText} -> {toText}", true);

        if (awardedCard.HasValue)
            builder.AddField("Card Earned!", $"You earned a **{awardedCard.Value}** card!", true);

        return builder.Build();
    }

    public static Embed WrongAnswerEmbed(PlayerState player, int previousLevel)
    {
        var dropLevels = previousLevel - player.CurrentLevel;
        var builder = new EmbedBuilder()
            .WithTitle("Wrong!")
            .WithDescription($"**{player.DisplayName}** guessed wrong!")
            .WithColor(Color.Red)
            .AddField("Penalty", $"Dropped {dropLevels} level(s) to level {player.CurrentLevel} room 1", true)
            .AddField("Cooldown", $"{(int)player.CooldownRemaining.TotalSeconds}s", true);

        return builder.Build();
    }

    public static Embed CooldownEmbed(PlayerState player, int secondsRemaining)
    {
        return new EmbedBuilder()
            .WithDescription($"**{player.DisplayName}**, you're on cooldown! Wait **{secondsRemaining}s**.")
            .WithColor(Color.Orange)
            .Build();
    }

    public static Embed WinEmbed(PlayerState player)
    {
        return new EmbedBuilder()
            .WithTitle("HEIST COMPLETE!")
            .WithDescription($"**{player.DisplayName}** has cleared the Crown Room and won the heist!")
            .WithColor(Color.Gold)
            .AddField("Stats", $"Total wrong guesses: {player.WrongGuessCount}\nRooms cleared: {player.RoomsCleared}", false)
            .Build();
    }

    public static Embed LobbyEmbed(IEnumerable<PlayerState> players)
    {
        var playerList = string.Join("\n", players.Select(p => $"- {p.DisplayName}"));
        if (string.IsNullOrEmpty(playerList))
            playerList = "_No players yet_";

        return new EmbedBuilder()
            .WithTitle("Heist Lobby")
            .WithDescription("A heist is being planned! Use `/heist-join` to join.")
            .WithColor(Color.Blue)
            .AddField("Players", playerList)
            .Build();
    }

    public static Embed PlayerJoinedEmbed(string displayName)
    {
        return new EmbedBuilder()
            .WithTitle("New Player!")
            .WithDescription($"**{displayName}** has joined the heist and is starting at Level 1 Room 1!")
            .WithColor(Color.Green)
            .Build();
    }

    public static Embed StatusEmbed(GameState game)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Heist Status")
            .WithColor(Color.Purple);

        if (game.Status != GameStatus.Active)
        {
            builder.WithDescription($"Game status: **{game.Status}**");
            return builder.Build();
        }

        var playersByLevel = game.Players.Values
            .OrderByDescending(p => p.CurrentLevel)
            .ThenByDescending(p => p.CurrentRoom)
            .GroupBy(p => p.CurrentLevel);

        foreach (var group in playersByLevel)
        {
            var level = game.Levels.FirstOrDefault(l => l.LevelNumber == group.Key);
            var levelName = level?.Name ?? $"Level {group.Key}";
            var players = string.Join(", ", group.Select(p =>
            {
                var status = p.IsOnCooldown ? " (frozen)" : "";
                var shield = p.ShieldActive ? " [shielded]" : "";
                return $"{p.DisplayName} (R{p.CurrentRoom}){status}{shield}";
            }));
            builder.AddField($"L{group.Key}: {levelName}", players, false);
        }

        return builder.Build();
    }

    public static Embed CardUsedEmbed(CardResult result)
    {
        return new EmbedBuilder()
            .WithTitle("Card Used!")
            .WithDescription(result.Message)
            .WithColor(Color.Magenta)
            .Build();
    }

    public static Embed CardInventoryEmbed(PlayerState player)
    {
        var cards = player.Cards.Count > 0
            ? string.Join("\n", player.Cards.GroupBy(c => c).Select(g => $"- **{g.Key}** x{g.Count()}"))
            : "_No cards_";

        return new EmbedBuilder()
            .WithTitle($"{player.DisplayName}'s Cards")
            .WithDescription(cards)
            .WithColor(Color.Teal)
            .Build();
    }

    public static Embed AssistantSelectionEmbed(IEnumerable<AssistantProfile> profiles)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Choose Your Assistant")
            .WithDescription("Select an AI assistant to accompany you on the heist. They'll provide commentary and answer your questions.")
            .WithColor(Color.Teal);

        foreach (var profile in profiles)
            builder.AddField(profile.Name, profile.Description, true);

        return builder.Build();
    }

    public static Embed AssistantResponseEmbed(AssistantProfile profile, string response)
    {
        return new EmbedBuilder()
            .WithAuthor(profile.Name, profile.AvatarUrl)
            .WithDescription(response)
            .WithColor(Color.DarkTeal)
            .Build();
    }

    private static Color GetLevelColor(int level) => level switch
    {
        1 => Color.Green,
        2 => Color.Blue,
        3 => Color.Orange,
        4 => Color.Red,
        5 => Color.Gold,
        _ => Color.Default
    };
}
