# Discord Arcade — Roadmap

## Current State

Heist Tower is the first game in the Arcade platform. The core gameplay loop is complete and production-ready:

- 5-tier word-scramble progression across Discord categories with role-based visibility
- All 6 power cards (Knockback, Shield, Spy, Freeze, Chaos, Hint)
- Risk mechanic with consecutive-wrong penalties and cooldowns
- AI assistant system (Ollama-backed, 3 personalities, proactive commentary)
- Tower lifecycle management (init, teardown, role assignment)
- Race to the Top win condition (Mode A)
- 144 unit tests across `Arcade.Core.Tests` and `Arcade.Heist.Tests`

The shared `Arcade.Core` library is split from game-specific code, establishing the multi-game architecture.

---

## Phase 1 — Complete the Heist Design

Priority: fill the gaps between the game design document and the current implementation.

### 1.1 King of the Tower (Mode B)

The `CrownHoldSeconds` config already exists but has no backing logic. Implement a crown-hold timer that starts when a player reaches Level 5 and resets if they get knocked back. First player to hold the Crown Room for the configured duration wins.

- Add `CrownHolderUserId` and `CrownHoldStartedAt` to `GameState`
- Tick-based check in `HandleAnswerAsync` or a background timer task
- Knockback/Chaos that removes the holder resets the timer
- New `CrownHoldEmbed` showing live countdown
- Game mode selection in `/heist-start` (race vs. king)

### 1.2 Enhanced Puzzle Types

The current puzzle engine only generates single-word and double-word scrambles. Expand it to support richer puzzle formats at higher tiers.

- Riddle-based puzzles: clue text with a one-word answer
- Anagram puzzles: multiple valid answers accepted
- Timed puzzles: a countdown per puzzle at Tier 4-5, auto-penalize on expiry
- Category hints: group words by theme so the Hint card can reveal the category

### 1.3 Spectator Experience

Players who are eliminated or haven't joined should still be able to watch. Add a `#heist-spectate` channel that receives a feed of game events (solves, knockbacks, card plays) via embeds, giving the server community a reason to follow along.

### 1.4 Post-Game Summary

After a game ends, post a recap embed to the lobby channel: final standings, total cards played, fastest solve, most knockbacks dealt/received, and a highlight reel of notable moments.

---

## Phase 2 — Player Progression

Priority: give players a reason to come back.

### 2.1 Seasonal Ladder (Mode C)

Persistent cross-game stats stored in `PlayerDataStore`:

- XP earned per game (scaled by placement, levels cleared, cards used)
- Ranks with thresholds (Lookout, Safecracker, Mastermind, etc.)
- Cosmetic titles displayed in embeds
- Seasonal resets with archive of past seasons

### 2.2 Player Profiles

`/profile` command showing a player's lifetime stats, current rank, favorite assistant, win/loss record, and most-used power cards. Backed by an extended `PlayerPreferences` model.

### 2.3 Achievements

Milestone-based unlocks: "First Heist", "Win Without Using Cards", "Knock Back 3 Players in One Game", "Clear All 5 Levels Without a Wrong Guess". Stored per-player, displayed in profiles and announced in-game.

### 2.4 Leaderboards

`/leaderboard` command with filters (all-time, seasonal, weekly). Show top players by XP, wins, fastest completions, and highest streaks.

---

## Phase 3 — Platform Infrastructure

Priority: make `Arcade.Core` robust enough to host multiple games without friction.

### 3.1 Database Migration

Replace JSON file persistence (`PlayerDataStore`, `TowerDataStore`) with SQLite via Entity Framework Core. JSON works for prototyping but doesn't scale to concurrent games, cross-game stats, or complex queries.

- `Arcade.Core` provides a shared `ArcadeDbContext` with `Players`, `GameSessions`, `Achievements` tables
- Each game project adds its own migrations for game-specific tables
- Backward-compatible: import existing JSON data on first run

### 3.2 Multi-Server Support

Currently the bot targets a single guild via `GuildId`. Refactor to support multiple guilds:

