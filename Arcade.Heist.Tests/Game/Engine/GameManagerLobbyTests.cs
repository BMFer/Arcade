using Arcade.Core.AI;
using Arcade.Core.Configuration;
using Arcade.Core.Data;
using Arcade.Heist.Configuration;
using Arcade.Heist.Data;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;
using Arcade.Heist.Game.Words;
using Discord.WebSocket;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Arcade.Heist.Tests.Game.Engine;

[TestFixture]
public class GameManagerLobbyTests
{
    private GameManager _gameManager = null!;

    [SetUp]
    public void Setup()
    {
        var heistOptions = Options.Create(new HeistOptions());
        var aiOptions = Options.Create(new AiOptions { AssistantEnabled = false });

        var client = new DiscordSocketClient();
        var towerDataStore = new TowerDataStore(heistOptions, NullLogger<TowerDataStore>.Instance);
        var towerManager = new TowerManager(client, towerDataStore, heistOptions, NullLogger<TowerManager>.Instance);
        var playerManager = new PlayerManager(heistOptions, NullLogger<PlayerManager>.Instance);
        var puzzleManager = new PuzzleManager(new WordBank(), new WordScrambler());
        var cardManager = new CardManager(heistOptions);

        var playerDataStore = new PlayerDataStore(aiOptions, NullLogger<PlayerDataStore>.Instance);
        var eventHandler = Substitute.For<IAssistantEventHandler>();
        var ollamaService = new OllamaService(new HttpClient(), aiOptions, NullLogger<OllamaService>.Instance);
        var assistantService = new AssistantService(
            ollamaService, playerDataStore, eventHandler, aiOptions, NullLogger<AssistantService>.Instance);

        _gameManager = new GameManager(
            client, towerManager, playerManager, puzzleManager, cardManager,
            assistantService, heistOptions, NullLogger<GameManager>.Instance);
    }

    // --- CreateLobby ---

    [Test]
    public void CreateLobby_SetsStatusToLobby()
    {
        _gameManager.CreateLobby(100);
        Assert.That(_gameManager.CurrentGame.Status, Is.EqualTo(GameStatus.Lobby));
    }

    [Test]
    public void CreateLobby_SetsChannelId()
    {
        _gameManager.CreateLobby(100);
        Assert.That(_gameManager.CurrentGame.LobbyChannelId, Is.EqualTo(100));
    }

    // --- JoinLobby ---

    [Test]
    public void JoinLobby_InLobby_Succeeds()
    {
        _gameManager.CreateLobby(100);
        var result = _gameManager.JoinLobby(1, "Alice");
        Assert.That(result, Is.True);
        Assert.That(_gameManager.CurrentGame.Players, Contains.Key((ulong)1));
    }

    [Test]
    public void JoinLobby_InIdle_Fails()
    {
        var result = _gameManager.JoinLobby(1, "Alice");
        Assert.That(result, Is.False);
    }

    [Test]
    public void JoinLobby_Duplicate_Fails()
    {
        _gameManager.CreateLobby(100);
        _gameManager.JoinLobby(1, "Alice");
        var result = _gameManager.JoinLobby(1, "Alice");
        Assert.That(result, Is.False);
    }

    // --- LeaveLobby ---

    [Test]
    public void LeaveLobby_InLobby_Succeeds()
    {
        _gameManager.CreateLobby(100);
        _gameManager.JoinLobby(1, "Alice");
        var result = _gameManager.LeaveLobby(1);
        Assert.That(result, Is.True);
        Assert.That(_gameManager.CurrentGame.Players, Does.Not.ContainKey((ulong)1));
    }

    [Test]
    public void LeaveLobby_InIdle_Fails()
    {
        var result = _gameManager.LeaveLobby(1);
        Assert.That(result, Is.False);
    }

    // --- IsGameChannel ---

    [Test]
    public void IsGameChannel_WhenNotActive_ReturnsFalse()
    {
        _gameManager.CreateLobby(100);
        Assert.That(_gameManager.IsGameChannel(100), Is.False);
    }
}
