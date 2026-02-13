using Discord.WebSocket;
using Arcade.Core.AI;
using Arcade.Heist.AI;
using Arcade.Heist.Configuration;
using Arcade.Heist.Discord.Embeds;
using Arcade.Heist.Game.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arcade.Heist.Game.Engine;

public class GameManager
{
    private readonly DiscordSocketClient _client;
    private readonly TowerManager _towerManager;
    private readonly PlayerManager _playerManager;
    private readonly PuzzleManager _puzzleManager;
    private readonly CardManager _cardManager;
    private readonly AssistantService _assistantService;
    private readonly HeistOptions _options;
    private readonly ILogger<GameManager> _logger;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private GameState _game = new();

    public GameState CurrentGame => _game;

    public GameManager(
        DiscordSocketClient client,
        TowerManager towerManager,
        PlayerManager playerManager,
        PuzzleManager puzzleManager,
        CardManager cardManager,
        AssistantService assistantService,
        IOptions<HeistOptions> options,
        ILogger<GameManager> logger)
    {
        _client = client;
        _towerManager = towerManager;
        _playerManager = playerManager;
        _puzzleManager = puzzleManager;
        _cardManager = cardManager;
        _assistantService = assistantService;
        _options = options.Value;
        _logger = logger;
    }

    public bool JoinLobby(ulong userId, string displayName)
    {
        if (_game.Status != GameStatus.Lobby)
            return false;
        if (_game.Players.ContainsKey(userId))
            return false;

        var player = _playerManager.CreatePlayer(userId, displayName);
        _game.Players[userId] = player;
        _logger.LogInformation("Player {Name} joined the lobby", displayName);
        return true;
    }

