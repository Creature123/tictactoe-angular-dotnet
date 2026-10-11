import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Board } from '../board/board';
import { GameControls } from '../game-controls/game-controls';
import { GameStatus } from '../game-status/game-status';
import { GameStore } from '../game.store';
import { ModeSelector } from '../mode-selector/mode-selector';
import { MoveHistory } from '../move-history/move-history';
import { Scoreboard } from '../scoreboard/scoreboard';

/** The only container: wires GameStore state into the presentational components. */
@Component({
  selector: 'app-game-page',
  imports: [Board, GameControls, GameStatus, ModeSelector, MoveHistory, Scoreboard],
  templateUrl: './game-page.html',
  styleUrl: './game-page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GamePage implements OnInit {
  protected readonly store = inject(GameStore);

  ngOnInit(): void {
    // Every page load starts a fresh Two Player game (A-16).
    this.store.newGame('TwoPlayer');
  }

  /** Index → row/col is presentation mapping (index = row * 3 + col), not a game rule. */
  protected onCellSelected(index: number): void {
    this.store.play(Math.floor(index / 3), index % 3);
  }
}
