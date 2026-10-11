import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GameControls } from './game-controls';

describe('GameControls', () => {
  let fixture: ComponentFixture<GameControls>;

  const button = (text: string) =>
    Array.from<HTMLButtonElement>(fixture.nativeElement.querySelectorAll('button')).find((b) =>
      b.textContent?.includes(text),
    )!;

  async function render(canUndo: boolean, pending = false) {
    fixture = TestBed.createComponent(GameControls);
    fixture.componentRef.setInput('canUndo', canUndo);
    fixture.componentRef.setInput('pending', pending);
    await fixture.whenStable();
  }

  it('disables Undo when canUndo is false (MH-06, MH-07)', async () => {
    await render(false);

    expect(button('Undo Last Move').disabled).toBe(true);
    expect(button('Reset Game').disabled).toBe(false);
  });

  it('disables Undo while a request is pending (FE-06)', async () => {
    await render(true, true);

    expect(button('Undo Last Move').disabled).toBe(true);
  });

  it('enables Undo when canUndo is true and nothing is pending, and emits on click', async () => {
    await render(true);
    let undone = 0;
    fixture.componentInstance.undo.subscribe(() => undone++);

    button('Undo Last Move').click();

    expect(button('Undo Last Move').disabled).toBe(false);
    expect(undone).toBe(1);
  });
});
