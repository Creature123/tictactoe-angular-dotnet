# Tic Tac Toe — Angular + .NET (ABB Round 2 assignment)

A browser Tic Tac Toe game. The **.NET Web API owns every game rule and all state**; the Angular app
renders whatever the API returns. The panel grades clarity, correctness, tests and engineering judgment,
so prefer simple, explainable code over clever or "enterprise" code.

## Sources of truth (in priority order)
1. `docs/requirements/problem-statement.md`: the original brief. Read-only. Never edit it.
2. `docs/SPEC.md`: requirement IDs (FR-, MH-, BE-, FE-), acceptance criteria and the API contract.
3. `docs/DECISIONS.md`: resolved ambiguities and trade-offs (ADR-lite).
If code and SPEC disagree, stop and tell me. Do not silently pick one.

## Repository layout
- `backend/src/TicTacToe.Core`: domain (Game, Board, rules, computer strategy, Scoreboard), `GameService`, `IGameRepository` + in-memory implementation. No ASP.NET references.
- `backend/src/TicTacToe.Api`: controllers, DTOs, ProblemDetails error mapping, DI, CORS, OpenAPI.
- `backend/tests/TicTacToe.Core.Tests`: xUnit unit tests for rules and state transitions.
- `backend/tests/TicTacToe.Api.Tests`: xUnit integration tests with WebApplicationFactory.
- `frontend/`: Angular workspace (standalone components, signals, Vitest).
- `docs/AI_WORKFLOW.md`: the prompt log and AI usage notes. Append to it only through `/ai-workflow-log`.

## Commands
- Backend build/test: `dotnet build backend` · `dotnet test backend`
- Run API: `dotnet run --project backend/src/TicTacToe.Api` → http://localhost:5080 (OpenAPI in Development)
- Frontend: `cd frontend && npm start` → http://localhost:4200 (dev proxy forwards `/api` to :5080)
- Frontend tests/build: `cd frontend && npm test -- --watch=false` · `npx ng build`

## Architecture invariants (do not break these)
- **The ordered move list is the single source of truth.** Board, current player, status, winner and
  winning cells are *derived* from it. Undo = remove moves + re-derive. No parallel mutable fields that can drift.
- Current player is derived: X moves first; X when the move count is even, O when it is odd.
- Computer Mode: human is X, computer is O. `Game.Play` appends X's move and the computer's reply in one call (atomic), and never replies once the game is complete; `GameService` only loads, stores and scores the game under its per-game lock (D-01).
- Computer priority is exact: win > block > center > corner (0, 2, 6, 8 order) > first free cell. Deterministic.
- Scoreboard is updated exactly once, on the InProgress → Won/Draw transition. Reset Game never touches it.
- Undo after completion is disabled (Option A, see DECISIONS.md). The API returns `canUndo`; the UI never computes it.
- API positions are 0-based (`row`, `col` 0..2; cell index = row*3+col). The UI displays them 1-based ("Row 1, Column 1").
- Expected rule violations (occupied cell, wrong player, game over, nothing to undo) are domain results,
  not exceptions. They map to RFC 7807 ProblemDetails with an `errorCode` extension.
- The frontend never re-implements game rules. It sends intents and renders the returned state.

## How to work in this repo
- For any change touching more than 2 files, present a short plan first and wait for my approval.
- Domain logic is test-first: write or extend tests from SPEC IDs, show them failing, then implement.
- Never skip, delete, weaken or `[Skip]` a test to make it pass. If a test looks wrong, stop and explain why.
- After every change run the relevant build and tests, and report the actual command output summary.
- Keep changes small and on-scope. Do not add packages, auth, databases/EF, SSR, UI component
  libraries, MediatR/CQRS or generic repositories unless I ask.
- Do not commit or push. I review and commit each slice myself.

## Skills to use
- C# code: `modern-csharp-coding-standards` · DI registration in Program.cs: `dependency-injection-patterns`
- Angular code: `angular-developer` · scaffolding the Angular app: `angular-new-app`
- Reviews: delegate to the `code-reviewer` subagent (fresh context, read-only); spec coverage: `spec-verifier` subagent.
