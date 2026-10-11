# AI-Assisted Development Workflow

<!-- Before submission, edit the Workflow and Guardrails sections so they describe exactly what you did. -->

This file records how the solution was built with AI assistance, so that the panel can see the prompts, the
AI output, and my own review and changes. Each phase entry is appended at the end of that phase with the
`/ai-workflow-log` skill. The "Human review" and "Manual changes" fields are written by me, not the AI.

## Tools
| Tool | Used for |
|------|----------|
| Claude (chat / Projects) | Reading the brief, planning phases, drafting the Claude Code configuration |
| Claude Code (VS Code extension + CLI) | Spec generation, TDD implementation, refactoring, reviews |
| Skills: `modern-csharp-coding-standards`, `dependency-injection-patterns` (Aaron Stannard, MIT) | C# conventions |
| Skills: `angular-developer`, `angular-new-app` (Angular team, MIT) | Modern Angular conventions and scaffolding |
| Skill: `code-reviewer` (Jeffallan, MIT), loaded into the `code-reviewer` subagent | Independent, read-only reviews |
| Custom: `spec-verifier` subagent, `/ai-workflow-log` skill, guard hooks | Traceability, logging, guardrails |

## Workflow
1. **Requirements → Spec.** Claude turned the brief into `docs/SPEC.md`, with requirement IDs, Given/When/Then
   criteria, the API contract and an ambiguity log. I resolved each ambiguity in `docs/DECISIONS.md`.
2. **Plan before code.** Every multi-file change started in Plan Mode. I approved or edited the plan before any edits.
3. **Test-first domain.** The tests were generated first from SPEC IDs. I reviewed and corrected them, then locked them (`.claude/tests.lock`,
   enforced by a PreToolUse hook) while the AI implemented the code to make them pass.
