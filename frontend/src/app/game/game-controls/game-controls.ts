import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/** Reset Game and Undo Last Move. Undo availability comes from the API (`canUndo`, FE-06). */
@Component({
  selector: 'app-game-controls',
  templateUrl: './game-controls.html',
  styleUrl: './game-controls.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameControls {
  readonly canUndo = input.required<boolean>();
  readonly pending = input(false);

  readonly resetGame = output<void>();
  readonly undo = output<void>();
}
