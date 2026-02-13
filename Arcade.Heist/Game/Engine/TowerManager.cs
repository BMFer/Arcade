using Discord;
using Discord.WebSocket;
using Arcade.Heist.Configuration;
using Arcade.Heist.Data;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Game.Engine;

public class TowerManager
{
    private readonly DiscordSocketClient _client;
    private readonly TowerDataStore _dataStore;
    private readonly HeistOptions _options;
    private readonly ILogger<TowerManager> _logger;

    public TowerManager(
        DiscordSocketClient client,
        TowerDataStore dataStore,
        IOptions<HeistOptions> options,
        ILogger<TowerManager> logger)
    {
        _client = client;
        _dataStore = dataStore;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> InitTowerAsync(SocketGuild guild)
    {
        var existing = await _dataStore.GetAsync();
        if (existing != null && existing.Count > 0)
            return (false, "Tower is already initialized. Use `/heist-teardown` first to rebuild.");

        var levels = new List<LevelInfo>();
        var botUser = guild.CurrentUser;

        for (int i = 0; i < _options.MaxLevels; i++)
        {
            var levelNum = i + 1;
            var name = i < LevelInfo.LevelNames.Length
                ? LevelInfo.LevelNames[i]
                : $"Level {levelNum}";

            // Create the role for this level
            var role = await guild.CreateRoleAsync(
                $"Heist-L{levelNum}",
                permissions: GuildPermissions.None,
                color: null,
                isHoisted: false,
                isMentionable: false);

            // Create a category for this level
            var category = await guild.CreateCategoryChannelAsync($"🏗️ Heist - L{levelNum}: {name}");

            // Set category permissions: deny @everyone, allow the level role, allow bot
            await category.AddPermissionOverwriteAsync(
                guild.EveryoneRole,
                new OverwritePermissions(viewChannel: PermValue.Deny));

            await category.AddPermissionOverwriteAsync(
                role,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow));

            await category.AddPermissionOverwriteAsync(
                botUser,
                new OverwritePermissions(
                    viewChannel: PermValue.Allow,
                    sendMessages: PermValue.Allow,
                    manageMessages: PermValue.Allow));

            // Create the puzzle room channel in this category
            var channel = await guild.CreateTextChannelAsync(
                $"room-{levelNum}-{name.ToLowerInvariant().Replace(' ', '-')}",
                props => { props.CategoryId = category.Id; });

            var level = new LevelInfo
            {
                LevelNumber = levelNum,
                Name = name,
                CategoryId = category.Id,
                ChannelId = channel.Id,
                RoleId = role.Id,
                DifficultyTier = Math.Clamp(levelNum, 1, 5)
            };

            levels.Add(level);
            _logger.LogInformation(
                "Created tower level {Level}: {Name} (Role={RoleId}, Category={CategoryId}, Channel={ChannelId})",
                levelNum, name, role.Id, category.Id, channel.Id);
        }

        await _dataStore.SaveAsync(levels);
        return (true, $"Tower initialized with {levels.Count} levels. Roles, categories, and channels are ready.");
    }

    public async Task<(bool Success, string Message)> TeardownTowerAsync(SocketGuild guild)
    {
        var levels = await _dataStore.GetAsync();
        if (levels == null || levels.Count == 0)
            return (false, "No tower to tear down. Use `/heist-init` to create one.");

        foreach (var level in levels)
        {
            try
            {
                var channel = guild.GetChannel(level.ChannelId);
                if (channel != null)
                    await channel.DeleteAsync();

                var category = guild.GetChannel(level.CategoryId);
                if (category != null)
                    await category.DeleteAsync();

                var role = guild.GetRole(level.RoleId);
                if (role != null)
                    await role.DeleteAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up level {Level}", level.LevelNumber);
            }
        }

        await _dataStore.ClearAsync();
        return (true, "Tower torn down. All roles, categories, and channels have been deleted.");
    }

    public async Task<List<LevelInfo>?> GetTowerLevelsAsync()
    {
        return await _dataStore.GetAsync();
    }

    public async Task PlacePlayerAtLevelAsync(SocketGuild guild, ulong userId, LevelInfo level)
    {
        var user = guild.GetUser(userId);
        if (user == null) return;

        var role = guild.GetRole(level.RoleId);
        if (role == null) return;

        await user.AddRoleAsync(role);
    }

    public async Task MovePlayerAsync(SocketGuild guild, ulong userId, LevelInfo fromLevel, LevelInfo toLevel)
    {
        var user = guild.GetUser(userId);
        if (user == null) return;

        var oldRole = guild.GetRole(fromLevel.RoleId);
        var newRole = guild.GetRole(toLevel.RoleId);

        if (oldRole != null)
            await user.RemoveRoleAsync(oldRole);
        if (newRole != null)
            await user.AddRoleAsync(newRole);
    }

    public async Task StripAllPlayerRolesAsync(SocketGuild guild, IEnumerable<ulong> userIds)
    {
        var levels = await _dataStore.GetAsync();
        if (levels == null) return;

        var roleIds = levels.Select(l => l.RoleId).ToHashSet();
        var roles = roleIds
            .Select(id => guild.GetRole(id))
            .Where(r => r != null)
            .ToList();

        foreach (var userId in userIds)
        {
            var user = guild.GetUser(userId);
            if (user == null) continue;

            foreach (var role in roles)
            {
                if (user.Roles.Any(r => r.Id == role!.Id))
                {
                    try
                    {
                        await user.RemoveRoleAsync(role!);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to remove role {Role} from user {User}", role!.Name, userId);
                    }
                }
            }
        }
    }
}
