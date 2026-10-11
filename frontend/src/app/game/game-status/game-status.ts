import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { GameMode } from '../game.models';
import { MODE_LABELS } from '../mode-labels';

/** Shows the selected mode and the turn / winner / draw message (FE-02). */
@Component({
  selector: 'app-game-status',
  templateUrl: './game-status.html',
  styleUrl: './game-status.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameStatus {
  readonly message = input.required<string>();
  readonly mode = input.required<GameMode>();

  protected readonly modeLabel = computed(() => MODE_LABELS[this.mode()]);
}
