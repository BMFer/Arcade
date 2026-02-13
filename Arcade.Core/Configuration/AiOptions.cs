namespace Arcade.Core.Configuration;

public class AiOptions
{
    public const string SectionName = "AI";

    public bool AssistantEnabled { get; set; } = true;
    public string OllamaEndpoint { get; set; } = "http://localhost:11434";
    public string OllamaDefaultModel { get; set; } = "llama3.2";
    public string AssistantsConfigPath { get; set; } = "assistants.json";
    public string PlayerDataPath { get; set; } = "data/players.json";
    public int AssistantMaxTokens { get; set; } = 150;
    public int AssistantTimeoutSeconds { get; set; } = 30;
}