- Per-guild game state isolation
- Guild-scoped configuration (different tower sizes, cooldown tuning)
- Shared player identity across guilds

### 3.3 Event Bus

Replace direct method calls between managers with an in-process event bus. Game events (`PlayerAdvanced`, `CardUsed`, `GameWon`) are published once and consumed by multiple listeners (AI commentary, spectator feed, stats tracking, achievements) without coupling the game engine to every downstream system.

### 3.4 AI Provider Abstraction

The current `OllamaService` is hardcoded to a single LLM backend. Abstract it behind an `ILanguageModelProvider` interface so the platform can support:

- Ollama (local, current)
- OpenAI / Anthropic API (cloud, higher quality)
- Mock provider (for testing and offline development)
- Per-assistant model routing (some assistants use local, others use cloud)

### 3.5 Configuration Dashboard

A simple web UI (ASP.NET Minimal API + static HTML) for server admins to configure game settings, manage tower state, view active games, and browse player stats without using slash commands.

---

## Phase 4 — New Games

Priority: prove the multi-game architecture by shipping a second game.

### 4.1 Arcade.Trivia

A trivia game using the same tower/category structure but with multiple-choice questions instead of word scrambles. Demonstrates that `Arcade.Core` (bot lifecycle, AI assistants, player data, hosted service base) transfers cleanly to a new game.

- `Arcade.Trivia/` project referencing `Arcade.Core`
- Own `TriviaOptions`, `TriviaCommands`, `TriviaBotHostedService`
- Question bank with categories and difficulty tiers
- Same power card concept but trivia-flavored (50/50, Skip, Steal)

### 4.2 Arcade.Cipher

A code-breaking cooperative game where players work together to decode encrypted messages before a timer expires. Each level introduces harder ciphers (Caesar, substitution, Vigenere). Tests a cooperative mode instead of PvP.

### 4.3 Game Launcher

Once multiple games exist, add a shared `/arcade` command that lists available games and lets players start any of them. Each game registers itself with the platform on startup. The launcher handles game discovery, session management, and prevents channel conflicts.

---

## Phase 5 — Community and Polish

### 5.1 Custom Word Lists

Let server admins upload themed word lists (e.g., a movie-themed heist, a holiday event). Validate format on upload, store per-guild, and allow selection at game start.

### 5.2 Tournament Mode

Bracket-style multi-round heists with automated matchmaking, seeding based on ladder rank, and a final championship round. Results posted to a `#tournament` channel.

### 5.3 Bot Presence and Activity

Set the bot's Discord presence to reflect the current state: "Watching a heist in progress", "Waiting for players", or idle. Small touch that makes the bot feel alive.

### 5.4 Localization

Extract all player-facing strings into resource files. Support English as the default with community-contributed translations. Per-guild language selection.

### 5.5 Documentation Site

A public docs site (GitHub Pages or similar) covering:

- Player guide: how to play, card strategies, tips
- Admin guide: setup, configuration, tower management
- Developer guide: how to add a new game to the platform
- API reference for `Arcade.Core` abstractions

---

## Non-Goals

Things deliberately out of scope to keep the project focused:

- **Monetization** — No premium features, paid cards, or loot boxes. This is a community project.
- **Voice channel integration** — Text-based gameplay keeps the barrier to entry low.
- **Cross-platform** — Discord is the only target. No web or mobile clients.
- **Real-time multiplayer sync** — Discord's message-based model is the interaction layer. No WebSocket game servers.

---

## Guiding Principles

1. **Games should be fun in 2 minutes.** If a new player can't understand the game within their first round, the design needs simplifying.
2. **The platform serves the games.** `Arcade.Core` exists to reduce boilerplate, not to impose architecture. If a game needs to break the pattern, let it.
3. **Test what matters.** Pure game logic and data persistence get thorough unit tests. Discord integration gets manual testing. Don't mock what you can't control.
4. **Ship incrementally.** Each phase delivers standalone value. No phase depends on a future phase being complete.
