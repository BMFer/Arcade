namespace Arcade.Heist.Game.Models;

public class PlayerState
{
    public ulong UserId { get; init; }
    public string DisplayName { get; set; } = string.Empty;
    public int CurrentLevel { get; set; } = 1;
    public int CurrentRoom { get; set; } = 1;
    public int WrongGuessCount { get; set; }
    public int ConsecutiveWrongCount { get; set; }
    public DateTimeOffset CooldownExpiry { get; set; } = DateTimeOffset.MinValue;
    public List<PowerCard> Cards { get; set; } = [];
    public bool ShieldActive { get; set; }
    public int RoomsCleared { get; set; }
    public string? AssistantId { get; set; }

    public bool IsOnCooldown => DateTimeOffset.UtcNow < CooldownExpiry;

    public TimeSpan CooldownRemaining =>
        IsOnCooldown ? CooldownExpiry - DateTimeOffset.UtcNow : TimeSpan.Zero;
}
