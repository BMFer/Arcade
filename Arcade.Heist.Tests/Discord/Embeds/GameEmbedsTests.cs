using Arcade.Core.Models;
using Arcade.Heist.Discord.Embeds;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;

namespace Arcade.Heist.Tests.Discord.Embeds;

[TestFixture]
public class GameEmbedsTests
{
    // --- PuzzleEmbed ---

    [Test]
    public void PuzzleEmbed_TitleContainsLevelAndRoom()
    {
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.PuzzleEmbed(puzzle, 3, 2);
        Assert.That(embed.Title, Does.Contain("Level 3"));
        Assert.That(embed.Title, Does.Contain("Room 2"));
    }

    [Test]
    public void PuzzleEmbed_DescriptionContainsUppercasedScramble()
    {
        var puzzle = MakePuzzle("gold", "dlgo");
        var embed = GameEmbeds.PuzzleEmbed(puzzle, 1, 1);
        Assert.That(embed.Description, Does.Contain("DLGO"));
    }

    [Test]
    public void PuzzleEmbed_Tier5_HasDoubleScrambleField()
    {
        var puzzle = new Puzzle { OriginalWord = "crown jewels", ScrambledWord = "nwocr slweje", DifficultyTier = 5 };
        var embed = GameEmbeds.PuzzleEmbed(puzzle, 5, 1);
        Assert.That(embed.Fields.Any(f => f.Name == "Mode" && f.Value.Contains("Double Scramble")), Is.True);
    }

    [Test]
    public void PuzzleEmbed_Tier1_NoDoubleScrambleField()
    {
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.PuzzleEmbed(puzzle, 1, 1);
        Assert.That(embed.Fields.Any(f => f.Name == "Mode"), Is.False);
    }

    // --- CorrectAnswerEmbed ---

    [Test]
    public void CorrectAnswerEmbed_TitleIsCorrect()
    {
        var player = MakePlayer();
        player.CurrentLevel = 1;
        player.CurrentRoom = 2;
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.CorrectAnswerEmbed(player, puzzle, 1, 1, null);
        Assert.That(embed.Title, Is.EqualTo("Correct!"));
    }

    [Test]
    public void CorrectAnswerEmbed_HasAdvancingField()
    {
        var player = MakePlayer();
        player.CurrentLevel = 1;
        player.CurrentRoom = 2;
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.CorrectAnswerEmbed(player, puzzle, 1, 1, null);
        Assert.That(embed.Fields.Any(f => f.Name == "Advancing"), Is.True);
    }

    [Test]
    public void CorrectAnswerEmbed_ShowsRoomProgression()
    {
        var player = MakePlayer();
        player.CurrentLevel = 1;
        player.CurrentRoom = 2;
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.CorrectAnswerEmbed(player, puzzle, 1, 1, null);
        var advField = embed.Fields.First(f => f.Name == "Advancing");
        Assert.That(advField.Value, Does.Contain("L1 Room 1"));
        Assert.That(advField.Value, Does.Contain("L1 Room 2"));
    }

    [Test]
    public void CorrectAnswerEmbed_WithCard_HasCardField()
    {
        var player = MakePlayer();
        player.CurrentLevel = 2;
        player.CurrentRoom = 1;
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.CorrectAnswerEmbed(player, puzzle, 1, 3, PowerCard.Shield);
        Assert.That(embed.Fields.Any(f => f.Name == "Card Earned!"), Is.True);
    }

    [Test]
    public void CorrectAnswerEmbed_NoCard_NoCardField()
    {
        var player = MakePlayer();
        player.CurrentLevel = 1;
        player.CurrentRoom = 2;
        var puzzle = MakePuzzle();
        var embed = GameEmbeds.CorrectAnswerEmbed(player, puzzle, 1, 1, null);
        Assert.That(embed.Fields.Any(f => f.Name == "Card Earned!"), Is.False);
    }

    // --- WrongAnswerEmbed ---

    [Test]
    public void WrongAnswerEmbed_TitleIsWrong()
    {
        var player = MakePlayer();
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(10);
        var embed = GameEmbeds.WrongAnswerEmbed(player, 2);
        Assert.That(embed.Title, Is.EqualTo("Wrong!"));
    }

