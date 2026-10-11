import { Component, inject } from '@angular/core';
import { GamePage } from './game/game-page/game-page';
import { ThemeService } from './theme/theme.service';

@Component({
  imports: [GamePage],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App {
  protected readonly theme = inject(ThemeService);
}
