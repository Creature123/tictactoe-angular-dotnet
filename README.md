# Tic Tac Toe — Angular + .NET

[![CI](https://github.com/Creature123/tictactoe-angular-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/Creature123/tictactoe-angular-dotnet/actions/workflows/ci.yml)

A browser Tic Tac Toe game. The **.NET Web API owns every game rule and all state**. The Angular app sends
intents (play, undo, reset) and renders whatever the API returns.

## Quick start (2 terminals)

```bash
# Terminal 1 – API on http://localhost:5080 (Swagger UI at /swagger)
dotnet run --project backend/src/TicTacToe.Api

# Terminal 2 – UI on http://localhost:4200 (dev proxy forwards /api to :5080)
cd frontend && npm install && npm start
```

**Prerequisites**
- .NET SDK **10.0.400** (pinned in [global.json](global.json), `rollForward: latestFeature`)
- Node.js **24** (npm 11; same version as CI)

---

## 1. Project overview

- Two modes: **Two Player** (X and O on the same screen) and **Play Against Computer** (human X, computer O).
- **The ordered move list is the single source of truth.** Board, current player, status, winner, winning
  cells and `canUndo` are derived from it. Undo removes moves and re-derives everything.
- Expected rule violations (occupied cell, wrong turn, game over, nothing to undo) are domain results, not
  exceptions. The API maps them to RFC 7807 ProblemDetails with an `errorCode` extension.
- Built spec-first: [docs/SPEC.md](docs/SPEC.md) (requirement IDs + acceptance criteria + API contract) →
  [docs/DECISIONS.md](docs/DECISIONS.md) → tests → code.

**Repository structure**

```text
.
├── backend/
│   ├── src/TicTacToe.Core/        # Domain: Game, rules, ComputerStrategy, Scoreboard, GameService, repository (no ASP.NET)
│   ├── src/TicTacToe.Api/         # Controllers, DTOs, ProblemDetails mapping, DI, CORS, OpenAPI, TicTacToe.Api.http
│   └── tests/
│       ├── TicTacToe.Core.Tests/  # xUnit unit tests (rules, state transitions, computer strategy)
│       └── TicTacToe.Api.Tests/   # xUnit integration tests (WebApplicationFactory)
├── frontend/src/app/
│   ├── game/                      # game-page, board, game-status, game-controls, mode-selector,
│   │                              # move-history, scoreboard, game.store, game-api.service
│   └── theme/                     # ThemeService (light/dark)
├── docs/                          # SPEC, DECISIONS, AI_WORKFLOW, prompts/PROMPTS, requirements/ (original brief)
├── .claude/                       # Claude Code setup: rules, agents, hooks, skills, settings
├── .github/workflows/ci.yml       # Build, test and vulnerability checks
├── global.json                    # .NET SDK pin
└── CLAUDE.md                      # Project instructions for Claude Code
```

## 2. Tech stack

| Layer | Technology | Version (from project files) |
|-------|------------|------------------------------|
| Backend | ASP.NET Core Web API, C# (`LangVersion latest`, nullable, warnings as errors) | .NET 10 (`net10.0`, SDK 10.0.400) |
| API docs | Microsoft.AspNetCore.OpenApi + Swashbuckle.AspNetCore.SwaggerUI | 10.0.11 · 10.2.3 |
| Backend tests | xUnit + Microsoft.AspNetCore.Mvc.Testing | 2.9.3 · 10.0.11 |
| Frontend | Angular (standalone components, signals), TypeScript | ^22.2.0 · ~6.0.2 |
| Frontend tests | Vitest (via `ng test`) | ^5.0.0 |
| CI | GitHub Actions (`ubuntu-latest`, Node 24) | [ci.yml](.github/workflows/ci.yml) |

## 3. Features implemented

| Area | What you get |
|------|--------------|
| Board | 3×3 board; X moves first; turns alternate; occupied cells are locked |
| Results | Row, column and diagonal wins; draw on a full board; all winning cells highlighted (two lines at once are both highlighted) |
| Modes | Two Player and Play Against Computer; changing mode starts a new game |
| Computer | Deterministic: win > block > center > corner (0, 2, 6, 8) > first free cell; the reply comes back in the same response as X's move |
| Move history | Move number, player and 1-based position ("Row 1, Column 1") |
| Undo | Two Player: last move. Computer: the computer move and the human move before it. Disabled when there is nothing to undo and after the game is finished |
| Reset Game | Same game id and mode, empty board and history, X to move; scoreboard unchanged |
| Scoreboard | X wins, O wins, draws, served by the backend; counted exactly once per finished game; Reset Scoreboard |
| Errors | Every error is ProblemDetails with an `errorCode`; the UI shows the `detail` message |
| Dark theme | Follows the OS setting by default; a toggle overrides it and the choice is remembered in the browser |

## 4. How to run the backend locally

```bash
dotnet build backend
dotnet run --project backend/src/TicTacToe.Api
```

- Listens on **http://localhost:5080** (Development profile). `/` redirects to Swagger UI.
- Swagger UI: http://localhost:5080/swagger · OpenAPI document: http://localhost:5080/openapi/v1.json
- Health check: http://localhost:5080/health → `Healthy`
- Swagger UI, OpenAPI and CORS for `http://localhost:4200` are enabled in Development only.

## 5. How to run the frontend locally

```bash
cd frontend
npm install
npm start          # ng serve → http://localhost:4200
npx ng build       # production build → frontend/dist/tictactoe-ui
```

- Start the API first. [proxy.conf.json](frontend/proxy.conf.json) forwards `/api` to `http://localhost:5080`.
- A page load creates a new Two Player game (A-16); the scoreboard is kept by the server.

> **Troubleshooting:** if `NODE_ENV=production` is set in your shell, npm skips devDependencies and
> `ng`/`vitest` are missing. Run `npm install --include=dev`.

## 6. API endpoint summary

Positions are 0-based (`row`, `col` 0..2; cell index = `row*3+col`). Full contract: [SPEC §5](docs/SPEC.md).

| # | Method + path | Body | Success | Errors (status · errorCode) |
|---|---------------|------|---------|-----------------------------|
| E1 | `POST /api/games` | `{ "mode": "TwoPlayer" \| "VsComputer" }` | 201 + `Location`, `GameState` | 400 · `INVALID_REQUEST` |
| E2 | `GET /api/games/{id}` | — | 200 `GameState` | 404 · `GAME_NOT_FOUND` (also for a non-Guid id) |
| E3 | `POST /api/games/{id}/moves` | `{ "player": "X", "row": 0, "col": 2 }` | 200 `GameState` (VsComputer: includes the computer reply) | 404 · `GAME_NOT_FOUND` · 400 · `INVALID_REQUEST` · 400 · `OUT_OF_BOUNDS` · 409 · `GAME_COMPLETED` · 409 · `NOT_YOUR_TURN` · 409 · `CELL_OCCUPIED` (checked in this order, A-10) |
| E4 | `POST /api/games/{id}/undo` | — | 200 `GameState` | 404 · `GAME_NOT_FOUND` · 409 · `NOTHING_TO_UNDO` · 409 · `GAME_COMPLETED` |
| E5 | `POST /api/games/{id}/reset` | — | 200 `GameState` (same id and mode) | 404 · `GAME_NOT_FOUND` |
| E6 | `GET /api/scoreboard` | — | 200 `{ xWins, oWins, draws }` | — |
| E7 | `POST /api/scoreboard/reset` | — | 200 `{ xWins: 0, oWins: 0, draws: 0 }` | — |
| — | `GET /health` | — | 200 `Healthy` | — |

Framework-level errors also carry an `errorCode`: 415 · `UNSUPPORTED_MEDIA_TYPE` (non-JSON body) and
404 · `NOT_FOUND` (no matching route).

`GameState` = `id, mode, status, currentPlayer (null when finished), winner, winningCells, board[9], moves, canUndo, scoreboard`.

Try it: [backend/src/TicTacToe.Api/TicTacToe.Api.http](backend/src/TicTacToe.Api/TicTacToe.Api.http)
(VS Code REST Client / Rider / Visual Studio) or Swagger UI at http://localhost:5080/swagger.

## 7. How to run tests

```bash
dotnet test backend                      # from the repo root
cd frontend && npm test -- --watch=false # Vitest, single run
```

| Suite | Tests | Status |
|-------|------:|--------|
| `TicTacToe.Core.Tests` (unit) | 91 | all passing |
| `TicTacToe.Api.Tests` (integration) | 38 | all passing |
| Frontend (Vitest, 11 spec files) | 62 | all passing |
| **Total** | **191** | |

CI runs the same commands on every push and pull request, plus `dotnet list package --vulnerable` and `npm audit --audit-level=high`.

**Requirement → test mapping** (the brief's 13 mandatory cases; full matrix in [SPEC §8](docs/SPEC.md))

| # | Test case | Requirement IDs | Where |
|---|-----------|-----------------|-------|
| 1 | Valid move | FR-02, FR-07, MH-02, BE-01 | `GameTests`, `GamesEndpointTests` |
| 2 | Invalid move (one per errorCode) | FR-04, FR-08, BE-04…BE-10 | `GameTests`, `GameErrorTests` |
| 3 | Turn switching | FR-05, FR-07, FR-08 | `GameTests` |
| 4–6 | Row / column / diagonal win | FR-09, FR-10, FR-11 | `GameTests` |
| 7 | Draw | FR-15 | `GameTests` |
| 8 | Reset game | FR-05, FR-18 | `GameTests`, `GameServiceTests`, `GamesEndpointTests` |
| 9 | Undo, two-player | MH-03, MH-05, MH-06 | `GameTests`, `GamesEndpointTests` |
| 10 | Undo, computer | MH-04, MH-05, MH-07 | `GameTests`, `ComputerModeTests` |
| 11 | Scoreboard update | FR-14, FR-17, MH-09, MH-10 | `ScoreboardTests`, `GameServiceTests`, `ScoreboardEndpointTests` |
| 12 | Computer move selection | MH-13, MH-14, MH-16…MH-21 | `ComputerStrategyTests` (one per SPEC §6 example) |
| 13 | Move after completion | FR-13, BE-07, MH-15 | `GameTests`, `GameErrorTests` |
| — | UI rendering + HTTP | FR-03, FR-06, FR-12, FR-16, MH-01, FE-01…FE-08 | `frontend/src/app/**/*.spec.ts` |

## 8. AI tools and prompt summary
 I used AI as an engineering team member, not an autopilot. I set the architecture and constraints up front (CLAUDE.md, rules, guardrails), had Claude turn the brief into a spec, and made every decision in the ambiguity log myself. Claude Code then implemented phase by phase under Plan mode and test-first discipline; I reviewed each plan and diff, triaged every review finding, and committed each slice myself. The "Notable AI mistakes I caught" table in AI_WORKFLOW.md shows where my review changed the outcome.
Built with **Claude Code** (VS Code extension + CLI), planned with Claude chat. Full log:
[docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md) · prompts used per phase: [docs/prompts/PROMPTS.md](docs/prompts/PROMPTS.md).

**Workflow:** brief → SPEC with requirement IDs → I resolved each ambiguity in DECISIONS → Plan mode before every
multi-file change → domain tests first (RED), reviewed by me, then locked → implementation (GREEN) →
independent review → I committed each slice myself after reading the diff.

**Prompt phases (PROMPTS.md):** 1 Spec + apply decisions · 2 Backend skeleton · 3 Domain tests RED → implement
GREEN → self-critique · 4 REST API · 5 Angular scaffold + data layer · 6 UI · 7 Verify · 8 README.

**Claude Code setup**

| Piece | Purpose |
|-------|---------|
| [CLAUDE.md](CLAUDE.md) | Sources of truth, architecture invariants, working rules (plan first, test-first, never weaken tests, no commits) |
| [.claude/rules/](.claude/rules/) | Path-scoped rules: `backend-dotnet.md`, `frontend-angular.md`, `testing.md` |
| Skills | `modern-csharp-coding-standards`, `dependency-injection-patterns`, `angular-developer`, `angular-new-app`, custom `/ai-workflow-log` |
| Subagents | `code-reviewer` (fresh context, read-only, reviews against SPEC) · `spec-verifier` (requirement → code → test traceability) |
| Hooks | `guard-tests.mjs` blocks edits to test files while `.claude/tests.lock` exists · `format-on-edit.mjs` runs `dotnet format` / Prettier |
| Permissions | [settings.json](.claude/settings.json) blocks edits to `docs/requirements/**` and `.env*`, asks before commits, pushes and new packages |

**Examples of AI mistakes I caught** (full table in AI_WORKFLOW.md): `global.json` placed in `backend/` where root
commands ignore it · a corner-rule test that still passed with the rule deleted · a controller-level
`[Produces("application/json")]` that broke `application/problem+json` (13 failing tests) · DECISIONS A-14
describing a route constraint the code does not use.

## 9. Design decisions

Full context and options in [docs/DECISIONS.md](docs/DECISIONS.md).

| ID | Decision | Why | Trade-off |
|----|----------|-----|-----------|
| ADR-00 | Undo disabled after a win or draw (Option A) | Scoreboard changes only on InProgress → Won/Draw and never needs reversing | Players cannot take back a finishing move |
| D-01 | Immutable `Game`; `Play` appends X's move and the computer reply | "No reply after completion" and "human can't play O" are enforced and unit-tested in Core | New `Game` per move (≤ 9 moves, negligible); `GameService` still locks per game |
| A-01 | One global scoreboard per API process | Brief targets a local single-user app | All tabs share it; lost on restart |
| A-02 / A-03 | Reset keeps the game id and mode | "Fresh session" = fresh round; stable id | Changing mode needs a new game |
| A-04 | Changing mode = `POST /api/games` | No extra endpoint or mid-game rules | Abandoned game is not scored |
| A-05 / A-06 | `player` and `mode` are required | Stale/double clicks rejected; explicit contract | Slightly larger request bodies |
| A-07 | Scoreboard embedded in `GameState` | One round trip after a game-ending move | Scoreboard data in two responses |
| A-08 | `currentPlayer` is `null` when finished | UI never shows a turn on a finished board | Client type is `Player \| null` |
| A-09 | `winningCells` = union of all completed lines | Every winning cell is highlighted | Win detection returns all lines |
| A-10 | Fixed error precedence (404 → 400 → 409s) | One deterministic errorCode per request | Only the first violation is reported |
| A-11 | Malformed input → 400 `INVALID_REQUEST` | Every error has an `errorCode` | Custom model-binding/JSON error handling |
| A-12 | Undo on a finished game → 409 `GAME_COMPLETED` | Client can tell "game over" from "nothing to undo" | — |
| A-13 | Games kept until restart | Simple, no background work | Memory grows with games |
| A-14 | Guid ids; non-Guid → 404 `GAME_NOT_FOUND` | Unguessable ids, consistent errorCode | Parsed in the controller, not a route constraint |
| A-15 | Board as flat 9-cell array | Matches the cell-index convention | UI converts to 1-based labels |
| A-16 | New Two Player game on every page load | No client persistence | Refresh drops the current game |
| A-17 | Computer reply shown immediately | Both moves come back in one response | No "thinking" animation |

## 10. Clarifications and assumptions

- **Undo after completion: Option A.** Undo is disabled once a game is won or drawn. The API returns
  `canUndo: false` (the UI never computes it) and `POST /undo` on a finished game returns 409 `GAME_COMPLETED`.
  The scoreboard therefore never has to be reversed.
- Undo in Computer mode removes the computer's move and the human move before it.
- "Session-level scoreboard" = one scoreboard per running API process (A-01).
- The API uses 0-based positions; the UI shows 1-based "Row 1, Column 1".
- Every resolved ambiguity has an ID in [docs/SPEC.md §7](docs/SPEC.md) and an entry in DECISIONS.md.

## 11. Known limitations

- **In-memory state:** all games and the scoreboard are lost when the API restarts.
- **Single global scoreboard:** shared by every browser tab and user (A-01).
- **No eviction:** games are never removed, and the per-game locks (`_gameLocks`) are never trimmed, so memory grows with the number of games created.
- **415 before 404:** a non-JSON body sent to an unknown game id returns 415 `UNSUPPORTED_MEDIA_TYPE`, not 404 `GAME_NOT_FOUND`, because the framework checks the content type before the controller runs.
- **HTTP only:** no HTTPS profile or redirection; local development only.
- **No authentication:** anyone who can reach the API can play or reset the scoreboard.
- **The computer is beatable by design:** its fixed priority (win > block > center > corner > first free) does not handle forks.

## 12. Future improvements

- Persistent store (e.g. a database) behind `IGameRepository`, plus TTL eviction of idle games and their locks.
- Per-client scoreboards (cookie or client id).
- Inline theme script in `index.html` to apply the saved theme before Angular boots (avoids a brief light flash in dark mode).
- HTTPS and authentication for anything beyond local use.
- Optional unbeatable (minimax) computer difficulty.
- End-to-end browser tests (e.g. Playwright) for the main acceptance criteria.
