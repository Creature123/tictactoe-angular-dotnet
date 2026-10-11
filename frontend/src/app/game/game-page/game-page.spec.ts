import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameState } from '../game.models';
import { GameStore } from '../game.store';
import { GamePage } from './game-page';

function gameState(overrides: Partial<GameState> = {}): GameState {
  return {
    id: '3f2b8c1e-6a4d-4b7e-9c11-2d5e8f0a7b93',
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

/** Stand-in for GameStore: plain signals plus spies, so the page is tested in isolation. */
function createStoreStub() {
  return {
    state: signal<GameState | null>(null),
    pending: signal(false),
    error: signal<string | null>(null),
    isGameOver: signal(false),
    canUndo: signal(false),
    statusMessage: signal(''),
    newGame: vi.fn(),
    play: vi.fn(),
    undo: vi.fn(),
    reset: vi.fn(),
    resetScoreboard: vi.fn(),
    clearError: vi.fn(),
  };
}

describe('GamePage', () => {
  let fixture: ComponentFixture<GamePage>;
  let store: ReturnType<typeof createStoreStub>;

  beforeEach(async () => {
    store = createStoreStub();
    TestBed.configureTestingModule({ providers: [{ provide: GameStore, useValue: store }] });
    fixture = TestBed.createComponent(GamePage);
    await fixture.whenStable();
  });

  it("starts a Two Player game on init (A-16)", () => {
    expect(store.newGame).toHaveBeenCalledExactlyOnceWith('TwoPlayer');
  });

  it('converts a selected cell index to row and column', async () => {
    store.state.set(gameState());
    await fixture.whenStable();

    const cells = fixture.nativeElement.querySelectorAll('app-board button');
    (cells[5] as HTMLButtonElement).click();

    expect(store.play).toHaveBeenCalledExactlyOnceWith(1, 2);
  });

  const button = (text: string) =>
    Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(text),
    )!;

  it('starts a new game in the picked mode (FE-08)', async () => {
    store.state.set(gameState());
    await fixture.whenStable();
    store.newGame.mockClear();

    button('Computer').click();

    expect(store.newGame).toHaveBeenCalledExactlyOnceWith('VsComputer');
  });

  it('sends one reset when Reset Game is clicked (FE-05)', async () => {
    store.state.set(gameState());
    await fixture.whenStable();

    button('Reset Game').click();

    expect(store.reset).toHaveBeenCalledOnce();
    expect(store.resetScoreboard).not.toHaveBeenCalled();
  });

  it('sends one scoreboard reset when Reset Scoreboard is clicked (FE-05)', async () => {
    store.state.set(gameState());
    await fixture.whenStable();

    button('Reset Scoreboard').click();

    expect(store.resetScoreboard).toHaveBeenCalledOnce();
    expect(store.reset).not.toHaveBeenCalled();
  });

  it('shows the API error in a dismissible banner (FE-07)', async () => {
    store.error.set('Row 1, Column 1 is already taken.');
    await fixture.whenStable();

    const banner = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(banner.textContent).toContain('Row 1, Column 1 is already taken.');

    (banner.querySelector('button') as HTMLButtonElement).click();
    expect(store.clearError).toHaveBeenCalledOnce();
  });
});
