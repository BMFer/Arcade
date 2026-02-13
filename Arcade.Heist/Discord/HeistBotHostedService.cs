using Arcade.Core.AI;
using Arcade.Core.Configuration;
using Arcade.Core.Models;
using Arcade.Heist.Discord.Embeds;
using Arcade.Heist.Discord.Handlers;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Discord;

public class HeistBotHostedService : Arcade.Core.Discord.BotHostedServiceBase
{
    private readonly MessageHandler _messageHandler;
    private readonly AssistantService _assistantService;

    public HeistBotHostedService(
        DiscordSocketClient client,
        InteractionService interactions,
        IServiceProvider services,
        ILogger<HeistBotHostedService> logger,
        IOptions<BotOptions> botOptions,
        MessageHandler messageHandler,
        AssistantService assistantService)
        : base(client, interactions, services, logger, botOptions)
    {
        _messageHandler = messageHandler;
        _assistantService = assistantService;
    }

    protected override void RegisterEventHandlers()
    {
        Client.SelectMenuExecuted += OnSelectMenuExecutedAsync;
        Client.MessageReceived += _messageHandler.HandleAsync;
    }

    private async Task OnSelectMenuExecutedAsync(SocketMessageComponent component)
    {
        if (component.Data.CustomId != "assistant-select")
            return;

        var selectedId = component.Data.Values.FirstOrDefault();
        if (selectedId == null) return;

        var profile = _assistantService.GetProfile(selectedId);
        if (profile == null)
        {
            await component.RespondAsync("Assistant not found.", ephemeral: true);
            return;
        }

        await _assistantService.SelectAssistantAsync(component.User.Id, selectedId);
        await component.RespondAsync(
            embed: GameEmbeds.AssistantResponseEmbed(profile, $"**{profile.Name}** is now your assistant!"),
            ephemeral: true);
    }
}
