import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { GameMode, GameState, MoveRequest, Scoreboard } from './game.models';

/** Thin HttpClient wrapper: one method per endpoint in docs/SPEC.md §5.2. */
@Injectable({ providedIn: 'root' })
export class GameApiService {
  private readonly http = inject(HttpClient);

  /** E1 */
  createGame(mode: GameMode): Observable<GameState> {
    return this.http.post<GameState>('/api/games', { mode });
  }

  /** E2 */
  getGame(id: string): Observable<GameState> {
    return this.http.get<GameState>(`/api/games/${id}`);
  }

  /** E3 */
  makeMove(id: string, move: MoveRequest): Observable<GameState> {
    return this.http.post<GameState>(`/api/games/${id}/moves`, move);
  }

  /** E4 */
  undo(id: string): Observable<GameState> {
    return this.http.post<GameState>(`/api/games/${id}/undo`, null);
  }

  /** E5 */
  reset(id: string): Observable<GameState> {
    return this.http.post<GameState>(`/api/games/${id}/reset`, null);
  }

  /** E6 */
  getScoreboard(): Observable<Scoreboard> {
    return this.http.get<Scoreboard>('/api/scoreboard');
  }

  /** E7 */
  resetScoreboard(): Observable<Scoreboard> {
    return this.http.post<Scoreboard>('/api/scoreboard/reset', null);
  }
}
