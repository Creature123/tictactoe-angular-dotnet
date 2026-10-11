---
paths:
  - "frontend/**/*.ts"
  - "frontend/**/*.html"
  - "frontend/**/*.scss"
  - "frontend/**/*.json"
---

# Frontend (Angular) rules

Angular syntax and API conventions come from the `angular-developer` skill. Do not restate them here.
These are the project-specific decisions:

- A single feature lives in `src/app/game/`. Keep `GamePage` as the only container. `Board`, `GameStatus`,
  `MoveHistory`, `Scoreboard`, `ModeSelector` and `GameControls` are presentational (inputs and outputs only, no services).
- `GameApiService` wraps HttpClient with one method per endpoint. Use relative URLs (`/api/...`).
  The dev proxy (`proxy.conf.json`) forwards to http://localhost:5080. No hard-coded hosts.
- `GameStore` is a signal-based service holding the last `GameState` returned by the API, plus `pending`
  and `error` signals. All derived UI values (status message, button enabled states) are `computed()`.
- Models in `game.models.ts` mirror the API contract in `docs/SPEC.md` field-for-field.
- **No game rules in the frontend.** No win checks, no turn logic, no undo maths. Render the server state.
  `canUndo`, `status`, `winner` and `winningCells` come from the API.
- While a request is in flight, disable the board and the action buttons to prevent double submissions.
- Map ProblemDetails `detail` to a visible, dismissible error message. Never swallow errors silently.
- Cells are `<button>` elements with an `aria-label` (e.g. "Row 1, Column 2, empty"). Winning cells get a `winning` CSS class.
- Display positions 1-based ("Row 1, Column 1"). The API is 0-based.
- Styling: plain SCSS with CSS grid. Laptop-first, usable down to ~1024px wide. No UI component library.
