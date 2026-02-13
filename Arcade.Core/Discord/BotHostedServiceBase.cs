using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Arcade.Core.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Arcade.Core.Discord;

public abstract class BotHostedServiceBase : IHostedService
{
    protected readonly DiscordSocketClient Client;
    protected readonly InteractionService Interactions;
    protected readonly IServiceProvider Services;
    protected readonly ILogger Logger;
    protected readonly BotOptions BotOptions;

    protected BotHostedServiceBase(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger logger,
        IOptions<BotOptions> botOptions)
    {
        Client = client;
        Interactions = interactions;
        Services = services;
        Logger = logger;
        BotOptions = botOptions.Value;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Client.Log += LogAsync;
        Interactions.Log += LogAsync;

        Client.Ready += OnReadyAsync;
        Client.InteractionCreated += OnInteractionCreatedAsync;

        RegisterEventHandlers();

        var token = BotOptions.BotToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            Logger.LogError("Bot token not configured. Set the bot token env var or Bot:BotToken in appsettings.json.");
            return;
        }

        await Client.LoginAsync(TokenType.Bot, token);
        await Client.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Client.StopAsync();
    }

    protected abstract void RegisterEventHandlers();

    private async Task OnReadyAsync()
    {
        Logger.LogInformation("Bot connected as {User}", Client.CurrentUser);

        await Interactions.AddModulesAsync(Assembly.GetEntryAssembly(), Services);

        if (BotOptions.GuildId != 0)
        {
            await Interactions.RegisterCommandsToGuildAsync(BotOptions.GuildId);
            Logger.LogInformation("Slash commands registered to guild {GuildId}", BotOptions.GuildId);
        }
        else
        {
            await Interactions.RegisterCommandsGloballyAsync();
            Logger.LogInformation("Slash commands registered globally");
        }
    }

    private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
    {
        var ctx = new SocketInteractionContext(Client, interaction);
        await Interactions.ExecuteCommandAsync(ctx, Services);
    }

    private Task LogAsync(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => LogLevel.Critical,
            LogSeverity.Error => LogLevel.Error,
            LogSeverity.Warning => LogLevel.Warning,
            LogSeverity.Info => LogLevel.Information,
            LogSeverity.Verbose => LogLevel.Debug,
            LogSeverity.Debug => LogLevel.Trace,
            _ => LogLevel.Information
        };

        Logger.Log(level, msg.Exception, "[{Source}] {Message}", msg.Source, msg.Message);
        return Task.CompletedTask;
    }
}
