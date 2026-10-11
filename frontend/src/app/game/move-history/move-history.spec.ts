import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MoveEntry } from '../game.models';
import { MoveHistory } from './move-history';

describe('MoveHistory', () => {
  let fixture: ComponentFixture<MoveHistory>;

  async function render(moves: MoveEntry[]) {
    fixture = TestBed.createComponent(MoveHistory);
    fixture.componentRef.setInput('moves', moves);
    await fixture.whenStable();
  }

  it('renders move number, player and 1-based position (MH-01)', async () => {
    await render([
      { number: 1, player: 'X', row: 0, col: 0 },
      { number: 2, player: 'O', row: 1, col: 1 },
    ]);

    const rows = Array.from<HTMLTableRowElement>(fixture.nativeElement.querySelectorAll('tbody tr')).map(
      (tr) => Array.from(tr.cells).map((td) => td.textContent?.trim()),
    );
    expect(rows).toEqual([
      ['1', 'X', 'Row 1, Column 1'],
      ['2', 'O', 'Row 2, Column 2'],
    ]);
  });

  it('shows an empty-state message when there are no moves', async () => {
    await render([]);

    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('No moves yet.');
  });
});
