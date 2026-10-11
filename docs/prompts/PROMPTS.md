# Claude Code prompts – ABB Tic Tac Toe

Copy each prompt into the Claude Code panel when you reach that phase.
Run `/clear` before each phase. Use Plan mode where it says so.

---

## Phase 1.1 – Spec (Plan mode, no code)

```text
I'm building the ABB Tic Tac Toe assignment. Before any code exists I want a specification I can review and approve. Do not write code or scaffold anything in this session.

Read @docs/requirements/problem-statement.md (the brief) and @CLAUDE.md (my architecture constraints).

Create docs/SPEC.md with these sections:
1. Scope – in and out of scope, 5 lines max.
2. Requirements – a table with stable IDs (FR-xx functional, MH-xx must-have, BE-xx backend, FE-xx frontend, NFR-xx). Each row has one testable acceptance criterion in Given/When/Then form. Every bullet in the brief must map to at least one ID.
3. Domain model – types, fields and invariants. Board, current player, status, winner and winning cells are derived from the ordered move list.
4. State transitions – a table of (status × mode × action: Move, Undo, Reset, computer reply) → resulting state or error code.
5. API contract – for each endpoint: method, path, request JSON, a response JSON example, and the status code + ProblemDetails errorCode for every error case.
6. Computer strategy – the exact priority order and a deterministic tie-break for each step.
7. Ambiguity log – every place the brief is ambiguous, silent or self-contradictory. For each: the options, your recommendation, the consequence. Mark each one DECISION NEEDED and do not decide for me. Where CLAUDE.md already decides something, cite it instead of re-opening it.
8. Test traceability – map each of the 13 mandatory test cases and each acceptance criterion to requirement IDs.

Prefer tables over prose and keep it under ~300 lines. Before writing the file, list any questions that block you.
```

## Phase 1.2 – Apply decisions

```text
My decisions on the ambiguity log in docs/SPEC.md §7.2:

- A-01 (a) One global scoreboard per API process. The brief targets a local single-user app; restart clears it – known limitation.
- A-02 (a) Same id, moves cleared. Keeps the client's URL stable; "fresh session" = fresh round.
- A-03 (a) Keep the mode. Resetting a computer game should stay a computer game.
- A-04 (a) Mode change = create a new game via E1. Abandoned game is not scored; no extra endpoint.
- A-05 (a) player required and validated. A stale or double click gets NOT_YOUR_TURN instead of a silent wrong move.
- A-06 (a) mode required. Explicit contract, no hidden default.
- A-07 (a) Embed scoreboard in GameState and keep E6. One round trip after a game-ending move.
- A-08 (a) currentPlayer = null on the wire when complete. UI never shows a turn on a finished board.
- A-09 (a) winningCells = union of all completed lines. Every winning cell is highlighted.
- A-10 (a) Fixed precedence as written in §4.
- A-11 (a) 400 INVALID_REQUEST for malformed input. Every error carries an errorCode.
- A-12 (a) GAME_COMPLETED for undo on a finished game. Client can tell "game over" from "nothing to undo".
- A-13 (a) Games live until process restart. Known limitation; future: TTL eviction or persistent store.
- A-14 (a) Guid ids; non-Guid → 404. Not guessable.
- A-15 (a) Flat 9-cell array + cell indices. Matches the cell-index convention in CLAUDE.md.
- A-16 (a) New TwoPlayer game on every page load. Simplest; scoreboard survives on the server.
- A-17 (a) Show the computer reply immediately. Both moves arrive in one response.

1. Update docs/SPEC.md: move every A-item into §7.1 as decided (or mark each "DECIDED – see DECISIONS.md"), remove all DECISION NEEDED markers, and change the Status line to "Approved".
2. Create docs/DECISIONS.md with one ADR-lite entry per decision (A-01 … A-17): Context, Options considered, Decision, Consequences.
3. INVALID_REQUEST is a new errorCode not listed in .claude/rules/backend-dotnet.md – tell me the exact line to add there; don't edit the rules file yourself.
4. If any decision conflicts with CLAUDE.md or .claude/rules, tell me.
Finish with a bullet summary of what changed in SPEC.md.
```

---

## Phase 2.1 – Backend skeleton

```text
Read @docs/SPEC.md. Scaffold the backend skeleton only – no game logic yet.

- backend/ with a solution containing src/TicTacToe.Core (classlib), src/TicTacToe.Api (ASP.NET Core Web API using controllers, not minimal APIs), tests/TicTacToe.Core.Tests and tests/TicTacToe.Api.Tests (xUnit).
- Target framework: net10.0 (first confirm with `dotnet --list-sdks` that a 10.0 SDK is installed; stop and tell me if it isn't).
- backend/Directory.Build.props: Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true, LangVersion latest.
- References: Api → Core; Core.Tests → Core; Api.Tests → Api, plus Microsoft.AspNetCore.Mvc.Testing.
- Api: launchSettings with a single "http" profile on http://localhost:5080; remove the WeatherForecast sample; add `public partial class Program {}` for WebApplicationFactory.
- Use the dotnet CLI (dotnet new / sln / add reference), not hand-written project files.

Show me the plan first. After I approve, run `dotnet build backend` and `dotnet test backend` and show me the summary.
```

