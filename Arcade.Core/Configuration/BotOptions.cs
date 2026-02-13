namespace Arcade.Core.Configuration;

public class BotOptions
{
    public const string SectionName = "Bot";

    public string BotToken { get; set; } = string.Empty;
    public ulong GuildId { get; set; }
    public string HostRoleName { get; set; } = "Arcade Host";
}
