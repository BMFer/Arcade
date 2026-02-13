using Arcade.Core.Configuration;

namespace Arcade.Core.Tests.Configuration;

[TestFixture]
public class BotOptionsTests
{
    [Test]
    public void BotToken_DefaultIsEmpty()
    {
        var options = new BotOptions();
        Assert.That(options.BotToken, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GuildId_DefaultIsZero()
    {
        var options = new BotOptions();
        Assert.That(options.GuildId, Is.EqualTo((ulong)0));
    }
}
