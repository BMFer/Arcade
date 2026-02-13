using Arcade.Core.Models;

namespace Arcade.Core.AI;

public interface IAssistantEventHandler
{
    Task PostCommentaryAsync(AssistantProfile profile, string response, ulong channelId);
}
