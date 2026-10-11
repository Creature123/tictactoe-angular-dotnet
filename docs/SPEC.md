# Tic Tac Toe — Specification

> Status: **Approved.** Derived from `docs/requirements/problem-statement.md` (the brief), `CLAUDE.md` and
> `.claude/rules/*.md`. Every ambiguity in §7 is decided; the rationale is in `docs/DECISIONS.md`.
> Brief references: FR§n = Functional Requirements §n, MH§n = Must-Have §n, BE = Backend Requirements,
> FE = Frontend Requirements, C1/C2 = Clarifications, TE = Testing Expectations, AC = Acceptance Criteria.

## 1. Scope
- **In:** 3×3 game, Two Player and Computer modes, win/draw detection, move history, Undo, Reset Game, session scoreboard + Reset Scoreboard.
- **In:** .NET Web API owns every rule and all state (in memory); Angular renders the state the API returns, over REST.
- **In:** backend unit + integration tests, frontend component/API tests, README, AI workflow notes.
- **Out:** auth, users, persistence/DB (SQLite included), SSR, online multiplayer, configurable board size, smarter AI.
- **Out:** deployment, mobile-first layout, i18n, UI component libraries.

## 2. Requirements
Each criterion is written to be tested by a single test. Positions in criteria are API 0-based `(row,col)`; cell index = row*3+col.

### 2.1 Functional (FR)
| ID | Source | Requirement | Acceptance criterion (Given / When / Then) |
|----|--------|-------------|--------------------------------------------|
| FR-01 | FR§1 | Standard 3×3 board | Given a new game, When its state is read, Then `board` has 9 cells, all `null` |
| FR-02 | FR§1 | An empty cell accepts a move | Given InProgress with (0,0) empty and X to move, When X plays (0,0), Then 200 and `board[0]="X"` |
| FR-03 | FR§1 | A played cell shows X or O | Given `board[4]="O"`, When the board renders, Then cell "Row 2, Column 2" shows "O" |
| FR-04 | FR§1 | A played cell stays locked | Given (0,0) is taken, When any player plays (0,0), Then 409 `CELL_OCCUPIED` and the state is unchanged |
| FR-05 | FR§2 | Two players, X moves first | Given a new or reset game, When its state is read, Then `currentPlayer="X"` |
| FR-06 | FR§2 | UI shows whose turn it is | Given InProgress with `currentPlayer="O"`, When the page renders, Then the status reads "O's turn" |
| FR-07 | FR§2 | Turns alternate after each valid move | Given TwoPlayer with X to move, When X plays a valid non-ending move, Then `currentPlayer="O"` |
| FR-08 | FR§2 | An invalid move does not change the turn | Given X to move, When X sends an invalid move (any §5 error), Then `currentPlayer` and `moves` are unchanged |
| FR-09 | FR§3 | Row win | Given X(0,0) O(1,0) X(0,1) O(1,1), When X plays (0,2), Then `status=Won`, `winner=X`, `winningCells=[0,1,2]` |
| FR-10 | FR§3 | Column win | Given X(0,0) O(0,1) X(1,0) O(1,1), When X plays (2,0), Then `status=Won`, `winningCells=[0,3,6]` |
| FR-11 | FR§3 | Diagonal win (both diagonals) | Given X(0,0) O(0,1) X(1,1) O(0,2), When X plays (2,2), Then `winningCells=[0,4,8]`; anti-diagonal gives `[2,4,6]` |
| FR-12 | FR§3 | UI shows winner and highlights winning cells | Given `status=Won, winner=X, winningCells=[0,4,8]`, When rendered, Then "X wins" is shown and exactly those 3 cells have class `winning` |
| FR-13 | FR§3,§4 | No moves after completion | Given `status` Won or Draw, When any move is sent, Then 409 `GAME_COMPLETED` and the state is unchanged |
| FR-14 | FR§3 | A win updates the scoreboard | Given `xWins=n`, When X makes the winning move, Then `xWins=n+1` (same for O / `oWins`) |
| FR-15 | FR§4 | Draw when the board is full with no winner | Given 8 moves and no line, When the 9th move completes no line, Then `status=Draw`, `winner=null`, `winningCells=[]` |
| FR-16 | FR§4 | UI shows a draw message | Given `status=Draw`, When rendered, Then "It's a draw" is shown and every cell is disabled |
| FR-17 | FR§4 | A draw updates the scoreboard | Given `draws=n`, When the drawing move is made, Then `draws=n+1` |
| FR-18 | FR§5, MH§3 | Reset Game: empty board, empty history, status cleared, X to move, fresh game, scoreboard unchanged | Given a game with moves (in progress or completed) and scoreboard S, When reset, Then `moves=[]`, board empty, `status=InProgress`, `currentPlayer=X`, `winner=null`, scoreboard = S |