4. **Thin vertical slices.** I committed each slice myself after reviewing the diff and running the tests.
5. **Independent review.** The `code-reviewer` subagent ran in a fresh context against SPEC. I triaged each finding
   (fix / won't fix + reason).
6. **Verification.** Automated tests, the `spec-verifier` traceability report, and a manual walk-through of every
   acceptance criterion in the browser.

## Guardrails
- `.claude/settings.json` blocks edits to `docs/requirements/**` and `.env*`, and asks before commits, pushes and new packages.
- `.claude/hooks/guard-tests.mjs` blocks edits to test files while `.claude/tests.lock` exists (prevents the
  AI from weakening tests to get to green).
- `.claude/hooks/format-on-edit.mjs` formats edited files deterministically (`dotnet format`, Prettier) instead of
  relying on the model.

## Notable AI mistakes I caught
<!-- Keep a running list as you go. This is the most valuable section for the panel. -->
| Phase | What the AI produced | Why it was wrong | How I caught it | Fix |
|-------|----------------------|------------------|-----------------|-----|
| Phase 1 | SPEC tie-break for "block" didn't cover an X fork (two threats) | Computer behaviour would be non-deterministic and untestable in a fork | Reviewed §6 strategy against my own walk-through scenarios | Required "block the lowest-indexed cell" + example T2 |
| Phase 2 | global.json was placed in backend/ | dotnet resolves global.json from the working directory upward; commands run from the repo root would ignore it and could pick the .NET 6 SDK | Claude flagged it after creating the file; I verified with `dotnet --version` from the root | Moved global.json to the repo root |
## Phase log
<!-- Entries are appended below by /ai-workflow-log -->

### Phase 1 – Requirement to specification — 2026-10-11 11:50
**Goal:** Turn the brief into a traceable spec (requirement IDs, acceptance criteria, API contract) and resolve every ambiguity.
**Prompts used:**
Phase 1.1 – Spec (Plan mode, no code):
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
Phase 1.2 – Apply decisions:
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
**AI generated:**
- `docs/SPEC.md`: FR/MH/BE/FE/NFR requirement IDs with Given/When/Then criteria; domain model where the ordered move list is the single source of truth; state transitions; API contract (shapes, endpoints, ProblemDetails + `errorCode`); deterministic computer strategy (win > block > center > corner > first free) with example boards; ambiguity log; traceability for the 13 mandatory tests and the brief's acceptance criteria.
- `docs/DECISIONS.md`: ADR-00 (undo disabled after completion, Option A) and A-01..A-17 (e.g. reset keeps game id and mode, scoreboard embedded in game state, `currentPlayer` null when complete, `winningCells` = union of completed lines, fixed error precedence, 400 `INVALID_REQUEST`, non-Guid id → 404, flat 9-cell board).
- `docs/prompts/PROMPTS.md`: prompt templates for Phases 1–8.
- Flagged that `.claude/rules/backend-dotnet.md` no longer matched A-09, A-11 and A-14.
**Human review:** Reviewed line by line: SPEC §6 computer-strategy example boards (replayed each one by hand), §4 state transitions vs my Option A decision, §5 error codes vs the backend rules file, and all 17 ambiguity items.
**Manual changes:** Updated `.claude/rules/backend-dotnet.md` by hand so it matches decisions A-09 (winningCells = union of lines), A-11 (new `INVALID_REQUEST` code) and A-14 (non-Guid id → 404). Claude flagged the mismatch; I made the edits myself rather than letting the AI change its own rules. Also asked for a deterministic fork tie-break (block lowest-index cell), which became example T2.
**Rejected AI suggestions:** None in this phase. All 17 recommendations matched my own reasoning; I wrote a one-line justification for each.
**Assumptions / trade-offs:** Single global scoreboard and no game eviction (lost on restart) – accepted for a local single-user app, listed as known limitations.
**Verification:** Docs-only phase, no code yet, so no build or tests run. Manual review of SPEC against the brief and the rules file (see Human review). Committed as `054c9b3`.

### Phase 2 – Backend skeleton — 2026-10-11 12:14
**Goal:** Scaffold the .NET 10 solution (Core, Api, two xUnit test projects) with no game logic, building and testing green.
**Prompts used:**
```
Read @docs/SPEC.md. Scaffold the backend skeleton only – no game logic yet.

- backend/ with a solution containing src/TicTacToe.Core (classlib), src/TicTacToe.Api (ASP.NET Core Web API using controllers, not minimal APIs), tests/TicTacToe.Core.Tests and tests/TicTacToe.Api.Tests (xUnit).
- Target framework: net10.0 (first confirm with `dotnet --list-sdks` that a 10.0 SDK is installed; stop and tell me if it isn't).
- backend/Directory.Build.props: Nullable enable, ImplicitUsings enable, TreatWarningsAsErrors true, LangVersion latest.
- References: Api → Core; Core.Tests → Core; Api.Tests → Api, plus Microsoft.AspNetCore.Mvc.Testing.
- Api: launchSettings with a single "http" profile on http://localhost:5080; remove the WeatherForecast sample; add `public partial class Program {}` for WebApplicationFactory.
- Use the dotnet CLI (dotnet new / sln / add reference), not hand-written project files.

Show me the plan first. After I approve, run `dotnet build backend` and `dotnet test backend` and show me the summary.
```
```
Add backend/global.json pinning the SDK to 10.0.400 with "rollForward": "latestFeature", so anyone cloning the repo builds with .NET 10 even if older SDKs are installed. Then re-run dotnet build backend and dotnet test backend.
```
```
Move global.json from backend/ to the repo root so it applies when commands run from the root. Then run `dotnet --version` from the repo root to confirm it shows 10.0.400, and re-run dotnet build backend and dotnet test backend.
```
**AI generated:**
- Checked `dotnet --list-sdks` (6.0.403 and 10.0.400 installed) and presented a plan in plan mode before scaffolding.
- `backend/TicTacToe.slnx` (the .NET 10 default format) with `/src/` and `/tests/` solution folders; `TicTacToe.Core` (classlib), `TicTacToe.Api` (`webapi --use-controllers --no-https`), `TicTacToe.Core.Tests` and `TicTacToe.Api.Tests` (xUnit), all created with the dotnet CLI.
- `backend/Directory.Build.props` with Nullable, ImplicitUsings, TreatWarningsAsErrors and LangVersion latest; removed the duplicate settings from each `.csproj`.
- References Api → Core, Core.Tests → Core, Api.Tests → Api, plus `Microsoft.AspNetCore.Mvc.Testing` 10.0.11.
- Api: deleted the WeatherForecast sample (and `Class1.cs` from Core); `Program.cs` keeps controllers and Development-only OpenAPI, adds `public partial class Program { }`, and drops `UseAuthorization` (out of plan, disclosed); a single `http` profile on :5080; `TicTacToe.Api.http` points at :5080.
- Deleted the template `UnitTest1.cs` files; added `HostSmokeTests.Host_Starts_ServesOpenApiDocument` (`WebApplicationFactory<Program>`, `GET /openapi/v1.json` → 200, `[Trait("Req","NFR-02")]`).
- `global.json` (SDK 10.0.400, `rollForward: latestFeature`) via `dotnet new globaljson`, first in `backend/`; flagged that the CLI resolves it from the working directory upward, then moved it to the repo root.
**Human review:** Reviewed line by line: Program.cs (every line explained to myself), the four .csproj files (confirmed Core has no ASP.NET reference), Directory.Build.props, launchSettings.json (port 5080), and the HostSmokeTests smoke test.
**Manual changes:** Requested global.json SDK pinning because .NET 6.0.403 is also installed on my machine – reviewers must get a reproducible .NET 10 build. When Claude pointed out global.json resolves from the working directory upward, I verified with `dotnet --version` from the root and had it moved to the repo root. Logged in the AI-mistakes table.
**Rejected AI suggestions:** None. I accepted two out-of-plan changes after review: removing UseAuthorization (no auth in scope – no dead middleware) and the host smoke test (cheap proof the API starts).
**Assumptions / trade-offs:** .slnx solution format (the .NET 10 default; needs SDK 10 to open, acceptable since global.json pins it). HTTP-only on :5080 with no HTTPS redirect – fine for a local-only exercise, listed as a known limitation.
**Verification:** `dotnet --version` (repo root) → 10.0.400. `dotnet build backend` → Build succeeded, 0 warnings, 0 errors. `dotnet test backend` → 1 passed, 0 failed, 0 skipped (Api.Tests); Core.Tests has no tests yet. Committed as `82e999f`.
