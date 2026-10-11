import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Player } from '../game.models';

/** The 3×3 grid. Cell index = row * 3 + col; labels are 1-based for the user. */
@Component({
  selector: 'app-board',
  templateUrl: './board.html',
  styleUrl: './board.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Board {
  readonly board = input.required<readonly (Player | null)[]>();
  readonly winningCells = input<readonly number[]>([]);
  /** True while the game is over or a request is pending. */
  readonly disabled = input(false);

  readonly cellSelected = output<number>();

  protected isWinning(index: number): boolean {
    return this.winningCells().includes(index);
  }

  protected label(cell: Player | null, index: number): string {
    return `Row ${Math.floor(index / 3) + 1}, Column ${(index % 3) + 1}, ${cell ?? 'empty'}`;
  }
}
