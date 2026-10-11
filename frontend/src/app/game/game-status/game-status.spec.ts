import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameMode } from '../game.models';
import { GameStatus } from './game-status';

describe('GameStatus', () => {
  let fixture: ComponentFixture<GameStatus>;

  async function render(message: string, mode: GameMode = 'TwoPlayer'): Promise<string> {
    fixture = TestBed.createComponent(GameStatus);
    fixture.componentRef.setInput('message', message);
    fixture.componentRef.setInput('mode', mode);
    await fixture.whenStable();
    return (fixture.nativeElement as HTMLElement).textContent ?? '';
  }

  it('shows whose turn it is (FR-06)', async () => {
    expect(await render("O's turn")).toContain("O's turn");
  });

  it('shows the winner message (FR-12)', async () => {
    expect(await render('X wins')).toContain('X wins');
  });

  it('shows the draw message (FR-16)', async () => {
    expect(await render("It's a draw")).toContain("It's a draw");
  });

  it('shows the selected mode label (FE-02)', async () => {
    const text = await render("X's turn", 'VsComputer');

    expect(text).toContain('Play Against Computer');
    expect(text).toContain("X's turn");
  });
});