---

## Phase 3.1 – Domain tests first, RED (Plan mode)

```text
Phase 3a – domain tests first (RED). Use the modern-csharp-coding-standards skill.

From @docs/SPEC.md, write xUnit tests in backend/tests/TicTacToe.Core.Tests for TicTacToe.Core: the Game domain, the computer strategy, the Scoreboard and the GameService (with the in-memory repository). Write NO production logic yet.

Cover at least:
- valid move; invalid moves (out of bounds, occupied cell, wrong player, move after completion), each also asserting the turn did not change
- turn switching
- row win, column win, both diagonals – assert the winner AND the winning cells
- draw (full board, no winner)
- reset game – board, history and status cleared; X to move; same mode; scoreboard unchanged
- undo in two-player mode (removes 1 move, turn restored); undo with no moves → rejected; undo after completion → rejected (Option A)
- undo in computer mode (removes the computer's move and the human's previous move; X to move; board as before X's move)
- scoreboard – X win, O win and draw each counted exactly once; a rejected move after completion doesn't count again; Reset Scoreboard zeroes all counts
- computer strategy – one test per priority rule (win, block, center, corner, any), a test that winning beats blocking, a corner tie-break test, and a test that the computer does not move once the game is over

Rules:
- Tag every test with [Trait("Req", "<SPEC ID>")].
- Build positions from explicit move sequences so a reader can replay the board.
- For each computer-strategy test, use a board where ONLY the rule under test decides the answer, and add a one-line comment explaining why.
- Create only the minimal public types and signatures needed to compile; method bodies throw NotImplementedException.
- Run `dotnet test backend` and confirm the tests fail with NotImplementedException, not compile errors.

Before writing, show me the test names grouped by requirement ID.

Context budget: keep this session lean.
- Read only the files you need; reference SPEC sections by number instead of re-reading the whole file.
- Don't paste full file contents or full test output back to me – show a diff summary and the pass/fail summary line only.
- If a step needs a broad search or review, delegate it to a subagent so its output doesn't fill this context.
- Stop and tell me if you think the context is getting full before the task is done.
```

Commit the failing tests, then lock them: `touch .claude/tests.lock`

## Phase 3.2 – Implement, GREEN

```text
Phase 3b – implement (GREEN). Tests are locked: a hook blocks edits to test files.

Implement TicTacToe.Core so that every test passes. Constraints that matter most:
- The ordered move list is the single source of truth; board, current player, status, winner and winning cells are derived from it.
- Expected rule violations return a domain error/result type, not exceptions.
- GameService.MakeMove: load game → lock it → apply the human move → if VsComputer and still InProgress, apply the computer move → record the score once on the InProgress→Won/Draw transition → return the state.
- InMemoryGameRepository uses ConcurrentDictionary; one lock per game; the Scoreboard has its own lock.
- No ASP.NET references in Core and no new packages.

Work in small steps and run `dotnet test backend` after each. If a test looks wrong, stop and tell me why; do not try to change it.
Finish with the test summary and a 10-line explanation of the design that I could give to an interview panel.
```

## Phase 3.3 – Self-critique (no edits)

```text
Without changing any code: review your implementation against @docs/SPEC.md sections 3, 4 and 6. List, ranked by risk: requirements that pass only because of the specific test data, duplicated logic, unclear names, and anything a senior reviewer would question. For each item, say what you would change.

Context budget: keep this session lean.
- Read only the files you need; reference SPEC sections by number instead of re-reading the whole file.
- Don't paste full file contents or full test output back to me – show a diff summary and the pass/fail summary line only.
- If a step needs a broad search or review, delegate it to a subagent so its output doesn't fill this context.
- Stop and tell me if you think the context is getting full before the task is done.
```

Afterwards: `rm .claude/tests.lock`

---

## Phase 4.1 – REST API (Plan mode)

```text
Phase 4 – REST API. Use the dependency-injection-patterns skill for service registration.

Implement TicTacToe.Api exactly per the API contract in @docs/SPEC.md section 5:
- GamesController: POST /api/games, GET /api/games/{id}, POST /api/games/{id}/moves, POST /api/games/{id}/undo, POST /api/games/{id}/reset
- ScoreboardController: GET /api/scoreboard, POST /api/scoreboard/reset
- Request/response DTOs as records, separate from domain types; enums serialized as strings.
- Map domain errors to ProblemDetails (400 / 404 / 409 with an errorCode extension) in ONE place, not in every action.
- builder.Services.AddTicTacToe(); CORS for http://localhost:4200 in Development only; OpenAPI document in Development.
- Update TicTacToe.Api.http with one request per endpoint plus one invalid-move example.

Then write integration tests in TicTacToe.Api.Tests with WebApplicationFactory<Program>: one happy path per endpoint, one test per error (400 OUT_OF_BOUNDS, 404 GAME_NOT_FOUND, 409 CELL_OCCUPIED, NOT_YOUR_TURN, GAME_COMPLETED, NOTHING_TO_UNDO), and a full computer-mode round trip. The scoreboard is a singleton, so keep the tests isolated and explain how you did it.

Show the plan first. Then implement and run `dotnet test backend`.
```

