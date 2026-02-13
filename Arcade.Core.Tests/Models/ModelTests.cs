using Arcade.Core.Models;

namespace Arcade.Core.Tests.Models;

[TestFixture]
public class ModelTests
{
    [Test]
    public void AssistantProfile_Roundtrip()
    {
        var profile = new AssistantProfile
        {
            Id = "glitch",
            Name = "Glitch",
            Personality = "Chaotic hacker",
            Description = "A mischievous AI",
            AvatarUrl = "https://example.com/avatar.png",
            OllamaModel = "llama3.2"
        };

        Assert.That(profile.Id, Is.EqualTo("glitch"));
        Assert.That(profile.Name, Is.EqualTo("Glitch"));
        Assert.That(profile.Personality, Is.EqualTo("Chaotic hacker"));
        Assert.That(profile.Description, Is.EqualTo("A mischievous AI"));
        Assert.That(profile.AvatarUrl, Is.EqualTo("https://example.com/avatar.png"));
        Assert.That(profile.OllamaModel, Is.EqualTo("llama3.2"));
    }

    [Test]
    public void AssistantProfile_OptionalFieldsNullByDefault()
    {
        var profile = new AssistantProfile
        {
            Id = "test",
            Name = "Test",
            Personality = "p",
            Description = "d"
        };

        Assert.That(profile.AvatarUrl, Is.Null);
        Assert.That(profile.OllamaModel, Is.Null);
    }

    [Test]
    public void PlayerPreferences_Roundtrip()
    {
        var prefs = new PlayerPreferences
        {
            UserId = 42
        };
        prefs.SelectedAssistantId = "glitch";

        Assert.That(prefs.UserId, Is.EqualTo((ulong)42));
        Assert.That(prefs.SelectedAssistantId, Is.EqualTo("glitch"));
    }
}