### 2.2 Must-have (MH)
| ID | Source | Requirement | Acceptance criterion |
|----|--------|-------------|----------------------|
| MH-01 | MH§1 | History shows move number, player and position (1-based in the UI) | Given moves X(0,0), O(1,1), When rendered, Then rows are "1 · X · Row 1, Column 1" and "2 · O · Row 2, Column 2" |
| MH-02 | MH§1 | History updates after every valid move, and only then | Given n moves, When a valid move is made, Then `moves` has n+1 entries and the last one is that move; after an invalid move it still has n |
| MH-03 | MH§2 | Undo in Two Player mode removes 1 move | Given TwoPlayer X(0,0) O(1,1), When undo, Then `moves=[X(0,0)]`, `board[4]=null`, `currentPlayer=O`, `status=InProgress` |
| MH-04 | MH§2 | Undo in Computer mode removes the computer move and the human move before it | Given VsComputer X(0,0) + computer O(1,1), When undo, Then `moves=[]`, board empty, `currentPlayer=X` |
| MH-05 | MH§2 | Undo restores board, turn, status and history by re-deriving them from the remaining moves | Given any undoable state, When undo, Then the state equals a fresh game replayed with the remaining moves |
| MH-06 | MH§2 | Undo is disabled when there is nothing to undo | Given `moves=[]`, Then `canUndo=false` and the Undo button is disabled; When undo is sent anyway, Then 409 `NOTHING_TO_UNDO` |
| MH-07 | C2 (Option A) | Undo is disabled after completion | Given Won or Draw, Then `canUndo=false`; When undo is sent, Then 409 `GAME_COMPLETED` (see A-12) and the scoreboard is unchanged |
| MH-08 | MH§3 | Backend serves a session scoreboard with X wins, O wins and draws | Given a fresh API process, When `GET /api/scoreboard`, Then `{xWins:0, oWins:0, draws:0}` |
| MH-09 | MH§3, C2 | A completed game counts exactly once | Given a game just won (counted), When more moves, undos or GETs are sent, Then the scoreboard is unchanged |
| MH-10 | MH§3 | Reset Scoreboard sets every count to zero | Given non-zero counts, When `POST /api/scoreboard/reset`, Then all counts are 0 and the current game is unchanged |
| MH-11 | MH§4 | Two modes: Two Player and Play Against Computer | Given `POST /api/games {mode:"VsComputer"}`, Then `mode="VsComputer"` (same for `"TwoPlayer"`) |
| MH-12 | MH§4 | Two Player: users make both X and O moves | Given TwoPlayer with X to move, When X plays, Then exactly 1 move is added and no automatic reply is made |
| MH-13 | MH§4 | Computer mode: human is X, computer is O and replies in the same request | Given VsComputer with X to move, When X plays a valid non-ending move, Then the response has X's move followed by O's move, and `currentPlayer=X` |
| MH-14 | MH§4 | The computer makes only valid moves | Given any InProgress board with O to move, When the computer picks, Then the cell is in 0..8 and was empty |
| MH-15 | MH§4 | The computer does not move after the game ends | Given VsComputer, When X's move wins or fills the board, Then no O move is added and `status` is Won/Draw |
| MH-16 | MH§4 | Priority 1: O wins if it can | Given board §6-E1, When the computer moves, Then it plays index 5 (it wins, even though X also threatens) |
| MH-17 | MH§4 | Priority 2: block X | Given board §6-E2, When the computer moves, Then it plays index 2 |
| MH-18 | MH§4 | Priority 3: center | Given board §6-E3, When the computer moves, Then it plays index 4 |
| MH-19 | MH§4 | Priority 4: corner | Given board §6-E4, When the computer moves, Then it plays index 2 |
| MH-20 | MH§4 | Priority 5: any free cell | Given board §6-E5, When the computer moves, Then it plays index 3 |
| MH-21 | MH§4 | The tie-break is deterministic (§6) | Given boards §6-T1 / §6-T2, When the computer moves, Then it plays index 5 / 6 |

