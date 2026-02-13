using Discord.WebSocket;
using Arcade.Heist.Game.Engine;
using Microsoft.Extensions.Logging;

namespace Arcade.Heist.Discord.Handlers;

public class MessageHandler
{
    private readonly GameManager _gameManager;
    private readonly ILogger<MessageHandler> _logger;

    public MessageHandler(GameManager gameManager, ILogger<MessageHandler> logger)
    {
        _gameManager = gameManager;
        _logger = logger;
    }

    public async Task HandleAsync(SocketMessage message)
    {
        // Ignore bot messages
        if (message.Author.IsBot)
            return;

        // Only process messages in game channels
        if (message is not SocketUserMessage userMessage)
            return;

        if (message.Channel is not SocketTextChannel textChannel)
            return;

        if (!_gameManager.IsGameChannel(textChannel.Id))
            return;

        var guild = textChannel.Guild;
        var content = message.Content.Trim();

        if (string.IsNullOrWhiteSpace(content))
            return;

        _logger.LogDebug("Answer attempt from {User} in channel {Channel}: {Content}",
            message.Author.Username, textChannel.Name, content);

        await _gameManager.HandleAnswerAsync(message.Author.Id, textChannel.Id, content, guild);
    }
}
