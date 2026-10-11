import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { GameMode } from '../game.models';
import { MODE_LABELS } from '../mode-labels';

/** Picking a different mode starts a new game (A-04). */
@Component({
  selector: 'app-mode-selector',
  templateUrl: './mode-selector.html',
  styleUrl: './mode-selector.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModeSelector {
  /** `null` before the first game has loaded. */
  readonly mode = input.required<GameMode | null>();
  readonly disabled = input(false);

  readonly modeChange = output<GameMode>();

  protected readonly modes: readonly GameMode[] = ['TwoPlayer', 'VsComputer'];
  protected readonly labels = MODE_LABELS;

  protected select(mode: GameMode): void {
    if (mode !== this.mode()) this.modeChange.emit(mode);
  }
}
