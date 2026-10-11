import { Component } from '@angular/core';
import { GamePage } from './game/game-page/game-page';

@Component({
  imports: [GamePage],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {}
