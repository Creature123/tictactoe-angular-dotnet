import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable } from 'rxjs';
import { GameApiService } from './game-api.service';
import { GameMode, GameState, ProblemDetails } from './game.models';

export const FALLBACK_ERROR = 'Something went wrong. Please try again.';

/**
 * Holds the latest GameState returned by the API. It sends intents and stores responses;
 * it never applies a game rule (the backend owns them, FE-06).
 */
@Injectable({ providedIn: 'root' })
export class GameStore {
  private readonly api = inject(GameApiService);

  private readonly _state = signal<GameState | null>(null);
  private readonly _pending = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly state = this._state.asReadonly();
  readonly pending = this._pending.asReadonly();
  readonly error = this._error.asReadonly();

  readonly isGameOver = computed(() => {
    const status = this._state()?.status;
    return status === 'Won' || status === 'Draw';
  });

  /** Straight from the API; the client never computes undo availability. */
  readonly canUndo = computed(() => this._state()?.canUndo ?? false);

  readonly statusMessage = computed(() => {
    const state = this._state();
    if (!state) return '';
    switch (state.status) {
      case 'Won':
        return `${state.winner} wins`;
      case 'Draw':
        return "It's a draw";
      case 'InProgress':
        return `${state.currentPlayer}'s turn`;
    }
  });

  newGame(mode: GameMode): void {
    this.run(
      () => this.api.createGame(mode),
      (game) => this._state.set(game),
    );
  }

  play(row: number, col: number): void {
    const state = this._state();
    // The player is echoed from the API's currentPlayer; the server validates it (A-05).
    if (!state?.currentPlayer) return;
    const player = state.currentPlayer;
    this.run(
      () => this.api.makeMove(state.id, { player, row, col }),
      (game) => this._state.set(game),
    );
  }

  undo(): void {
    const id = this._state()?.id;
    if (!id) return;
    this.run(
      () => this.api.undo(id),
      (game) => this._state.set(game),
    );
  }

  reset(): void {
    const id = this._state()?.id;
    if (!id) return;
    this.run(
      () => this.api.reset(id),
      (game) => this._state.set(game),
    );
  }

  resetScoreboard(): void {
    this.run(
      () => this.api.resetScoreboard(),
      (scoreboard) => this._state.update((s) => (s ? { ...s, scoreboard } : s)),
    );
  }

  clearError(): void {
    this._error.set(null);
  }

  /** Runs one request at a time; actions sent while a request is pending are ignored. */
  private run<T>(request: () => Observable<T>, apply: (result: T) => void): void {
    if (this._pending()) return;
    this._pending.set(true);
    request().subscribe({
      next: (result) => {
        apply(result);
        this._error.set(null);
        this._pending.set(false);
      },
      error: (err: unknown) => {
        this._error.set(toMessage(err));
        this._pending.set(false);
      },
    });
  }
}

function toMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    const problem = err.error as Partial<ProblemDetails> | null;
    if (problem && typeof problem.detail === 'string' && problem.detail) return problem.detail;
  }
  return FALLBACK_ERROR;
}