### 2.3 Backend (BE)
| ID | Source | Requirement | Acceptance criterion |
|----|--------|-------------|----------------------|
| BE-01 | BE, Tech | REST endpoints as listed in §5 | Given the API is running, When each §5 endpoint is called with valid input, Then it returns the documented success code and body |
| BE-02 | BE, C1 | The backend owns the game state, history, status and scoreboard | Given a move was made, When `GET /api/games/{id}` is sent with no client state, Then the response includes that move |
| BE-03 | BE (Game State) | The game response has id, board, current player, mode, status, winner, winning cells, moves, `canUndo` and the scoreboard (A-07) | Given any game, When any game endpoint returns 200, Then the body has every field in §5.1 |
| BE-04 | BE (Move Request) | The move request carries the game id (route), player, row and column | Given a body without `player`, `row` or `col`, When posted, Then 400 `INVALID_REQUEST` (A-11) |
| BE-05 | BE (Move Request) | Reject a move outside the board | Given row or col outside 0..2, When posted, Then 400 `OUT_OF_BOUNDS` |
| BE-06 | BE (Move Request) | Reject a move on an occupied cell | Same as FR-04: 409 `CELL_OCCUPIED` |
| BE-07 | BE (Move Request) | Reject a move after completion | Same as FR-13: 409 `GAME_COMPLETED` |
| BE-08 | BE (Move Request) | Reject a move by the wrong player | Given X to move, When `player="O"` is posted, Then 409 `NOT_YOUR_TURN` |
| BE-09 | BE, rules | An unknown game id returns 404 | Given an id that does not exist, When any `/api/games/{id}…` endpoint is called, Then 404 `GAME_NOT_FOUND` |
| BE-10 | rules | Every error is RFC 7807 ProblemDetails with an `errorCode` | Given any 4xx, Then the body has `title`, `status`, `detail` and `errorCode` |
| BE-11 | BE (statuses) | Status values are `InProgress`, `Won`, `Draw`, sent as strings | Given any game, Then `status` is one of those three strings |

### 2.4 Frontend (FE)
| ID | Source | Requirement | Acceptance criterion |
|----|--------|-------------|----------------------|
| FE-01 | FE, FR§1 | Board of 9 cell buttons | Given a state, When rendered, Then 9 buttons; empty cells are enabled only while InProgress and no request is pending |
| FE-02 | FE | Shows the current player, the selected mode and a winner or draw message | Given `mode=VsComputer, status=InProgress, currentPlayer=X`, When rendered, Then "Play Against Computer" and "X's turn" are shown |
| FE-03 | FE | Highlights the winning cells | Covered by FR-12 |
| FE-04 | FE, MH§1, MH§3 | Shows the move history and the scoreboard from the API response | Given a response, When rendered, Then the history table and the scoreboard match it exactly |
| FE-05 | FE | Reset Game, Undo Last Move and Reset Scoreboard buttons call the API | When each button is clicked, Then exactly one matching HTTP request is sent (HttpTestingController) |
| FE-06 | FE, C1 | Renders only the backend state; no game rules on the client | Given any API response, Then the UI state comes from it; Undo is enabled iff `canUndo && !pending` |
| FE-07 | FE, AC | Invalid actions show the ProblemDetails message | Given a 409 response, When it arrives, Then its `detail` is shown in a dismissible message and the board is unchanged |
| FE-08 | MH§4 | The user can pick the mode | Given the mode selector, When the user picks a mode, Then `POST /api/games {mode}` is sent and the new game is rendered; selecting the mode that is already active sends no request (A-04) |
| FE-09 | FE | Comfortable on a laptop browser | Given a 1024–1920 px wide viewport, Then nothing overflows horizontally and every control is visible without zooming |

