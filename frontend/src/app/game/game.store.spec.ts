import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameState, ProblemDetails } from './game.models';
import { FALLBACK_ERROR, GameStore } from './game.store';

const ID = '3f2b8c1e-6a4d-4b7e-9c11-2d5e8f0a7b93';

function gameState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: ID,
    mode: 'TwoPlayer',
    status: 'InProgress',
    currentPlayer: 'X',
    winner: null,
    winningCells: [],
    board: Array(9).fill(null),
    moves: [],
    canUndo: false,
    scoreboard: { xWins: 0, oWins: 0, draws: 0 },
    ...overrides,
  };
}

const cellOccupied: ProblemDetails = {
  type: 'about:blank',
  title: 'Cell occupied',
  status: 409,
  detail: 'Row 1, Column 1 is already taken.',
  errorCode: 'CELL_OCCUPIED',
};

describe('GameStore', () => {
  let store: GameStore;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    store = TestBed.inject(GameStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Puts the store into a known state through the public API. */
  function startWith(state: GameState): void {
    store.newGame(state.mode);
    http.expectOne('/api/games').flush(state);
  }

  describe('success', () => {
    it('should store the created game and clear pending on newGame', () => {
      store.newGame('VsComputer');
      expect(store.pending()).toBe(true);

      const created = gameState({ mode: 'VsComputer' });
      http.expectOne('/api/games').flush(created);

      expect(store.state()).toEqual(created);
      expect(store.pending()).toBe(false);
    });

    it('should send the API currentPlayer with row/col on play and store the response', () => {
      startWith(
        gameState({ currentPlayer: 'O', moves: [{ number: 1, player: 'X', row: 0, col: 0 }] }),
      );

      store.play(1, 2);
      const req = http.expectOne(`/api/games/${ID}/moves`);
      expect(req.request.body).toEqual({ player: 'O', row: 1, col: 2 });

      const after = gameState({ currentPlayer: 'X', canUndo: true });
      req.flush(after);
      expect(store.state()).toEqual(after);
    });

    it('should replace the state with the response on undo', () => {
      startWith(gameState({ canUndo: true }));

      store.undo();
      const undone = gameState({ currentPlayer: 'O' });
      http.expectOne(`/api/games/${ID}/undo`).flush(undone);

      expect(store.state()).toEqual(undone);
    });

    it('should replace the state with the response on reset', () => {
      startWith(gameState({ canUndo: true }));

      store.reset();
      const fresh = gameState();
      http.expectOne(`/api/games/${ID}/reset`).flush(fresh);

      expect(store.state()).toEqual(fresh);
    });

    it('should replace only the scoreboard in the current state on resetScoreboard', () => {
      const before = gameState({ scoreboard: { xWins: 3, oWins: 1, draws: 2 } });
      startWith(before);

      store.resetScoreboard();
      http.expectOne('/api/scoreboard/reset').flush({ xWins: 0, oWins: 0, draws: 0 });

      expect(store.state()).toEqual({ ...before, scoreboard: { xWins: 0, oWins: 0, draws: 0 } });
    });

    it('should clear a previous error after a successful request', () => {
      startWith(gameState());
      store.play(0, 0);
      http
        .expectOne(`/api/games/${ID}/moves`)
        .flush(cellOccupied, { status: 409, statusText: 'Conflict' });
      expect(store.error()).not.toBeNull();

      store.play(0, 1);
      http.expectOne(`/api/games/${ID}/moves`).flush(gameState());
      expect(store.error()).toBeNull();
    });
  });

  describe('error mapping', () => {
    it('should map ProblemDetails.detail to error and leave the state unchanged', () => {
      const before = gameState();
      startWith(before);

      store.play(0, 0);
      http
        .expectOne(`/api/games/${ID}/moves`)
        .flush(cellOccupied, { status: 409, statusText: 'Conflict' });

      expect(store.error()).toBe('Row 1, Column 1 is already taken.');
      expect(store.state()).toEqual(before);
      expect(store.pending()).toBe(false);
    });

    it('should use a fallback message when the response is not ProblemDetails', () => {
      store.newGame('TwoPlayer');
      http.expectOne('/api/games').error(new ProgressEvent('error'));

      expect(store.error()).toBe(FALLBACK_ERROR);
      expect(store.state()).toBeNull();
    });

    it('should dismiss the message on clearError', () => {
      startWith(gameState());
      store.undo();
      http.expectOne(`/api/games/${ID}/undo`).flush(
        { ...cellOccupied, detail: 'There is nothing to undo.', errorCode: 'NOTHING_TO_UNDO' },
        {
          status: 409,
          statusText: 'Conflict',
        },
      );
      expect(store.error()).toBe('There is nothing to undo.');

      store.clearError();
      expect(store.error()).toBeNull();
    });
  });

  describe('pending guard', () => {
    it('should ignore actions while a request is in flight', () => {
      startWith(gameState());

      store.play(0, 0);
      store.play(0, 1);
      store.undo();
      store.reset();
      store.resetScoreboard();
      store.newGame('VsComputer');

      // Only the first move request was sent; http.verify() fails on any extra request.
      http.expectOne(`/api/games/${ID}/moves`).flush(gameState({ currentPlayer: 'O' }));
      expect(store.pending()).toBe(false);
    });

    it('should accept a new action once the pending request has failed', () => {
      startWith(gameState());
      store.play(0, 0);
      http
        .expectOne(`/api/games/${ID}/moves`)
        .flush(cellOccupied, { status: 409, statusText: 'Conflict' });

      store.play(0, 1);
      http.expectOne(`/api/games/${ID}/moves`).flush(gameState());
    });
  });

  describe('computed values (formatting only, from the API state)', () => {
    it('should show whose turn it is while InProgress', () => {
      startWith(gameState({ currentPlayer: 'O' }));
      expect(store.statusMessage()).toBe("O's turn");
      expect(store.isGameOver()).toBe(false);
    });

    it('should show the winner when Won', () => {
      startWith(
        gameState({ status: 'Won', winner: 'X', currentPlayer: null, winningCells: [0, 4, 8] }),
      );
      expect(store.statusMessage()).toBe('X wins');
      expect(store.isGameOver()).toBe(true);
    });

    it("should show It's a draw when Draw", () => {
      startWith(gameState({ status: 'Draw', currentPlayer: null }));
      expect(store.statusMessage()).toBe("It's a draw");
      expect(store.isGameOver()).toBe(true);
    });

    it('should take canUndo straight from the API', () => {
      expect(store.canUndo()).toBe(false);
      startWith(gameState({ canUndo: true }));
      expect(store.canUndo()).toBe(true);
    });
  });
});
