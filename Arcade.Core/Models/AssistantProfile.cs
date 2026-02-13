namespace Arcade.Core.Models;

public class AssistantProfile
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Personality { get; init; }
    public string? AvatarUrl { get; init; }
    public required string Description { get; init; }
    public string? OllamaModel { get; init; }
}