### 2.5 Non-functional and deliverables (NFR)
| ID | Source | Requirement | Acceptance criterion |
|----|--------|-------------|----------------------|
| NFR-01 | Tech | Angular + TypeScript, .NET Web API, REST, in-memory storage, GitHub | Given the repo, Then `frontend/` is Angular, `backend/` is ASP.NET Core and there is no DB dependency |
| NFR-02 | Intro, AC | Runs locally and is easy to review; the frontend talks to the API over REST | Given a clean clone, When the README commands are run, Then the API serves :5080, the UI serves :4200 and a game can be played |
| NFR-03 | TE | Tests: backend unit tests for rules and transitions (all 13 cases in §8), integration tests, frontend rendering/API tests | When `dotnet test backend` and `npm test -- --watch=false` are run, Then all tests pass and every §8 row has a test |
| NFR-04 | README, C2 | The README has the 12 required sections and states the Option A choice | Given README.md, Then all 12 headings exist and the Undo/scoreboard choice is stated |
| NFR-05 | Submission, AI | Submission contents, API docs, assumptions/limitations, AI workflow notes | Given the repo, Then README + `docs/SPEC.md` (API) + `docs/DECISIONS.md` + `docs/AI_WORKFLOW.md` exist and are current |

## 3. Domain model
| Type | Fields | Notes |
|------|--------|-------|
| `Player` | enum `X`, `O` | serialized as a string |
| `GameMode` | enum `TwoPlayer`, `VsComputer` | serialized as a string |
| `GameStatus` | enum `InProgress`, `Won`, `Draw` | serialized as a string |
| `Position` | `Row`, `Col` (0..2) | `Index = Row*3 + Col` |
| `Move` | `Number` (1-based), `Player`, `Position` | value object |
| `Game` | `Id` (Guid), `Mode`, `Moves` (ordered list) | **the only stored game state** |
| `Scoreboard` | `XWins`, `OWins`, `Draws` | stored; one per API process (A-01) |

| Derived (never stored) | Rule |
|------------------------|------|
| `Board` | 9 cells; cell `m.Position.Index` = `m.Player` for every move m |
| `CurrentPlayer` | `X` if `Moves.Count` is even, otherwise `O` (CLAUDE.md); sent as `null` when not InProgress (A-08) |
| `WinningCells` | union of the cells of every completed line, ascending index (A-09); `[]` if none |
| `Status` | `Won` if `WinningCells` is not empty; else `Draw` if `Moves.Count == 9`; else `InProgress` (a win beats a draw) |
| `Winner` | the player of the last move if `Won`, otherwise `null` |
| `CanUndo` | `Status == InProgress && Moves.Count > 0` (Option A) |

| # | Invariant |
|---|-----------|
| I-1 | Positions in `Moves` are unique, and every position is in 0..2 |
| I-2 | `Moves[i].Player` = X for even i, O for odd i; `Moves[i].Number = i+1` |
| I-3 | No move follows the move that ends the game (Won/Draw is terminal until Reset) |
| I-4 | VsComputer and InProgress ⇒ `Moves.Count` is even (each human move is answered atomically, and undo removes pairs) |
| I-5 | `Winner != null` ⇔ `Status == Won` ⇔ `WinningCells` is not empty |
| I-6 | The scoreboard changes only on an InProgress → Won/Draw transition (by 1) and on Reset Scoreboard (to 0) |
| I-7 | One game's mutation (move + computer reply + score update) is atomic under that game's lock (rules) |

