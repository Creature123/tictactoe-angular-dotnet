import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameMode } from '../game.models';
import { ModeSelector } from './mode-selector';

describe('ModeSelector', () => {
  let fixture: ComponentFixture<ModeSelector>;
  let emitted: GameMode[];

  const buttons = () => Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button'));
  const button = (text: string) => buttons().find((b) => b.textContent?.includes(text))!;

  async function render(mode: GameMode | null, disabled = false) {
    fixture = TestBed.createComponent(ModeSelector);
    fixture.componentRef.setInput('mode', mode);
    fixture.componentRef.setInput('disabled', disabled);
    emitted = [];
    fixture.componentInstance.modeChange.subscribe((m) => emitted.push(m));
    await fixture.whenStable();
  }

  it('emits the other mode once when it is clicked (FE-08)', async () => {
    await render('TwoPlayer');

    button('Computer').click();

    expect(emitted).toEqual(['VsComputer']);
  });

  it('emits nothing when the active mode is clicked', async () => {
    await render('TwoPlayer');

    button('Two Player').click();

    expect(emitted).toEqual([]);
  });

  it('marks the active mode as pressed', async () => {
    await render('VsComputer');

    expect(button('Computer').getAttribute('aria-pressed')).toBe('true');
    expect(button('Two Player').getAttribute('aria-pressed')).toBe('false');
  });

  it('disables every mode button while a request is pending (FE-06)', async () => {
    await render('TwoPlayer', true);

    expect(buttons().every((b) => b.disabled)).toBe(true);
  });
});
