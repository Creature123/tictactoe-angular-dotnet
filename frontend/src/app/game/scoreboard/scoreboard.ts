import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { Scoreboard as ScoreboardCounts } from '../game.models';

/** Session scoreboard from the API, plus Reset Scoreboard. */
@Component({
  selector: 'app-scoreboard',
  templateUrl: './scoreboard.html',
  styleUrl: './scoreboard.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Scoreboard {
  readonly scoreboard = input.required<ScoreboardCounts>();
  readonly pending = input(false);

  readonly resetScoreboard = output<void>();
}
