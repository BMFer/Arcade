namespace Arcade.Core.Models;

public class PlayerPreferences
{
    public ulong UserId { get; init; }
    public string? SelectedAssistantId { get; set; }
}
