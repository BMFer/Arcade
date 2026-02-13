# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Discord Arcade — a multi-game Discord bot platform. The first game is **Heist Tower**, a progressive word-scramble PvP game. Players climb tower levels by solving increasingly difficult word scrambles, using power cards, and sabotaging opponents. Full game design is in `prompt.md`.

## Tech Stack

- **Runtime:** .NET 9 (C#, nullable enabled, implicit usings)
- **Discord library:** Discord.Net 3.18.0
- **Solution:** `Arcade.sln` with two projects:
  - `Arcade.Core` — shared infrastructure (class library)
  - `Arcade.Heist` — the Heist Tower game (console app, references Arcade.Core)

## Build & Run Commands

```bash
# Build
dotnet build Arcade.sln

# Run
dotnet run --project Arcade.Heist

# Build release
dotnet build Arcade.sln -c Release
```

## Architecture

### Arcade.Core (shared infrastructure)
- `Configuration/BotOptions.cs` — Bot token, guild ID (section `"Bot"`)
- `Configuration/AiOptions.cs` — Ollama/assistant settings (section `"AI"`)
- `Models/AssistantProfile.cs`, `Models/PlayerPreferences.cs` — shared AI models
- `AI/OllamaService.cs` — HTTP client for Ollama API
- `AI/AssistantService.cs` — orchestrator, loads profiles, fire-and-forget commentary
- `AI/IAssistantEventHandler.cs` — interface for posting AI commentary to channels
- `Data/PlayerDataStore.cs` — thread-safe JSON persistence for player preferences
- `Discord/BotHostedServiceBase.cs` — abstract base for Discord bot lifecycle

### Arcade.Heist (game-specific)
- `Configuration/HeistOptions.cs` — game-specific settings (section `"Heist"`)
- `AI/HeistAssistantEventHandler.cs` — implements `IAssistantEventHandler` for Discord embed posting
- `AI/HeistGameContext.cs` — formats `PlayerState` into game context string for AI
- `Discord/HeistBotHostedService.cs` — subclass of `BotHostedServiceBase`, wires game events
- `Discord/Commands/HeistCommands.cs` — slash commands
- `Discord/Embeds/GameEmbeds.cs` — embed builders
- `Discord/Handlers/MessageHandler.cs` — message event handler
- `Game/Engine/` — GameManager, TowerManager, PlayerManager, PuzzleManager, CardManager
- `Game/Models/` — GameState, PlayerState, Puzzle, PowerCard, LevelInfo
- `Game/Words/` — WordBank, WordScrambler
- `Data/TowerDataStore.cs` — tower level persistence

### Configuration Sections (appsettings.json)
- `"Bot"` → `BotOptions` (BotToken, GuildId)
- `"AI"` → `AiOptions` (OllamaEndpoint, AssistantEnabled, etc.)
- `"Heist"` → `HeistOptions` (LobbyCountdownSeconds, MaxLevels, TowerDataPath, etc.)

### Adding a New Game
Future games (e.g., `Arcade.Trivia/`) should reference `Arcade.Core` and implement:
- `IAssistantEventHandler` for AI commentary
- Subclass of `BotHostedServiceBase` for Discord lifecycle
- Game-specific options class bound to its own config section

## Key Design Considerations

- Bot must manage Discord category/channel creation and permissions dynamically
- Per-player state tracking (current level, inventory, cooldowns, wrong-guess count)
- Word scramble generation with difficulty tiers and themed word lists
- Real-time interaction via Discord message events with answer validation
- Power card effects require cross-player state manipulation (knockback, freeze, chaos)
- `BotHostedServiceBase.OnReadyAsync` uses `Assembly.GetEntryAssembly()` for command module discovery (not `GetExecutingAssembly()`)