---

## Phase 5.1 – Angular scaffold (can run in a 2nd tab during Phase 4)

```text
Phase 5a – Angular scaffold. Use the angular-new-app skill.

Create the Angular app in ./frontend:
- name tictactoe-ui; use the installed Angular CLI; standalone; SCSS; no SSR; no routing; keep the CLI defaults for zoneless and the test runner.
- flags: --skip-git (we are already inside a git repo) and --ai-config=none (this repo already has CLAUDE.md and rules; I don't want a second, conflicting instruction file).
- add proxy.conf.json forwarding /api to http://localhost:5080 and wire it into the serve target, so `npm start` uses it.
- add Prettier as a dev dependency (the format hook uses it); remove the placeholder welcome page.
Then run `npx ng build` and `npm test -- --watch=false` and show me the results.
```

## Phase 5.2 – Data layer (Plan mode)

```text
Phase 5b – data layer. Use the angular-developer skill.

Create in frontend/src/app/game/:
1. game.models.ts – types that mirror the API contract in @docs/SPEC.md section 5 field for field (GameState, MoveEntry, Scoreboard, MoveRequest, CreateGameRequest, ProblemDetails; string unions for Player, GameMode, GameStatus).
2. game-api.service.ts – HttpClient wrapper, one method per endpoint, relative /api URLs.
3. game.store.ts – a signal-based store with state, pending and error signals; methods newGame(mode), play(row, col), undo(), reset(), resetScoreboard(); computed statusMessage, isGameOver, canUndo (straight from the API's canUndo). It must not implement any game rule. Ignore actions while a request is pending. Map ProblemDetails.detail to error.
4. Unit tests – HttpTestingController for every GameApiService method (URL, verb, body), and store tests for success, error mapping and the pending guard.

Run `npm test -- --watch=false` and `npx ng build`.
```

---

## Phase 6.1 – UI (Plan mode)

```text
Phase 6 – UI. Use the angular-developer skill. Build the UI per the FE requirements in @docs/SPEC.md:

- GamePage: the only container; injects GameStore; starts a TwoPlayer game on load.
- Presentational components (inputs/outputs only, no services):
  - ModeSelector – Two Player / Play vs Computer; switching mode starts a new game
  - Board – 3×3 CSS grid of buttons; inputs board, winningCells, disabled; output cellSelected(index)
  - GameStatus – whose turn it is, or the winner / draw message, plus the current mode
  - MoveHistory – table Move | Player | Position, shown 1-based as "Row r, Column c"
  - Scoreboard – X wins / O wins / Draws, plus a Reset Scoreboard button
  - GameControls – Reset Game and Undo Last Move (disabled when !canUndo or pending)
- A dismissible error banner bound to store.error.
- Cells: <button> with an aria-label; disabled when occupied, game over or pending; the `winning` class highlights winning cells.
- Layout: board on the left, status / history / scoreboard on the right; clean on a 1280–1440px laptop screen; stacks below ~900px.

Then component tests: Board renders 9 buttons, emits only for enabled cells and applies `winning`; GameStatus shows the winner and draw messages; GameControls disables Undo when canUndo is false; MoveHistory renders 1-based positions.
Run `npm test -- --watch=false` and `npx ng build`.
```

---

## Phase 7 – Verify

```text
@agent-spec-verifier Produce the traceability report for the whole repository.
```

After `/clear`:

```text
@agent-code-reviewer Review git diff spec-approved...HEAD against docs/SPEC.md. Prioritise correctness and spec compliance over style. Report only; do not edit.
```

Bug fix template:

```text
Bug: <steps> → <actual result>. Expected: <expected result> (SPEC <ID>).
First write a failing test that reproduces it and show me it failing. Then make the smallest fix, then run all tests.
```

---

## Phase 8.1 – README

```text
Write README.md for the interview panel. Use exactly these sections, in this order:
1 Project overview · 2 Tech stack (versions read from the project files) · 3 Features implemented · 4 How to run the backend locally · 5 How to run the frontend locally · 6 API endpoint summary (table built from the controllers, with status codes; link the .http file and the OpenAPI URL) · 7 How to run tests (backend and frontend, plus a requirement → test mapping summary) · 8 AI tools and prompt summary (summarise docs/AI_WORKFLOW.md and link it; never invent prompts) · 9 Design decisions (from docs/DECISIONS.md: decision, why, trade-off) · 10 Clarifications and assumptions (state Undo Option A explicitly) · 11 Known limitations · 12 Future improvements.
Add a "Quick start (2 terminals)" block at the top and a repository structure tree.
Only include commands you have run successfully in this repo. Keep it skimmable: tables and short bullets, about 200 lines.
```
