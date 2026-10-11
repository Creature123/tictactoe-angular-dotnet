# Design Decisions (ADR-lite)

Each entry resolves an ambiguity in `docs/requirements/problem-statement.md` (the brief). The IDs match
`docs/SPEC.md` §7.2. All entries are **Accepted** (decided by the project owner, 2026-10-11).
The `.claude/rules` files and CLAUDE.md fix the HTTP status codes, error codes and architecture invariants, so they are not repeated here.

---

## ADR-00 — Undo after completion: Option A (disabled)
- **Context:** Brief Clarification 2 asks for Option A or Option B, with the choice stated in the README. `CLAUDE.md` refers to this entry.
- **Options considered:** (A) Undo disabled after Won/Draw; the scoreboard stays final. (B) Undo allowed after completion; the scoreboard is reversed when the result changes.
- **Decision:** Option A.
- **Consequences:** The scoreboard changes only on InProgress → Won/Draw and never needs reversing. `canUndo=false` on a finished game; undo there returns 409 `GAME_COMPLETED` (A-12). After an undo the derived status is always InProgress. The README must state this choice.

## A-01 — Scoreboard scope
- **Context:** The brief asks for a "session-level scoreboard" served by the backend, but never defines "session".
- **Options considered:** (a) One global scoreboard per API process. (b) One scoreboard per browser client, keyed by a cookie or header id.
- **Decision:** (a). The brief targets a local, single-user app.
- **Consequences:** No client identity or cookies. Every tab shares one scoreboard. Restarting the API clears it (known limitation).

## A-02 — Reset keeps the same game id
- **Context:** Reset should "start a fresh game session", but the suggested endpoint is `POST /api/games/{id}/reset`.
- **Options considered:** (a) Same id, moves cleared. (b) Create a new game and return a new id.
- **Decision:** (a). "Fresh session" means a fresh round.
- **Consequences:** The client's id and URL stay stable. Reset is `Moves.Clear()`, and every other field is re-derived from the empty list. The scoreboard is untouched.

## A-03 — Mode is kept on Reset
- **Context:** The brief does not say what happens to the game mode on Reset.
- **Options considered:** (a) Keep the current mode. (b) Go back to Two Player.
- **Decision:** (a).
- **Consequences:** A reset computer game stays a computer game. To change mode, the user starts a new game (A-04).

## A-04 — Changing mode means creating a new game
- **Context:** The brief needs two modes but says nothing about switching between them during a game.
- **Options considered:** (a) Switch only by creating a new game (`POST /api/games`). (b) Add an endpoint that changes the mode of an existing game.
- **Decision:** (a).
- **Consequences:** No extra endpoint, and no rules for changing the mode of a game in progress. The abandoned game is not scored. The UI mode selector calls E1.

## A-05 — `player` is required and validated
- **Context:** The brief lists `player` as part of the move request, but in Computer mode the human is always X.
- **Options considered:** (a) Required and validated in both modes. (b) Optional in Computer mode.
- **Decision:** (a).
- **Consequences:** A stale or double click is rejected with 409 `NOT_YOUR_TURN` instead of becoming a silent wrong move. In Computer mode `player:"O"` is always `NOT_YOUR_TURN`.

## A-06 — `mode` is required when creating a game
- **Context:** The brief does not define the body of `POST /api/games`.
- **Options considered:** (a) `mode` is required. (b) `mode` is optional and defaults to TwoPlayer.
- **Decision:** (a).
- **Consequences:** The contract is explicit, with no hidden default. A missing or unknown mode returns 400 `INVALID_REQUEST`.

## A-07 — Scoreboard embedded in the game state
- **Context:** The brief says the game state should include "Scoreboard or a way to retrieve scoreboard".
- **Options considered:** (a) Embed the scoreboard in `GameState` and keep `GET /api/scoreboard`. (b) Serve it only from `GET /api/scoreboard`.
- **Decision:** (a).
- **Consequences:** One round trip after a game-ending move. The scoreboard data is duplicated in two responses, and both come from the same source. E6 and E7 stay for Reset Scoreboard and for reading the scoreboard on its own.

## A-08 — `currentPlayer` is `null` once the game is complete
- **Context:** The current player is derived from the parity of the move count (`CLAUDE.md`), which would still give X or O after a win or a draw.
- **Options considered:** (a) Send `null` when the status is not InProgress. (b) Always send the parity value.
- **Decision:** (a).
- **Consequences:** The UI never shows a turn on a finished board. The domain still derives the player by parity; only the response mapping sends `null`. The frontend model types it as `Player | null`.

