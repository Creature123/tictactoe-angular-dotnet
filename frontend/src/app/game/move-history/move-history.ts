import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MoveEntry } from '../game.models';

/** Move list from the API; positions shown 1-based (MH-01). */
@Component({
  selector: 'app-move-history',
  templateUrl: './move-history.html',
  styleUrl: './move-history.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MoveHistory {
  readonly moves = input.required<readonly MoveEntry[]>();

  protected position(move: MoveEntry): string {
    return `Row ${move.row + 1}, Column ${move.col + 1}`;
  }
}
