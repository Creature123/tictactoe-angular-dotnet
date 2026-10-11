import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Player } from '../game.models';
import { Board } from './board';

function emptyBoard(): (Player | null)[] {
  return Array(9).fill(null);
}

describe('Board', () => {
  let fixture: ComponentFixture<Board>;
  let selected: number[];

  const cells = () => Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button'));

  beforeEach(() => {
    fixture = TestBed.createComponent(Board);
    selected = [];
    fixture.componentInstance.cellSelected.subscribe((index) => selected.push(index));
  });

  async function render(board: (Player | null)[], winningCells: number[] = [], disabled = false) {
    fixture.componentRef.setInput('board', board);
    fixture.componentRef.setInput('winningCells', winningCells);
    fixture.componentRef.setInput('disabled', disabled);
    await fixture.whenStable();
  }

  it('renders 9 cell buttons, all enabled on an empty in-progress board (FE-01)', async () => {
    await render(emptyBoard());

    expect(cells()).toHaveLength(9);
    expect(cells().every((c) => !c.disabled)).toBe(true);
  });

  it('labels cells 1-based and shows the mark (FR-03)', async () => {
    const board = emptyBoard();
    board[4] = 'O';
    await render(board);

    expect(cells()[4].getAttribute('aria-label')).toBe('Row 2, Column 2, O');
    expect(cells()[4].textContent?.trim()).toBe('O');
    expect(cells()[0].getAttribute('aria-label')).toBe('Row 1, Column 1, empty');
  });

  it('emits the index when an empty cell is clicked', async () => {
    await render(emptyBoard());

    cells()[5].click();

    expect(selected).toEqual([5]);
  });

  it('disables occupied cells and does not emit for them', async () => {
    const board = emptyBoard();
    board[0] = 'X';
    await render(board);

    cells()[0].click();

    expect(cells()[0].disabled).toBe(true);
    expect(selected).toEqual([]);
  });

  it('disables every cell and emits nothing when disabled (game over or pending, FR-16)', async () => {
    await render(emptyBoard(), [], true);

    cells().forEach((c) => c.click());

    expect(cells().every((c) => c.disabled)).toBe(true);
    expect(selected).toEqual([]);
  });

  it('applies the winning class to exactly the winning cells (FR-12)', async () => {
    const board: (Player | null)[] = ['X', 'O', 'O', null, 'X', null, null, null, 'X'];
    await render(board, [0, 4, 8], true);

    const winning = cells()
      .map((c, i) => (c.classList.contains('winning') ? i : -1))
      .filter((i) => i >= 0);
    expect(winning).toEqual([0, 4, 8]);
  });
});
