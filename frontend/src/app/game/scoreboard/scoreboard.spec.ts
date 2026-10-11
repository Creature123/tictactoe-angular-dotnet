import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Scoreboard } from './scoreboard';

describe('Scoreboard', () => {
  let fixture: ComponentFixture<Scoreboard>;

  const resetButton = () => fixture.nativeElement.querySelector('button') as HTMLButtonElement;
  const score = (label: string) =>
    Array.from<HTMLElement>(fixture.nativeElement.querySelectorAll('.score'))
      .find((s) => s.querySelector('dt')?.textContent?.trim() === label)
      ?.querySelector('dd')
      ?.textContent?.trim();

  async function render(pending = false) {
    fixture = TestBed.createComponent(Scoreboard);
    fixture.componentRef.setInput('scoreboard', { xWins: 3, oWins: 1, draws: 2 });
    fixture.componentRef.setInput('pending', pending);
    await fixture.whenStable();
  }

  it('renders the counts from the API (FE-04)', async () => {
    await render();

    expect(score('X wins')).toBe('3');
    expect(score('O wins')).toBe('1');
    expect(score('Draws')).toBe('2');
  });

  it('emits once when Reset Scoreboard is clicked (FE-05)', async () => {
    await render();
    let resets = 0;
    fixture.componentInstance.resetScoreboard.subscribe(() => resets++);

    resetButton().click();

    expect(resets).toBe(1);
  });

  it('disables Reset Scoreboard while a request is pending (FE-06)', async () => {
    await render(true);

    expect(resetButton().disabled).toBe(true);
  });
});