    [Test]
    public void WrongAnswerEmbed_HasPenaltyField()
    {
        var player = MakePlayer();
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(10);
        var embed = GameEmbeds.WrongAnswerEmbed(player, 2);
        Assert.That(embed.Fields.Any(f => f.Name == "Penalty"), Is.True);
    }

    [Test]
    public void WrongAnswerEmbed_PenaltyMentionsRoom1()
    {
        var player = MakePlayer();
        player.CurrentRoom = 1;
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(10);
        var embed = GameEmbeds.WrongAnswerEmbed(player, 2);
        var penaltyField = embed.Fields.First(f => f.Name == "Penalty");
        Assert.That(penaltyField.Value, Does.Contain("room 1"));
    }

    [Test]
    public void WrongAnswerEmbed_HasCooldownField()
    {
        var player = MakePlayer();
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(10);
        var embed = GameEmbeds.WrongAnswerEmbed(player, 2);
        Assert.That(embed.Fields.Any(f => f.Name == "Cooldown"), Is.True);
    }

    // --- CooldownEmbed ---

    [Test]
    public void CooldownEmbed_ContainsPlayerName()
    {
        var player = MakePlayer();
        var embed = GameEmbeds.CooldownEmbed(player, 5);
        Assert.That(embed.Description, Does.Contain("TestPlayer"));
    }

    [Test]
    public void CooldownEmbed_ContainsSeconds()
    {
        var player = MakePlayer();
        var embed = GameEmbeds.CooldownEmbed(player, 5);
        Assert.That(embed.Description, Does.Contain("5s"));
    }

    // --- WinEmbed ---

    [Test]
    public void WinEmbed_TitleIsHeistComplete()
    {
        var player = MakePlayer();
        var embed = GameEmbeds.WinEmbed(player);
        Assert.That(embed.Title, Is.EqualTo("HEIST COMPLETE!"));
    }

    [Test]
    public void WinEmbed_HasStatsField()
    {
        var player = MakePlayer();
        player.WrongGuessCount = 3;
        player.RoomsCleared = 15;
        var embed = GameEmbeds.WinEmbed(player);
        Assert.That(embed.Fields.Any(f => f.Name == "Stats"), Is.True);
    }

    [Test]
    public void WinEmbed_StatsContainsRoomsCleared()
    {
        var player = MakePlayer();
        player.RoomsCleared = 15;
        var embed = GameEmbeds.WinEmbed(player);
        var statsField = embed.Fields.First(f => f.Name == "Stats");
        Assert.That(statsField.Value, Does.Contain("Rooms cleared: 15"));
    }

    // --- LobbyEmbed ---

    [Test]
    public void LobbyEmbed_TitleIsHeistLobby()
    {
        var embed = GameEmbeds.LobbyEmbed([]);
        Assert.That(embed.Title, Is.EqualTo("Heist Lobby"));
    }

    [Test]
    public void LobbyEmbed_NoPlayers_ShowsPlaceholder()
    {
        var embed = GameEmbeds.LobbyEmbed([]);
        Assert.That(embed.Fields.Any(f => f.Value.Contains("No players yet")), Is.True);
    }

    [Test]
    public void LobbyEmbed_WithPlayers_ListsThem()
    {
        var players = new[] { MakePlayer() };
        var embed = GameEmbeds.LobbyEmbed(players);
        Assert.That(embed.Fields.Any(f => f.Value.Contains("TestPlayer")), Is.True);
    }

    // --- PlayerJoinedEmbed ---

    [Test]
    public void PlayerJoinedEmbed_MentionsRoom1()
    {
        var embed = GameEmbeds.PlayerJoinedEmbed("Alice");
        Assert.That(embed.Description, Does.Contain("Room 1"));
    }

    // --- StatusEmbed ---

    [Test]
    public void StatusEmbed_TitleIsHeistStatus()
    {
        var game = new GameState();
        var embed = GameEmbeds.StatusEmbed(game);
        Assert.That(embed.Title, Is.EqualTo("Heist Status"));
    }

