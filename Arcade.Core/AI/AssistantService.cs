using System.Text.Json;
using Arcade.Core.Configuration;
using Arcade.Core.Data;
using Arcade.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Core.AI;

public class AssistantService
{
    private readonly OllamaService _ollama;
    private readonly PlayerDataStore _dataStore;
    private readonly IAssistantEventHandler _eventHandler;
    private readonly AiOptions _options;
    private readonly ILogger<AssistantService> _logger;
    private List<AssistantProfile> _profiles = [];

    public IReadOnlyList<AssistantProfile> Profiles => _profiles;

    public AssistantService(
        OllamaService ollama,
        PlayerDataStore dataStore,
        IAssistantEventHandler eventHandler,
        IOptions<AiOptions> options,
        ILogger<AssistantService> logger)
    {
        _ollama = ollama;
        _dataStore = dataStore;
        _eventHandler = eventHandler;
        _options = options.Value;
        _logger = logger;

        LoadProfiles();
    }

    private void LoadProfiles()
    {
        try
        {
            var path = _options.AssistantsConfigPath;
            if (!File.Exists(path))
            {
                _logger.LogWarning("Assistants config not found at {Path}", path);
                return;
            }

            var json = File.ReadAllText(path);
            _profiles = JsonSerializer.Deserialize<List<AssistantProfile>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];

            _logger.LogInformation("Loaded {Count} assistant profiles", _profiles.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load assistant profiles");
        }
    }

    public AssistantProfile? GetProfile(string id) =>
        _profiles.FirstOrDefault(p => p.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    public async Task SelectAssistantAsync(ulong userId, string assistantId)
    {
        var prefs = await _dataStore.GetAsync(userId) ?? new PlayerPreferences { UserId = userId };
        prefs.SelectedAssistantId = assistantId;
        await _dataStore.SaveAsync(prefs);
    }

    public async Task<AssistantProfile?> GetPlayerAssistantAsync(ulong userId)
    {
        var prefs = await _dataStore.GetAsync(userId);
        if (prefs?.SelectedAssistantId == null) return null;
        return GetProfile(prefs.SelectedAssistantId);
    }

    public async Task<string?> AskAsync(ulong userId, string question, string? gameContext = null)
    {
        if (!_options.AssistantEnabled) return null;

        var profile = await GetPlayerAssistantAsync(userId);
        if (profile == null) return null;

        var systemPrompt = BuildSystemPrompt(profile, gameContext);
        return await _ollama.GenerateAsync(systemPrompt, question, profile.OllamaModel);
    }

    public void CommentOnEvent(string eventDescription, ulong userId, ulong channelId, string? gameContext = null)
    {
        if (!_options.AssistantEnabled) return;

        _ = Task.Run(async () =>
        {
            try
            {
                var profile = await GetPlayerAssistantAsync(userId);
                if (profile == null) return;

                var systemPrompt = BuildSystemPrompt(profile, gameContext);
                var prompt = $"React to this game event in character (1-2 short sentences): {eventDescription}";

                var response = await _ollama.GenerateAsync(systemPrompt, prompt, profile.OllamaModel);
                if (string.IsNullOrWhiteSpace(response)) return;

                await _eventHandler.PostCommentaryAsync(profile, response, channelId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to post assistant commentary");
            }
        });
    }

    private static string BuildSystemPrompt(AssistantProfile profile, string? gameContext)
    {
        var prompt = profile.Personality;

        if (!string.IsNullOrEmpty(gameContext))
            prompt += "\n\n" + gameContext;

        return prompt;
    }
}
