using Arcade.Core.AI;
using Arcade.Core.Models;
using Arcade.Heist.Discord.Embeds;
using Discord.WebSocket;

namespace Arcade.Heist.AI;

public class HeistAssistantEventHandler : IAssistantEventHandler
{
    private readonly DiscordSocketClient _client;

    public HeistAssistantEventHandler(DiscordSocketClient client)
    {
        _client = client;
    }

    public async Task PostCommentaryAsync(AssistantProfile profile, string response, ulong channelId)
    {
        if (_client.GetChannel(channelId) is SocketTextChannel channel)
        {
            await channel.SendMessageAsync(embed: GameEmbeds.AssistantResponseEmbed(profile, response));
        }
    }
}
