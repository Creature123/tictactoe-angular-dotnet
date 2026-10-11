---
paths:
  - "backend/**/*.cs"
  - "backend/**/*.csproj"
  - "backend/**/*.props"
---

# Backend (.NET) rules

## Layering
- `TicTacToe.Core` has zero ASP.NET / HTTP dependencies. Controllers stay thin: bind → call `GameService` → map to DTO.
- Domain types (Game, Board, Move, Scoreboard) never leak to the wire. Map to response DTOs in `TicTacToe.Api`.
- Register services through one `AddTicTacToe()` IServiceCollection extension. Use singletons for the
  in-memory repository and scoreboard, and say so in a comment.

## Domain modelling
- Use enums `Player { X, O }`, `GameMode { TwoPlayer, VsComputer }` and `GameStatus { InProgress, Won, Draw }`.
  Serialize them as strings (`JsonStringEnumConverter`).
- Model expected failures as a domain result type (e.g. `MoveError.CellOccupied`), not exceptions.
  Keep exceptions for programmer errors only.
- Keep the 8 winning lines in one static readonly table. Win detection returns every completed line; `winningCells` is the union of their cells (A-09).
- Seal classes by default, use `record`/`readonly record struct` for values, and keep nullable reference types on with no `!` suppressions.
- No static mutable state. Do not use `async` for purely in-memory, CPU-bound work. Don't fake async.

## Concurrency
- The repository uses a `ConcurrentDictionary<Guid, Game>`. Every mutation of one game (move + computer reply
  + score recording) runs under that game's lock so it is atomic. The scoreboard has its own lock.
- Keep the critical sections short and do no I/O inside them. Do not introduce Channels, actors or
  background services. They are out of scope.

## HTTP contract
- 200 on success with the full game state. Also 201 + Location for `POST /api/games`.
- 400: malformed input (missing field, unknown enum value, wrong type) `INVALID_REQUEST`; row/col outside 0..2 → `OUT_OF_BOUNDS`.
- 404: unknown game id, including an id that is not a Guid → `GAME_NOT_FOUND` (A-14).
- 409: rule conflict (occupied cell, wrong player, game already completed, nothing to undo).
- Every error is ProblemDetails with `title`, `detail` and an `errorCode` extension
  (`CELL_OCCUPIED`, `NOT_YOUR_TURN`, `GAME_COMPLETED`, `NOTHING_TO_UNDO`, `OUT_OF_BOUNDS`, `GAME_NOT_FOUND`, `INVALID_REQUEST`).
  Framework-generated errors get one too via `CustomizeProblemDetails`: 415 → `UNSUPPORTED_MEDIA_TYPE`,
  404 for an unmatched route → `NOT_FOUND`.
- Keep `TicTacToe.Api.http` updated with one runnable sample request per endpoint.
- CORS allows only `http://localhost:4200` in Development.