## 4. State transitions
"Any" means both modes. Every action on an unknown id → 404 `GAME_NOT_FOUND`. Errors leave state and scoreboard unchanged.

| # | Status | Mode | Action / condition | Result | Error | Scoreboard |
|---|--------|------|--------------------|--------|-------|------------|
| T-01 | InProgress | TwoPlayer | Move, valid, game not ended | move appended; turn flips | — | — |
| T-02 | InProgress | TwoPlayer | Move, valid, completes a line | Won, winner = mover | — | +1 winner |
| T-03 | InProgress | TwoPlayer | Move, valid, 9th move, no line | Draw | — | +1 draws |
| T-04 | InProgress | VsComputer | X move, valid, game not ended | X appended, then computer reply (T-05/T-06) | — | — |
| T-05 | InProgress | VsComputer | computer reply, game not ended | O appended; X to move | — | — |
| T-06 | InProgress | VsComputer | computer reply completes a line | Won, winner O | — | +1 oWins |
| T-07 | InProgress | VsComputer | X move wins | Won, winner X; **no reply** | — | +1 xWins |
| T-08 | InProgress | VsComputer | X move is the 9th, no line | Draw; **no reply** (O can never make move 9) | — | +1 draws |
| T-09 | any | any | Move with malformed body / unknown player | unchanged | 400 `INVALID_REQUEST` | — |
| T-10 | any | any | Move with row/col outside 0..2 | unchanged | 400 `OUT_OF_BOUNDS` | — |
| T-11 | Won / Draw | any | Move (any) | unchanged | 409 `GAME_COMPLETED` | — |
| T-12 | InProgress | any | Move with `player ≠ currentPlayer` (VsComputer: any `O`) | unchanged | 409 `NOT_YOUR_TURN` | — |
| T-13 | InProgress | any | Move on an occupied cell | unchanged | 409 `CELL_OCCUPIED` | — |
| T-14 | InProgress | TwoPlayer | Undo, ≥1 move | last 1 move removed; re-derive | — | — |
| T-15 | InProgress | VsComputer | Undo, ≥2 moves (always even, I-4) | last 2 moves removed; re-derive; X to move | — | — |
| T-16 | InProgress | any | Undo, 0 moves | unchanged | 409 `NOTHING_TO_UNDO` | — |
| T-17 | Won / Draw | any | Undo | unchanged (Option A) | 409 `GAME_COMPLETED` | — |
| T-18 | any | any | Reset | `Moves=[]` → InProgress, X to move; same id and mode (A-02, A-03) | — | unchanged |
| T-19 | any | any | Reset Scoreboard | games unchanged | — | all = 0 |

Errors are checked in this order (A-10): 404 → 400 `INVALID_REQUEST` → 400 `OUT_OF_BOUNDS` → `GAME_COMPLETED` → `NOT_YOUR_TURN` → `CELL_OCCUPIED`.

## 5. API contract
Base path `/api`. JSON uses camelCase, and enums are strings. Positions are 0-based. Success = 200 with a body, except `POST /api/games` = 201 + `Location`.

### 5.1 Shared shapes
`GameState`: VsComputer, after X played (0,0) and the computer replied at the center:
```json
{
  "id": "3f2b8c1e-6a4d-4b7e-9c11-2d5e8f0a7b93",
  "mode": "VsComputer",
  "status": "InProgress",
  "currentPlayer": "X",
  "winner": null,
  "winningCells": [],
  "board": ["X", null, null, null, "O", null, null, null, null],
  "moves": [
    { "number": 1, "player": "X", "row": 0, "col": 0 },
    { "number": 2, "player": "O", "row": 1, "col": 1 }
  ],
  "canUndo": true,
  "scoreboard": { "xWins": 0, "oWins": 0, "draws": 0 }
}
```
`board` is a flat array of 9 (index = row*3+col; A-15). `winningCells` holds cell indices. `scoreboard` is embedded per A-07.