    public async Task<(bool Success, LevelInfo? Level)> JoinGameAsync(ulong userId, string displayName, SocketGuild guild)
    {
        await _lock.WaitAsync();
        try
        {
            if (_game.Status != GameStatus.Active)
                return (false, null);
            if (_game.Players.ContainsKey(userId))
                return (false, null);

            var player = _playerManager.CreatePlayer(userId, displayName);
            player.CurrentLevel = 1;
            player.CurrentRoom = 1;
            _game.Players[userId] = player;

            var level1 = _game.Levels.FirstOrDefault(l => l.LevelNumber == 1);
            if (level1 == null)
                return (false, null);

            var room1 = GetRoomInfo(1, 1);
            if (room1 == null)
                return (false, null);

            await _towerManager.PlacePlayerAtRoomAsync(guild, userId, room1);

            var channel = guild.GetTextChannel(room1.ChannelId);
            if (channel != null)
            {
                await channel.SendMessageAsync(embed: GameEmbeds.PlayerJoinedEmbed(displayName));

                if (_game.ActivePuzzles.TryGetValue((1, 1), out var puzzle))
                    await channel.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(puzzle, 1, 1));
            }

            _assistantService.CommentOnEvent(
                $"{displayName} has joined the heist mid-game and is starting at Level 1 Room 1!",
                userId, room1.ChannelId, HeistGameContext.Format(player));

            _logger.LogInformation("Player {Name} joined mid-game at Level 1 Room 1", displayName);
            return (true, level1);
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool LeaveLobby(ulong userId)
    {
        if (_game.Status != GameStatus.Lobby)
            return false;
        return _game.Players.Remove(userId);
    }

    public void CreateLobby(ulong lobbyChannelId)
    {
        _game = new GameState
        {
            Status = GameStatus.Lobby,
            LobbyChannelId = lobbyChannelId
        };
    }

    public async Task<bool> StartGameAsync(SocketGuild guild)
    {
        await _lock.WaitAsync();
        try
        {
            if (_game.Status != GameStatus.Lobby || _game.Players.Count == 0)
                return false;

            // Load persisted tower — must be initialized via /heist-init
            var levels = await _towerManager.GetTowerLevelsAsync();
            if (levels == null || levels.Count == 0)
                return false;

            _game.Status = GameStatus.Active;
            _game.Levels = levels;

            // Place all players at level 1, room 1
            var room1 = GetRoomInfo(1, 1);
            if (room1 == null)
                return false;

            foreach (var player in _game.Players.Values)
            {
                player.CurrentLevel = 1;
                player.CurrentRoom = 1;
                await _towerManager.PlacePlayerAtRoomAsync(guild, player.UserId, room1);
            }

            // Generate puzzles for all rooms on all levels
            foreach (var level in _game.Levels)
            {
                foreach (var room in level.Rooms)
                {
                    var puzzle = _puzzleManager.GeneratePuzzle(level.LevelNumber);
                    _game.ActivePuzzles[(level.LevelNumber, room.RoomNumber)] = puzzle;
                }
            }

            // Post the first puzzle in level 1 room 1's channel
            var channel = guild.GetTextChannel(room1.ChannelId);
            if (channel != null)
            {
                var puzzle = _game.ActivePuzzles[(1, 1)];
                await channel.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(puzzle, 1, 1));
            }

            // Fire proactive commentary for game start
            foreach (var p in _game.Players.Values)
                _assistantService.CommentOnEvent("The heist has begun! Players are entering the tower at Level 1 Room 1.", p.UserId, room1.ChannelId, HeistGameContext.Format(p));

            _logger.LogInformation("Game started with {Count} players", _game.Players.Count);
            return true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task HandleAnswerAsync(ulong userId, ulong channelId, string message, SocketGuild guild)
    {
        await _lock.WaitAsync();
        try
        {
            if (_game.Status != GameStatus.Active)
                return;

            if (!_game.Players.TryGetValue(userId, out var player))
                return;

            // Find the level and room for this channel
            var (level, room) = FindLevelAndRoom(channelId);
            if (level == null || room == null)
                return;

            // Player must be on this level and room
            if (player.CurrentLevel != level.LevelNumber || player.CurrentRoom != room.RoomNumber)
                return;

            // Check cooldown
            if (player.IsOnCooldown)
            {
                var channel = guild.GetTextChannel(channelId);
                if (channel != null)
                {
                    var remaining = (int)Math.Ceiling(player.CooldownRemaining.TotalSeconds);
                    await channel.SendMessageAsync(embed: GameEmbeds.CooldownEmbed(player, remaining));
                }
                return;
            }

            // Check answer
            var puzzleKey = (level.LevelNumber, room.RoomNumber);
            if (!_game.ActivePuzzles.TryGetValue(puzzleKey, out var puzzle))
                return;

            var textChannel = guild.GetTextChannel(channelId);

            if (_puzzleManager.CheckAnswer(puzzle, message))
            {
                // Correct answer!
                puzzle.Solved = true;
                puzzle.SolvedByUserId = userId;

                var oldLevel = player.CurrentLevel;
                var oldRoom = player.CurrentRoom;
                _playerManager.AdvancePlayer(player);

                // Check for card award
                PowerCard? awardedCard = null;
                if (_playerManager.ShouldAwardCard(player))
                {
                    awardedCard = _cardManager.AwardRandomCard();
                    player.Cards.Add(awardedCard.Value);
                }

                // Announce in current room
                if (textChannel != null)
                    await textChannel.SendMessageAsync(embed: GameEmbeds.CorrectAnswerEmbed(player, puzzle, oldLevel, oldRoom, awardedCard));

                // Fire proactive commentary
                var correctEvent = $"{player.DisplayName} solved the puzzle '{puzzle.OriginalWord}' and advanced from level {oldLevel} room {oldRoom} to level {player.CurrentLevel} room {player.CurrentRoom}!";
                if (awardedCard.HasValue)
                    correctEvent += $" They also earned a {awardedCard.Value} card!";
                _assistantService.CommentOnEvent(correctEvent, player.UserId, channelId, HeistGameContext.Format(player));

                // Generate new puzzle for the solved room
                var newPuzzle = _puzzleManager.GeneratePuzzle(level.LevelNumber);
                _game.ActivePuzzles[puzzleKey] = newPuzzle;

                // Post new puzzle in the solved room
                if (textChannel != null)
                    await textChannel.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(newPuzzle, level.LevelNumber, room.RoomNumber));

                // Check if player won
                if (_playerManager.HasWon(player, _options.MaxLevels))
                {
                    _game.WinnerId = userId;
                    _game.Status = GameStatus.Finished;

                    // Announce winner in the lobby channel
                    var lobbyChannel = guild.GetTextChannel(_game.LobbyChannelId);
                    if (lobbyChannel != null)
                        await lobbyChannel.SendMessageAsync(embed: GameEmbeds.WinEmbed(player));

                    // Also announce in the current channel
                    if (textChannel != null)
                        await textChannel.SendMessageAsync(embed: GameEmbeds.WinEmbed(player));

                    _assistantService.CommentOnEvent($"{player.DisplayName} has cleared the Crown Room and won the heist!", player.UserId, _game.LobbyChannelId, HeistGameContext.Format(player));

                    // Strip roles after a delay, tower structure stays
                    var winPlayerIds = _game.Players.Keys.ToList();
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        await _towerManager.StripAllPlayerRolesAsync(guild, winPlayerIds);
                        _game = new GameState();
                    });

                    return;
                }

                // Move player to next room
                var newRoomInfo = GetRoomInfo(player.CurrentLevel, player.CurrentRoom);
                var oldRoomInfo = GetRoomInfo(oldLevel, oldRoom);
                if (newRoomInfo != null && oldRoomInfo != null)
                {
                    await _towerManager.MovePlayerToRoomAsync(guild, userId, oldRoomInfo, newRoomInfo);

                    // Post puzzle in the new room's channel
                    var nextChannel = guild.GetTextChannel(newRoomInfo.ChannelId);
                    if (nextChannel != null)
                    {
                        var nextKey = (player.CurrentLevel, player.CurrentRoom);
                        if (!_game.ActivePuzzles.ContainsKey(nextKey))
                        {
                            var nextPuzzle = _puzzleManager.GeneratePuzzle(player.CurrentLevel);
                            _game.ActivePuzzles[nextKey] = nextPuzzle;
                        }
                        await nextChannel.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(
                            _game.ActivePuzzles[nextKey], player.CurrentLevel, player.CurrentRoom));
                    }
                }
            }
            else
            {
                // Wrong answer
                var oldLevel = player.CurrentLevel;
                var oldRoom = player.CurrentRoom;
                _playerManager.PenalizePlayer(player);

                if (textChannel != null)
                    await textChannel.SendMessageAsync(embed: GameEmbeds.WrongAnswerEmbed(player, oldLevel));

                // Fire proactive commentary
                var dropLevels = oldLevel - player.CurrentLevel;
                _assistantService.CommentOnEvent($"{player.DisplayName} guessed wrong and dropped {dropLevels} level(s) to level {player.CurrentLevel} room 1.", player.UserId, channelId, HeistGameContext.Format(player));

                // Move player down if position changed
                if (player.CurrentLevel != oldLevel || player.CurrentRoom != oldRoom)
                {
                    var newRoomInfo = GetRoomInfo(player.CurrentLevel, player.CurrentRoom);
                    var oldRoomInfo = GetRoomInfo(oldLevel, oldRoom);
                    if (newRoomInfo != null && oldRoomInfo != null)
                    {
                        await _towerManager.MovePlayerToRoomAsync(guild, userId, oldRoomInfo, newRoomInfo);

                        // Post puzzle in the new (lower) room channel
                        var newChannel = guild.GetTextChannel(newRoomInfo.ChannelId);
                        if (newChannel != null)
                        {
                            var newKey = (player.CurrentLevel, player.CurrentRoom);
                            if (_game.ActivePuzzles.TryGetValue(newKey, out var existingPuzzle))
                            {
                                await newChannel.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(existingPuzzle, player.CurrentLevel, player.CurrentRoom));
                            }
                        }
                    }
                }
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<CardResult> UseCardAsync(ulong userId, PowerCard card, ulong? targetUserId, SocketGuild guild)
    {
        await _lock.WaitAsync();
        try
        {
            if (_game.Status != GameStatus.Active)
                return new CardResult { Success = false, Message = "No active game." };

            if (!_game.Players.TryGetValue(userId, out var player))
                return new CardResult { Success = false, Message = "You're not in the game." };

            if (!_cardManager.HasCard(player, card))
                return new CardResult { Success = false, Message = $"You don't have a {card} card." };

            CardResult result;

            switch (card)
            {
                case PowerCard.Knockback:
                case PowerCard.Freeze:
                case PowerCard.Chaos:
                    if (targetUserId == null)
                        return new CardResult { Success = false, Message = "This card requires a target player." };
                    if (!_game.Players.TryGetValue(targetUserId.Value, out var target))
                        return new CardResult { Success = false, Message = "Target player not found in game." };
                    if (targetUserId == userId)
                        return new CardResult { Success = false, Message = "You can't target yourself." };

                    result = card switch
                    {
                        PowerCard.Knockback => _cardManager.UseKnockback(player, target, _options.MaxLevels),
                        PowerCard.Freeze => _cardManager.UseFreeze(player, target),
                        PowerCard.Chaos => _cardManager.UseChaos(player, target, _game.ActivePuzzles, _puzzleManager),
                        _ => new CardResult { Success = false, Message = "Unknown card." }
                    };

                    // Handle knockback movement
                    if (card == PowerCard.Knockback && result.TargetMoved)
                    {
                        var oldRoomInfo = GetRoomInfo(result.TargetOldLevel, result.TargetOldRoom);
                        var newRoomInfo = GetRoomInfo(result.TargetNewLevel, result.TargetNewRoom);
                        if (oldRoomInfo != null && newRoomInfo != null)
                            await _towerManager.MovePlayerToRoomAsync(guild, targetUserId.Value, oldRoomInfo, newRoomInfo);
                    }

                    // If chaos, post new puzzle in target's channel
                    if (card == PowerCard.Chaos && result.NewPuzzle != null)
                    {
                        var targetRoom = GetRoomInfo(target.CurrentLevel, target.CurrentRoom);
                        if (targetRoom != null)
                        {
                            var ch = guild.GetTextChannel(targetRoom.ChannelId);
                            if (ch != null)
                                await ch.SendMessageAsync(embed: GameEmbeds.PuzzleEmbed(result.NewPuzzle, target.CurrentLevel, target.CurrentRoom));
                        }
                    }
                    break;

                case PowerCard.Shield:
                    result = _cardManager.UseShield(player);
                    break;

                case PowerCard.Spy:
                    _game.ActivePuzzles.TryGetValue((player.CurrentLevel, player.CurrentRoom), out var spyPuzzle);
                    result = _cardManager.UseSpy(player, spyPuzzle, _puzzleManager);
                    break;

                case PowerCard.Hint:
                    _game.ActivePuzzles.TryGetValue((player.CurrentLevel, player.CurrentRoom), out var hintPuzzle);
                    result = _cardManager.UseHint(player, hintPuzzle, _puzzleManager);
                    break;

                default:
                    result = new CardResult { Success = false, Message = "Unknown card." };
                    break;
            }

            // Broadcast non-private card usage to lobby
            if (result.Success && !result.IsPrivate)
            {
                var lobbyChannel = guild.GetTextChannel(_game.LobbyChannelId);
                if (lobbyChannel != null)
                    await lobbyChannel.SendMessageAsync(embed: GameEmbeds.CardUsedEmbed(result));

                // Fire proactive commentary for card effects on the target
                if (targetUserId != null && _game.Players.TryGetValue(targetUserId.Value, out var cardTarget))
                {
                    var targetRoom = GetRoomInfo(cardTarget.CurrentLevel, cardTarget.CurrentRoom);
                    if (targetRoom != null)
                        _assistantService.CommentOnEvent(result.Message, cardTarget.UserId, targetRoom.ChannelId, HeistGameContext.Format(cardTarget));
                }
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task EndGameAsync(SocketGuild guild)
    {
        await _lock.WaitAsync();
        try
        {
            if (_game.Status == GameStatus.Idle)
                return;

            _game.Status = GameStatus.Finished;
            await _towerManager.StripAllPlayerRolesAsync(guild, _game.Players.Keys);
            _game = new GameState();
        }
        finally
        {
            _lock.Release();
        }
    }

    public bool IsGameChannel(ulong channelId)
    {
        return _game.Status == GameStatus.Active &&
               _game.Levels.Any(l => l.Rooms.Any(r => r.ChannelId == channelId));
    }

    private RoomInfo? GetRoomInfo(int level, int room)
    {
        return _game.Levels
            .FirstOrDefault(l => l.LevelNumber == level)
            ?.Rooms.FirstOrDefault(r => r.RoomNumber == room);
    }

    private (LevelInfo? Level, RoomInfo? Room) FindLevelAndRoom(ulong channelId)
    {
        foreach (var level in _game.Levels)
        {
            var room = level.Rooms.FirstOrDefault(r => r.ChannelId == channelId);
            if (room != null)
                return (level, room);
        }
        return (null, null);
    }
}
