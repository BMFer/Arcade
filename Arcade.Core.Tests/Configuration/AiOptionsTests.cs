using Arcade.Core.Configuration;

namespace Arcade.Core.Tests.Configuration;

[TestFixture]
public class AiOptionsTests
{
    private AiOptions _options = null!;

    [SetUp]
    public void Setup()
    {
        _options = new AiOptions();
    }

    [Test]
    public void AssistantEnabled_DefaultTrue()
    {
        Assert.That(_options.AssistantEnabled, Is.True);
    }

    [Test]
    public void OllamaEndpoint_DefaultLocalhost()
    {
        Assert.That(_options.OllamaEndpoint, Is.EqualTo("http://localhost:11434"));
    }

    [Test]
    public void OllamaDefaultModel_DefaultLlama()
    {
        Assert.That(_options.OllamaDefaultModel, Is.EqualTo("llama3.2"));
    }

    [Test]
    public void AssistantsConfigPath_DefaultAssistantsJson()
    {
        Assert.That(_options.AssistantsConfigPath, Is.EqualTo("assistants.json"));
    }

    [Test]
    public void PlayerDataPath_DefaultDataPlayersJson()
    {
        Assert.That(_options.PlayerDataPath, Is.EqualTo("data/players.json"));
    }

    [Test]
    public void AssistantMaxTokens_Default150()
    {
        Assert.That(_options.AssistantMaxTokens, Is.EqualTo(150));
    }

    [Test]
    public void AssistantTimeoutSeconds_Default30()
    {
        Assert.That(_options.AssistantTimeoutSeconds, Is.EqualTo(30));
    }
}
