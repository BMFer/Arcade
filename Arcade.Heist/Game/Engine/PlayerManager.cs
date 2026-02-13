using Arcade.Heist.Configuration;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Game.Engine;

public class PlayerManager
{
    private readonly HeistOptions _options;
    private readonly ILogger<PlayerManager> _logger;

    public PlayerManager(IOptions<HeistOptions> options, ILogger<PlayerManager> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public PlayerState CreatePlayer(ulong userId, string displayName)
    {
        return new PlayerState
        {
            UserId = userId,
            DisplayName = displayName,
            CurrentLevel = 1,
            CurrentRoom = 1
        };
    }

    public void AdvancePlayer(PlayerState player)
    {
        player.CurrentRoom++;
        if (player.CurrentRoom > _options.RoomsPerLevel)
        {
            player.CurrentRoom = 1;
            player.CurrentLevel++;
        }

        player.ConsecutiveWrongCount = 0;
        player.RoomsCleared++;

        _logger.LogInformation("Player {Player} advanced to level {Level} room {Room}",
            player.DisplayName, player.CurrentLevel, player.CurrentRoom);
    }

    public void PenalizePlayer(PlayerState player)
    {
        player.WrongGuessCount++;
        player.ConsecutiveWrongCount++;

        int dropLevels = player.ConsecutiveWrongCount >= 2 ? 2 : 1;
        player.CurrentLevel = Math.Max(1, player.CurrentLevel - dropLevels);
        player.CurrentRoom = 1;
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(_options.WrongGuessCooldownSeconds);

        if (player.ConsecutiveWrongCount >= 2)
            player.ConsecutiveWrongCount = 0; // Reset after double penalty

        _logger.LogInformation("Player {Player} penalized, dropped {Drop} level(s) to level {Level} room 1",
            player.DisplayName, dropLevels, player.CurrentLevel);
    }

    public bool ShouldAwardCard(PlayerState player)
    {
        return player.RoomsCleared > 0 && player.RoomsCleared % _options.CardAwardInterval == 0;
    }

    public void ApplyCooldown(PlayerState player, int seconds)
    {
        player.CooldownExpiry = DateTimeOffset.UtcNow.AddSeconds(seconds);
    }

    public bool HasWon(PlayerState player, int maxLevels)
    {
        return player.CurrentLevel > maxLevels;
    }
}