    [Test]
    public void StatusEmbed_Active_ShowsRoomInfo()
    {
        var game = new GameState
        {
            Status = GameStatus.Active,
            Players = new Dictionary<ulong, PlayerState>
            {
                [1] = new() { UserId = 1, DisplayName = "Alice", CurrentLevel = 2, CurrentRoom = 2 },
                [2] = new() { UserId = 2, DisplayName = "Bob", CurrentLevel = 2, CurrentRoom = 1 }
            },
            Levels =
            [
                new LevelInfo { LevelNumber = 1, Name = "Basement" },
                new LevelInfo { LevelNumber = 2, Name = "Ground Floor" }
            ]
        };
        var embed = GameEmbeds.StatusEmbed(game);
        Assert.That(embed.Fields.Any(f => f.Name.Contains("Ground Floor")), Is.True);
        var field = embed.Fields.First(f => f.Name.Contains("Ground Floor"));
        Assert.That(field.Value, Does.Contain("(R2)"));
        Assert.That(field.Value, Does.Contain("(R1)"));
    }

    // --- CardUsedEmbed ---

    [Test]
    public void CardUsedEmbed_TitleIsCardUsed()
    {
        var result = new CardResult { Success = true, Message = "Test card effect" };
        var embed = GameEmbeds.CardUsedEmbed(result);
        Assert.That(embed.Title, Is.EqualTo("Card Used!"));
    }

    // --- CardInventoryEmbed ---

    [Test]
    public void CardInventoryEmbed_NoCards_ShowsPlaceholder()
    {
        var player = MakePlayer();
        var embed = GameEmbeds.CardInventoryEmbed(player);
        Assert.That(embed.Description, Does.Contain("No cards"));
    }

    [Test]
    public void CardInventoryEmbed_WithCards_ListsThem()
    {
        var player = MakePlayer();
        player.Cards.Add(PowerCard.Shield);
        player.Cards.Add(PowerCard.Shield);
        var embed = GameEmbeds.CardInventoryEmbed(player);
        Assert.That(embed.Description, Does.Contain("Shield"));
        Assert.That(embed.Description, Does.Contain("x2"));
    }

    // --- HelpEmbed ---

    [Test]
    public void HelpEmbed_TitleIsHowToPlay()
    {
        var embed = GameEmbeds.HelpEmbed();
        Assert.That(embed.Title, Is.EqualTo("How to Play — Heist Tower"));
    }

    [Test]
    public void HelpEmbed_HasGameplayAndCardsFields()
    {
        var embed = GameEmbeds.HelpEmbed();
        Assert.That(embed.Fields.Any(f => f.Name == "Gameplay"), Is.True);
        Assert.That(embed.Fields.Any(f => f.Name == "Power Cards"), Is.True);
        Assert.That(embed.Fields.Any(f => f.Name == "Commands"), Is.True);
    }

    // --- AssistantSelectionEmbed ---

    [Test]
    public void AssistantSelectionEmbed_TitleIsChooseYourAssistant()
    {
        var profiles = new[]
        {
            new AssistantProfile { Id = "g", Name = "Glitch", Personality = "p", Description = "d" }
        };
        var embed = GameEmbeds.AssistantSelectionEmbed(profiles);
        Assert.That(embed.Title, Is.EqualTo("Choose Your Assistant"));
    }

    // --- AssistantResponseEmbed ---

    [Test]
    public void AssistantResponseEmbed_HasAuthorName()
    {
        var profile = new AssistantProfile { Id = "g", Name = "Glitch", Personality = "p", Description = "d" };
        var embed = GameEmbeds.AssistantResponseEmbed(profile, "Hello!");
        Assert.That(embed.Author.HasValue, Is.True);
        Assert.That(embed.Author!.Value.Name, Is.EqualTo("Glitch"));
    }

    // Helpers

    private static Puzzle MakePuzzle(string original = "gold", string scrambled = "dlgo", int tier = 1) =>
        new() { OriginalWord = original, ScrambledWord = scrambled, DifficultyTier = tier };

    private static PlayerState MakePlayer() => new()
    {
        UserId = 1,
        DisplayName = "TestPlayer",
        CurrentLevel = 1,
        CurrentRoom = 1
    };
}
