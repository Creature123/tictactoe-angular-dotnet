// Mirrors the API contract in docs/SPEC.md §5. Positions are 0-based; board index = row * 3 + col.

export type Player = 'X' | 'O';
export type GameMode = 'TwoPlayer' | 'VsComputer';
export type GameStatus = 'InProgress' | 'Won' | 'Draw';

export interface MoveEntry {
  number: number;
  player: Player;
  row: number;
  col: number;
}

export interface Scoreboard {
  xWins: number;
  oWins: number;
  draws: number;
}

export interface GameState {
  id: string;
  mode: GameMode;
  status: GameStatus;
  /** `null` once the game is complete (A-08). */
  currentPlayer: Player | null;
  winner: Player | null;
  winningCells: number[];
  board: (Player | null)[];
  moves: MoveEntry[];
  canUndo: boolean;
  scoreboard: Scoreboard;
}

export interface CreateGameRequest {
  mode: GameMode;
}

export interface MoveRequest {
  player: Player;
  row: number;
  col: number;
}

/** RFC 7807 error body; `detail` is user-readable and shown as-is (FE-07). */
export interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail: string;
  errorCode: string;
}
