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

    public static Embed AssistantSelectionEmbed(IEnumerable<AssistantProfile> profiles, string? bannerFileName = null)
    {
        var builder = new EmbedBuilder()
            .WithTitle("Choose Your Assistant")
            .WithDescription("Select an AI assistant to accompany you on the heist. They'll provide commentary and answer your questions.")
            .WithColor(Color.Teal);

        if (bannerFileName != null)
            builder.WithImageUrl($"attachment://{bannerFileName}");

        foreach (var profile in profiles)
            builder.AddField(profile.Name, profile.Description, true);

        return builder.Build();
    }

    public static Embed AssistantResponseEmbed(AssistantProfile profile, string response, string? bannerFileName = null)
    {
        var builder = new EmbedBuilder()
            .WithAuthor(profile.Name, profile.AvatarUrl)
            .WithDescription(response)
            .WithColor(Color.DarkTeal);

        if (bannerFileName != null)
            builder.WithImageUrl($"attachment://{bannerFileName}");

        return builder.Build();
    }

    private static readonly Random _bannerRandom = new();

    public static FileAttachment? GetBannerAttachment(AssistantProfile profile)
    {
        if (string.IsNullOrEmpty(profile.BannerPath))
            return null;

        var dirPath = Path.Combine(AppContext.BaseDirectory, profile.BannerPath);
        if (!Directory.Exists(dirPath))
            return null;

        var images = Directory.GetFiles(dirPath, "*.png");
        if (images.Length == 0)
            return null;

        var chosen = images[_bannerRandom.Next(images.Length)];
        return new FileAttachment(chosen, Path.GetFileName(chosen));
    }

    public static FileAttachment? GetDialogBanner(string fileName)
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Images", "Dialogs", fileName);
        if (!File.Exists(fullPath))
            return null;

        return new FileAttachment(fullPath, fileName);
    }

    public static Embed HelpEmbed()
    {
        return new EmbedBuilder()
            .WithTitle("How to Play — Heist Tower")
            .WithDescription(
                "Race to the top of a 5-level tower by solving word scrambles! " +
                "Each level has **3 rooms**, each with its own puzzle. Solve all 3 to advance to the next level. " +
                "First player to clear all rooms of the **Crown Room** wins.")
            .WithColor(Color.Blue)
            .AddField("Gameplay",
                "- A scrambled word appears in your room's channel\n" +
                "- Type the unscrambled answer to advance\n" +
                "- Each level has 3 rooms — solve them in order\n" +
                "- After room 3, you move to room 1 of the next level", false)
            .AddField("Wrong Answers",
                "- 1 wrong guess: drop **1 level** (back to room 1) + cooldown\n" +
                "- 2 wrong in a row: drop **2 levels** (back to room 1) + cooldown\n" +
                "- You lose all room progress when you drop!", false)
            .AddField("Power Cards",
                "Every **3 rooms** cleared, you earn a random card:\n" +
                "- **Knockback** — Push a player down 1 level\n" +
                "- **Shield** — Block the next knockback\n" +
                "- **Spy** — Reveal a letter of your puzzle\n" +
                "- **Freeze** — Freeze a player for 60 seconds\n" +
                "- **Chaos** — Re-scramble another player's puzzle\n" +
                "- **Hint** — Get first/last letter clue", false)
            .AddField("Commands",
                "`/heist-start` — Create a lobby\n" +
                "`/heist-join` — Join the lobby or an active heist\n" +
                "`/heist-leave` — Leave the lobby\n" +
                "`/heist-status` — See who's on which level\n" +
                "`/heist-cards` — View your card inventory\n" +
                "`/use-card <card> [target]` — Play a power card\n" +
                "`/choose-assistant` — Pick an AI companion\n" +
                "`/ask-assistant <question>` — Ask your AI a question", false)
            .AddField("Tips",
                "- Don't guess randomly — two wrong in a row costs you 2 levels\n" +
                "- Save Knockback for when opponents are near the top\n" +
                "- Shield up when you're deep into levels 4-5\n" +
                "- Freeze can lock someone out for a full minute", false)
            .Build();
    }

    public static Embed RoadmapEmbed()
    {
        return new EmbedBuilder()
            .WithTitle("Arcade Roadmap")
            .WithDescription("Here's what's been built so far and what's coming next for the Arcade platform.")
            .WithColor(Color.Gold)
            .AddField("Completed",
                "- 5-level tower with 3 rooms per level\n" +
                "- Word scramble puzzles (5 difficulty tiers)\n" +
                "- Power cards: Knockback, Shield, Spy, Freeze, Chaos, Hint\n" +
                "- Wrong-answer penalties with cooldowns\n" +
                "- Dynamic Discord channel/role creation\n" +
                "- AI assistant companions (Ollama-powered)\n" +
                "- Arcade Host role management\n" +
                "- Race to the Top win condition", false)
            .AddField("In Progress",
                "- Leaderboards and stats tracking\n" +
                "- King of the Tower mode (hold crown room for 60s)\n" +
                "- Timed puzzle rounds", false)
            .AddField("Planned",
                "- Seasonal Ladder with XP, ranks, and cosmetic titles\n" +
                "- Riddle-based and multi-answer puzzles\n" +
                "- Hidden clues across previous channels\n" +
                "- Per-player inventory channels\n" +
                "- Additional Arcade games (Trivia, etc.)", false)
            .WithFooter("Have ideas? Let the Arcade Host know!")
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