`ProblemDetails` (every error):
```json
{ "type": "about:blank", "title": "Cell occupied", "status": 409,
  "detail": "Row 1, Column 1 is already taken.", "errorCode": "CELL_OCCUPIED" }
```
`detail` is written for the user to read (1-based positions), because the UI shows it as-is (FE-07).

### 5.2 Endpoints
| # | Method + path | Request body | Success | Errors (status · errorCode) |
|---|---------------|--------------|---------|-----------------------------|
| E1 | `POST /api/games` | `{ "mode": "TwoPlayer" }` | 201, `Location: /api/games/{id}`, `GameState` (empty board) | 400 · `INVALID_REQUEST` (mode missing/unknown, A-06) |
| E2 | `GET /api/games/{id}` | — | 200 `GameState` | 404 · `GAME_NOT_FOUND` (also for an id that is not a Guid, A-14) |
| E3 | `POST /api/games/{id}/moves` | `{ "player": "X", "row": 0, "col": 2 }` | 200 `GameState` (VsComputer: includes the computer reply) | 400 · `INVALID_REQUEST`; 400 · `OUT_OF_BOUNDS`; 404 · `GAME_NOT_FOUND`; 409 · `GAME_COMPLETED`; 409 · `NOT_YOUR_TURN`; 409 · `CELL_OCCUPIED` |
| E4 | `POST /api/games/{id}/undo` | — | 200 `GameState` | 404 · `GAME_NOT_FOUND`; 409 · `NOTHING_TO_UNDO`; 409 · `GAME_COMPLETED` |
| E5 | `POST /api/games/{id}/reset` | — | 200 `GameState` (same id and mode, empty board) | 404 · `GAME_NOT_FOUND` |
| E6 | `GET /api/scoreboard` | — | 200 `{ "xWins": 2, "oWins": 1, "draws": 0 }` | — |
| E7 | `POST /api/scoreboard/reset` | — | 200 `{ "xWins": 0, "oWins": 0, "draws": 0 }` | — |

Framework-level errors are written by ASP.NET Core before any controller runs. They still get an `errorCode` (BE-10):

| Status · errorCode | When |
|--------------------|------|
| 415 · `UNSUPPORTED_MEDIA_TYPE` | A request body sent with a Content-Type other than JSON (for example `text/plain` on E3) |
| 404 · `NOT_FOUND` | A path that matches no endpoint (unmatched route) |

Known limitation: a 415 is returned before the A-10 check for an unknown game, so a non-JSON body sent to an unknown game id returns 415 instead of 404 `GAME_NOT_FOUND`.

Example E3 win response (TwoPlayer, FR-09): `"status": "Won", "winner": "X", "winningCells": [0,1,2], "currentPlayer": null, "canUndo": false`.

## 6. Computer strategy
A pure function of the board, called only when `Status == InProgress` and O is to move (MH-15). Exact order (CLAUDE.md):

| Step | Rule | Candidates | Tie-break |
|------|------|------------|-----------|
| 1 | Win | empty cells that complete a line with two O | lowest cell index |
| 2 | Block | empty cells that complete a line with two X | lowest cell index |
| 3 | Center | index 4 if empty | — |
| 4 | Corner | 0, 2, 6, 8 | first empty in that order |
| 5 | Any | every empty cell | lowest cell index |

Example boards (moves in play order, `row,col`; board shown as indices 0–8, `.` = empty, O to move). Each board is decided by one rule only:

