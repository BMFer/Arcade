namespace Arcade.Heist.Game.Models;

public class GameState
{
    public GameStatus Status { get; set; } = GameStatus.Idle;
    public Dictionary<ulong, PlayerState> Players { get; set; } = [];
    public List<LevelInfo> Levels { get; set; } = [];
    public Dictionary<int, Puzzle> ActivePuzzles { get; set; } = [];
    public ulong? WinnerId { get; set; }
    public ulong LobbyChannelId { get; set; }
}

public enum GameStatus
{
    Idle,
    Lobby,
    Active,
    Finished
}
