using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Arcade.Core.AI;
using Arcade.Core.Discord;
using Arcade.Heist.AI;
using Arcade.Heist.Configuration;
using Arcade.Heist.Discord.Embeds;
using Arcade.Heist.Game.Engine;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Discord.Commands;

public class HeistCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GameManager _gameManager;
    private readonly TowerManager _towerManager;
    private readonly AssistantService _assistantService;
    private readonly HeistOptions _options;

    public HeistCommands(GameManager gameManager, TowerManager towerManager, AssistantService assistantService, IOptions<HeistOptions> options)
    {
        _gameManager = gameManager;
        _towerManager = towerManager;
        _assistantService = assistantService;
        _options = options.Value;
    }

    [SlashCommand("heist-start", "Start a new heist! Creates a lobby for players to join.")]
    public async Task StartAsync()
    {
        var game = _gameManager.CurrentGame;
        if (game.Status != GameStatus.Idle)
        {
            await RespondAsync("A heist is already in progress!", ephemeral: true);
            return;
        }

        _gameManager.CreateLobby(Context.Channel.Id);
        _gameManager.JoinLobby(Context.User.Id, Context.User.GlobalName ?? Context.User.Username);

        await RespondAsync(embed: GameEmbeds.LobbyEmbed(_gameManager.CurrentGame.Players.Values));

        // Countdown then start
        _ = Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(_options.LobbyCountdownSeconds));

            var currentGame = _gameManager.CurrentGame;
            if (currentGame.Status != GameStatus.Lobby)
                return;

            if (currentGame.Players.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No players joined. Heist cancelled.");
                _gameManager.CreateLobby(0); // Reset
                return;
            }

            await Context.Channel.SendMessageAsync($"The heist begins with **{currentGame.Players.Count}** player(s)! Preparing the tower...\nNew players can still join with `/heist-join` while the heist is active.");

            var guild = Context.Guild;
            var started = await _gameManager.StartGameAsync(guild);

            if (!started)
            {
                await Context.Channel.SendMessageAsync("Tower is not initialized. An admin must run `/heist-init` first.");
                return;
            }

            await Context.Channel.SendMessageAsync("Tower is ready! Head to your assigned channel to start solving puzzles!");
        });
    }

    [SlashCommand("heist-join", "Join the current heist lobby or an active heist.")]
    public async Task JoinAsync()
    {
        var game = _gameManager.CurrentGame;
        var displayName = Context.User.GlobalName ?? Context.User.Username;

        switch (game.Status)
        {
            case GameStatus.Lobby:
                if (_gameManager.JoinLobby(Context.User.Id, displayName))
                    await RespondAsync(embed: GameEmbeds.LobbyEmbed(game.Players.Values));
                else
                    await RespondAsync("You're already in the lobby!", ephemeral: true);
                break;

            case GameStatus.Active:
                var (success, level) = await _gameManager.JoinGameAsync(Context.User.Id, displayName, Context.Guild);
                if (success)
                    await RespondAsync($"You've joined the heist! Head to the Level 1 channel to start solving puzzles.", ephemeral: true);
                else
                    await RespondAsync("You're already in the game!", ephemeral: true);
                break;

            default:
                await RespondAsync("No heist is running. Use `/heist-start` to create one.", ephemeral: true);
                break;
        }
    }

    [SlashCommand("heist-leave", "Leave the current heist.")]
    public async Task LeaveAsync()
    {
        var game = _gameManager.CurrentGame;
        if (game.Status == GameStatus.Lobby)
        {
            if (_gameManager.LeaveLobby(Context.User.Id))
                await RespondAsync($"**{Context.User.GlobalName ?? Context.User.Username}** left the lobby.");
            else
                await RespondAsync("You're not in the lobby.", ephemeral: true);
        }
        else if (game.Status == GameStatus.Active)
        {
            await RespondAsync("Can't leave during an active heist!", ephemeral: true);
        }
        else
        {
            await RespondAsync("No heist to leave.", ephemeral: true);
        }
    }

    [SlashCommand("heist-status", "Check the current heist status.")]
    public async Task StatusAsync()
    {
        var game = _gameManager.CurrentGame;
        if (game.Status == GameStatus.Idle)
        {
            await RespondAsync("No heist in progress. Use `/heist-start` to begin one.", ephemeral: true);
            return;
        }

        await RespondAsync(embed: GameEmbeds.StatusEmbed(game));
    }

    [SlashCommand("heist-end", "Force end the current heist (host/admin only).")]
    [RequireArcadeHost]
    public async Task EndAsync()
    {
        var game = _gameManager.CurrentGame;
        if (game.Status == GameStatus.Idle)
        {
            await RespondAsync("No heist to end.", ephemeral: true);
            return;
        }

        await RespondAsync("Ending the heist and stripping roles...");
        await _gameManager.EndGameAsync(Context.Guild);
        await FollowupAsync("Heist ended. Player roles stripped. Tower channels remain for the next game.");
    }

    [SlashCommand("heist-init", "Initialize the heist tower (host/admin only). Creates roles, categories, and channels.")]
    [RequireArcadeHost]
    public async Task InitAsync()
    {
        await DeferAsync();
        var (success, message) = await _towerManager.InitTowerAsync(Context.Guild as SocketGuild ?? throw new InvalidOperationException("Guild not available"));
        await FollowupAsync(message);
    }

    [SlashCommand("heist-teardown", "Tear down the heist tower (host/admin only). Deletes all tower roles, categories, and channels.")]
    [RequireArcadeHost]
    public async Task TeardownAsync()
    {
        var game = _gameManager.CurrentGame;
        if (game.Status == GameStatus.Active || game.Status == GameStatus.Lobby)
        {
            await RespondAsync("Can't tear down the tower while a heist is in progress. Use `/heist-end` first.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var (success, message) = await _towerManager.TeardownTowerAsync(Context.Guild as SocketGuild ?? throw new InvalidOperationException("Guild not available"));
        await FollowupAsync(message);
    }

    [SlashCommand("use-card", "Use a power card from your inventory.")]
    public async Task UseCardAsync(
        [Summary("card", "The card to use")] PowerCard card,
        [Summary("target", "Target player (required for Knockback, Freeze, Chaos)")] SocketGuildUser? target = null)
    {
        var game = _gameManager.CurrentGame;
        if (game.Status != GameStatus.Active)
        {
            await RespondAsync("No active heist.", ephemeral: true);
            return;
        }

        var result = await _gameManager.UseCardAsync(
            Context.User.Id,
            card,
            target?.Id,
            Context.Guild);

        await RespondAsync(result.Message, ephemeral: result.IsPrivate);
    }

    [SlashCommand("heist-cards", "View your power card inventory.")]
    public async Task CardsAsync()
    {
        var game = _gameManager.CurrentGame;
        if (!game.Players.TryGetValue(Context.User.Id, out var player))
        {
            await RespondAsync("You're not in a game.", ephemeral: true);
            return;
        }

        await RespondAsync(embed: GameEmbeds.CardInventoryEmbed(player), ephemeral: true);
    }

    [SlashCommand("ping", "Check if the bot is alive.")]
    public async Task PingAsync()
    {
        await RespondAsync($"Pong! Latency: {Context.Client.Latency}ms");
    }

    [SlashCommand("choose-assistant", "Choose an AI assistant to accompany you.")]
    public async Task ChooseAssistantAsync()
    {
        var profiles = _assistantService.Profiles;
        if (profiles.Count == 0)
        {
            await RespondAsync("No assistants are available.", ephemeral: true);
            return;
        }

        var menuBuilder = new SelectMenuBuilder()
            .WithCustomId("assistant-select")
            .WithPlaceholder("Pick your assistant...")
            .WithMinValues(1)
            .WithMaxValues(1);

        foreach (var profile in profiles)
            menuBuilder.AddOption(profile.Name, profile.Id, profile.Description);

        var component = new ComponentBuilder()
            .WithSelectMenu(menuBuilder)
            .Build();

        await RespondAsync(
            embed: GameEmbeds.AssistantSelectionEmbed(profiles),
            components: component,
            ephemeral: true);
    }

    [SlashCommand("ask-assistant", "Ask your AI assistant a question.")]
    public async Task AskAssistantAsync([Summary("question", "Your question")] string question)
    {
        var assistant = await _assistantService.GetPlayerAssistantAsync(Context.User.Id);
        if (assistant == null)
        {
            await RespondAsync("You haven't chosen an assistant yet. Use `/choose-assistant` first.", ephemeral: true);
            return;
        }

        await DeferAsync();

        // Get player state for game context if in a game
        string? gameContext = null;
        if (_gameManager.CurrentGame.Players.TryGetValue(Context.User.Id, out var playerState))
            gameContext = HeistGameContext.Format(playerState);

        var response = await _assistantService.AskAsync(Context.User.Id, question, gameContext);
        if (response == null)
        {
            await FollowupAsync("Your assistant is unavailable right now. Try again later.", ephemeral: true);
            return;
        }

        await FollowupAsync(embed: GameEmbeds.AssistantResponseEmbed(assistant, response));
    }
}