| Ex | Moves | Board | Expected | Proves |
|----|-------|-------|----------|--------|
| E1 | X0,0 O1,0 X0,1 O1,1 X2,2 | `XX.OO...X` | 5 | win beats block (X threatens 2) |
| E2 | X0,0 O1,0 X0,1 | `XX.O.....` | 2 | block |
| E3 | X0,0 | `X........` | 4 | center |
| E4 | X1,1 O0,0 X2,2 | `O...X...X` | 2 | corner order (0 taken → 2; "any free cell" would give 1) |
| E5 | X0,0 O0,1 X0,2 O2,0 X1,1 O2,2 X2,1 | `XOX.X.OXO` | 3 | any free cell (lowest) |
| T1 | X0,0 O0,1 X0,2 O1,0 X2,0 O1,1 X2,2 | `XOXOO.X.X` | 5 | two winning cells {5,7} → lowest |
| T2 | X0,0 O0,1 X0,2 O1,0 X1,1 | `XOXOX....` | 6 | two blocking cells {6,8} → lowest |

## 7. Ambiguity log
### 7.1 Already decided (cited, not reopened)
| Topic | Decision | Source |
|-------|----------|--------|
| Undo after completion (C2) | Option A: disabled; the scoreboard is final | CLAUDE.md |
| "Recalculate win or draw status" on undo | Always re-derived; under Option A the result is always InProgress | CLAUDE.md (Option A + derived state) |
| When the computer moves | Inside the same `POST /moves`, atomically; never after completion | CLAUDE.md |
| Corner order / "any cell" | 0, 2, 6, 8 / first free cell | CLAUDE.md |
| Position base | API 0-based, UI 1-based | CLAUDE.md |
| Status codes, errorCodes, ProblemDetails | 400 / 404 / 409 as in §5 | `.claude/rules/backend-dotnet.md` |
| Storage, concurrency | In-memory `ConcurrentDictionary`, per-game lock, no DB | CLAUDE.md, rules |
| When the scoreboard updates | Once, on InProgress → Won/Draw; Reset Game never touches it | CLAUDE.md |
| A 9th move that also completes a line | Won, not Draw ("all 9 cells filled **and no winner**") | brief FR§4 |

### 7.2 Resolved ambiguities (decided by the owner)
Full context, options and consequences for each item: `docs/DECISIONS.md`.

| ID | Question | Decision | Consequence | Status |
|----|----------|----------|-------------|--------|
| A-01 | Scope of the "session-level scoreboard" | One scoreboard per API process | Every tab shares it; an API restart clears it (known limitation) | DECIDED – see DECISIONS.md |
| A-02 | Reset: same game or new game? | Same id, moves cleared | The client's id/URL stays stable; "fresh session" = fresh round | DECIDED – see DECISIONS.md |
| A-03 | Mode after Reset | Mode is kept | A reset computer game stays a computer game | DECIDED – see DECISIONS.md |
| A-04 | Changing mode mid-game | Only by creating a new game (E1) | The abandoned game is not scored; no extra endpoint | DECIDED – see DECISIONS.md |
| A-05 | `player` in the move request | Required and validated in both modes | A stale or double click gets `NOT_YOUR_TURN` | DECIDED – see DECISIONS.md |
| A-06 | Body of `POST /games` | `mode` is required | No hidden default; a missing mode → 400 `INVALID_REQUEST` | DECIDED – see DECISIONS.md |
| A-07 | Where the scoreboard is served | Embedded in `GameState`, and E6 is kept | One round trip after a game-ending move | DECIDED – see DECISIONS.md |
| A-08 | `currentPlayer` once the game is complete | `null` on the wire (the domain still derives by parity) | The UI never shows a turn on a finished board | DECIDED – see DECISIONS.md |
| A-09 | One move completes two lines | `winningCells` = union of every completed line, ascending | Every winning cell is highlighted | DECIDED – see DECISIONS.md |
| A-10 | Several violations at once | Fixed precedence as in §4 | One deterministic errorCode per request | DECIDED – see DECISIONS.md |
| A-11 | Malformed input | 400 `INVALID_REQUEST` | Every error carries an `errorCode` | DECIDED – see DECISIONS.md |
| A-12 | Undo on a completed game | 409 `GAME_COMPLETED` | The client can tell "game over" from "nothing to undo" | DECIDED – see DECISIONS.md |
| A-13 | How long games stay in memory | Until the process restarts | Unbounded growth (known limitation); future: TTL or a persistent store | DECIDED – see DECISIONS.md |
| A-14 | Game id format | Guid; a non-Guid id → 404 `GAME_NOT_FOUND` | Ids are not guessable | DECIDED – see DECISIONS.md |
| A-15 | Board wire format | Flat 9-cell array; `winningCells` as cell indices | Matches the CLAUDE.md cell index | DECIDED – see DECISIONS.md |
| A-16 | Page load and refresh | A new TwoPlayer game on every load | A refresh drops the game; the scoreboard survives | DECIDED – see DECISIONS.md |
| A-17 | Timing of the computer reply in the UI | Shown as soon as the response arrives | Both moves appear together | DECIDED – see DECISIONS.md |

