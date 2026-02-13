using Arcade.Heist.Game.Models;

namespace Arcade.Heist.AI;

public static class HeistGameContext
{
    public static string Format(PlayerState player)
    {
        var context = $"Game context: The player \"{player.DisplayName}\" is on level {player.CurrentLevel}, room {player.CurrentRoom}. " +
                      $"They have {player.Cards.Count} power card(s). " +
                      $"Wrong guesses so far: {player.WrongGuessCount}. " +
                      $"Rooms cleared: {player.RoomsCleared}.";

        if (player.IsOnCooldown)
            context += $" They are currently on cooldown ({(int)player.CooldownRemaining.TotalSeconds}s remaining).";

        if (player.ShieldActive)
            context += " They have an active shield.";

        return context;
    }
}
