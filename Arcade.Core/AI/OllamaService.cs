using System.Text.Json;
using System.Text.Json.Serialization;
using Arcade.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Core.AI;

public class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<OllamaService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OllamaService(HttpClient httpClient, IOptions<AiOptions> options, ILogger<OllamaService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(_options.OllamaEndpoint);
        _httpClient.Timeout = TimeSpan.FromSeconds(_options.AssistantTimeoutSeconds);
    }

    public async Task<string?> GenerateAsync(string systemPrompt, string userPrompt, string? model = null)
    {
        try
        {
            var request = new OllamaRequest
            {
                Model = model ?? _options.OllamaDefaultModel,
                System = systemPrompt,
                Prompt = userPrompt,
                Stream = false,
                Options = new OllamaRequestOptions
                {
                    NumPredict = _options.AssistantMaxTokens
                }
            };

            var json = JsonSerializer.Serialize(request, JsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/generate", content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama returned {StatusCode}", response.StatusCode);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaResponse>(responseJson, JsonOptions);
            return result?.Response;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama request failed");
            return null;
        }
    }

    private class OllamaRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("system")]
        public string? System { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("stream")]
        public bool Stream { get; init; }

        [JsonPropertyName("options")]
        public OllamaRequestOptions? Options { get; init; }
    }

    private class OllamaRequestOptions
    {
        [JsonPropertyName("num_predict")]
        public int NumPredict { get; init; }
    }

    private class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; init; }
    }
}