## 8. Test traceability
### 8.1 The 13 mandatory test cases (brief TE)
| # | Test case | Requirement IDs | Layer |
|---|-----------|-----------------|-------|
| 1 | Valid move | FR-02, FR-07, MH-02, BE-01 | Core unit + API integration |
| 2 | Invalid move | FR-04, FR-08, BE-04, BE-05, BE-06, BE-08, BE-10 | Core unit + API (one per errorCode) |
| 3 | Turn switching | FR-05, FR-07, FR-08 | Core unit |
| 4 | Row win | FR-09 | Core unit |
| 5 | Column win | FR-10 | Core unit |
| 6 | Diagonal win | FR-11 (both diagonals) | Core unit |
| 7 | Draw | FR-15 (+ a win on move 9 is Won) | Core unit |
| 8 | Reset game | FR-05, FR-18 | Core unit + API |
| 9 | Undo in two-player mode | MH-03, MH-05, MH-06 | Core unit + API |
| 10 | Undo in computer mode | MH-04, MH-05, MH-07 | Core unit + API |
| 11 | Scoreboard update | FR-14, FR-17, MH-09, MH-10 | Core unit + API |
| 12 | Computer move selection | MH-13, MH-14, MH-16 … MH-21 | Core unit (one per §6 example) |
| 13 | Move after game completion | FR-13, BE-07, MH-15 | Core unit + API |

Frontend (Vitest): FR-03, FR-06, FR-12, FR-16, MH-01, FE-01 … FE-08 (rendering + HttpTestingController).

### 8.2 Brief acceptance criteria (AC)
| AC | Requirement IDs | Verified by |
|----|-----------------|-------------|
| Angular app runs locally | NFR-01, NFR-02 | manual + README |
| .NET API runs locally | NFR-01, NFR-02 | manual + README |
| Frontend talks to the backend over REST | BE-01, FE-05, FE-06 | FE API tests + manual |
| A new game can be created | FR-01, MH-11, BE-01 | API test |
| Two Player Mode works | MH-11, MH-12, FR-07 | Core + API tests |
| Computer Mode works | MH-13 … MH-21 | Core + API tests |
| Turns alternate | FR-05, FR-07, FR-08 | Core test |
| Invalid moves are handled | FR-04, FR-08, BE-04 … BE-10, FE-07 | Core + API + FE tests |
| Win detection | FR-09 … FR-11 | Core tests |
| Draw detection | FR-15, FR-16 | Core + FE tests |
| Winning cells are highlighted | FR-12, FE-03 | FE test |
| Move history is shown | MH-01, MH-02, FE-04 | Core + FE tests |
| Undo works per mode | MH-03 … MH-07 | Core + API tests |
| Scoreboard works | FR-14, FR-17, MH-08, MH-09 | Core + API tests |
| Reset Game works | FR-18 | Core + API tests |
| Reset Scoreboard works | MH-10 | API test |
| Basic tests are included | NFR-03 | CI-style `dotnet test` / `npm test` run |
| README explains run and review | NFR-04, NFR-05 | `spec-verifier` + manual |
| Candidate can explain the implementation | NFR-05 (DECISIONS.md, AI_WORKFLOW.md) | panel review |