## A-09 — `winningCells` is the union of every completed line
- **Context:** On move 9, one move can complete two lines at once (e.g. a row and a column through the same cell).
- **Options considered:** (a) The union of the cells of every completed line, in ascending order. (b) Only the first line in table order.
- **Decision:** (a).
- **Consequences:** Every winning cell is highlighted. Win detection returns every completed line, not only the first. This needs a test where one move completes two lines.

## A-10 — Fixed error precedence
- **Context:** One request can break several rules at once (e.g. a move on an occupied cell of a finished game).
- **Options considered:** (a) A fixed check order that returns the first violation. (b) Report every violation.
- **Decision:** (a), with the order in SPEC §4: 404 → 400 `INVALID_REQUEST` → 400 `OUT_OF_BOUNDS` → `GAME_COMPLETED` → `NOT_YOUR_TURN` → `CELL_OCCUPIED`.
- **Consequences:** Each request gets one deterministic error code, and each step can be tested.

## A-11 — 400 `INVALID_REQUEST` for malformed input
- **Context:** Missing fields, an unknown enum value (`"player":"Z"`) or wrong JSON types are not covered by the error codes in the rules file.
- **Options considered:** (a) A ProblemDetails 400 with `errorCode: "INVALID_REQUEST"`. (b) The framework's default validation 400, which has no `errorCode`.
- **Decision:** (a).
- **Consequences:** Every error carries an `errorCode`, so the frontend handles every error the same way. The API has to customise the model-binding and JSON-error responses. The new code needs a line in `.claude/rules/backend-dotnet.md`.

## A-12 — `GAME_COMPLETED` for undo on a finished game
- **Context:** Under Option A (ADR-00), undo is rejected after a win or a draw. The brief does not give an error for it.
- **Options considered:** (a) 409 `GAME_COMPLETED`. (b) 409 `NOTHING_TO_UNDO`.
- **Decision:** (a).
- **Consequences:** The client can tell "game over" from "nothing to undo", and the same code is used for moves after completion.

## A-13 — Games live until the process restarts
- **Context:** In-memory storage is allowed; the brief says nothing about how long games are kept.
- **Options considered:** (a) Keep every game until the process restarts. (b) Remove games after a TTL, or cap how many are kept.
- **Decision:** (a).
- **Consequences:** Simple, with no background work. Memory grows with the number of games (known limitation). Future improvement: TTL eviction or a persistent store.

## A-14 — Guid game ids; a non-Guid id returns 404
- **Context:** The brief does not define the id format.
- **Options considered:** (a) Guid, with a route constraint; a malformed id returns 404 `GAME_NOT_FOUND`. (b) Sequential integers.
- **Decision:** (a).
- **Consequences:** Ids cannot be guessed and are never reused. A non-Guid id gets the same 404 as an unknown id.

## A-15 — Board sent as a flat 9-cell array
- **Context:** The brief asks for the "Board state" without a format.
- **Options considered:** (a) A flat array of 9 (`index = row*3+col`), with `winningCells` as cell indices. (b) A 3×3 nested array, with winning cells as `{row,col}` pairs.
- **Decision:** (a).
- **Consequences:** Matches the cell-index convention in `CLAUDE.md`. The UI turns the index into 1-based labels ("Row 1, Column 1"). Moves still carry `row` and `col`.

## A-16 — New Two Player game on every page load
- **Context:** The brief does not say what the UI shows on first load or after a refresh.
- **Options considered:** (a) Create a new TwoPlayer game on every load. (b) Store the game id in localStorage and restore it.
- **Decision:** (a).
- **Consequences:** Simplest, with no client persistence. A refresh drops the current game. The scoreboard survives on the server.

## A-17 — Show the computer reply immediately
- **Context:** The brief says the computer should "make a move automatically after the human move", but says nothing about timing in the UI.
- **Options considered:** (a) Render the reply as soon as the response arrives. (b) Add an artificial delay or animation.
- **Decision:** (a). Both moves come back in one response (atomic `POST /moves`, `CLAUDE.md`).
- **Consequences:** No timers in the UI and nothing extra to test. Both moves appear together.
