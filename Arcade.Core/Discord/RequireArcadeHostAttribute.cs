using Discord;
using Discord.Interactions;
using Arcade.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Arcade.Core.Discord;

public class RequireArcadeHostAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        if (context.User is not IGuildUser guildUser)
            return Task.FromResult(PreconditionResult.FromError("This command must be used in a server."));

        if (guildUser.GuildPermissions.Administrator)
            return Task.FromResult(PreconditionResult.FromSuccess());

        var botOptions = (IOptions<BotOptions>)services.GetService(typeof(IOptions<BotOptions>))!;
        var roleName = botOptions.Value.HostRoleName;

        var hasHostRole = guildUser.Guild.Roles
            .Any(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase)
                       && guildUser.RoleIds.Contains(r.Id));

        return hasHostRole
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError(
                $"You need the **{roleName}** role or **Administrator** permission to use this command."));
    }
}
