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
            var attachment = GameEmbeds.GetBannerAttachment(profile);
            if (attachment is { } file)
                await channel.SendFileAsync(file, embed: GameEmbeds.AssistantResponseEmbed(profile, response, file.FileName));
            else
                await channel.SendMessageAsync(embed: GameEmbeds.AssistantResponseEmbed(profile, response));
        }
    }
}
