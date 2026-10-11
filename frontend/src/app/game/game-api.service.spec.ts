import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { GameApiService } from './game-api.service';
import { GameState, Scoreboard } from './game.models';

const ID = '3f2b8c1e-6a4d-4b7e-9c11-2d5e8f0a7b93';

describe('GameApiService', () => {
  let api: GameApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(GameApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('should POST the mode to /api/games on createGame (E1)', () => {
    const game = { id: ID } as GameState;
    let result: GameState | undefined;
    api.createGame('VsComputer').subscribe((g) => (result = g));

    const req = http.expectOne('/api/games');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ mode: 'VsComputer' });
    req.flush(game);
    expect(result).toEqual(game);
  });

  it('should GET /api/games/{id} on getGame (E2)', () => {
    api.getGame(ID).subscribe();

    const req = http.expectOne(`/api/games/${ID}`);
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('should POST player, row and col to /api/games/{id}/moves on makeMove (E3)', () => {
    api.makeMove(ID, { player: 'X', row: 0, col: 2 }).subscribe();

    const req = http.expectOne(`/api/games/${ID}/moves`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ player: 'X', row: 0, col: 2 });
    req.flush({});
  });

  it('should POST to /api/games/{id}/undo with no body on undo (E4)', () => {
    api.undo(ID).subscribe();

    const req = http.expectOne(`/api/games/${ID}/undo`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});
  });

  it('should POST to /api/games/{id}/reset with no body on reset (E5)', () => {
    api.reset(ID).subscribe();

    const req = http.expectOne(`/api/games/${ID}/reset`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({});
  });

  it('should GET /api/scoreboard on getScoreboard (E6)', () => {
    const scoreboard: Scoreboard = { xWins: 2, oWins: 1, draws: 0 };
    let result: Scoreboard | undefined;
    api.getScoreboard().subscribe((s) => (result = s));

    const req = http.expectOne('/api/scoreboard');
    expect(req.request.method).toBe('GET');
    req.flush(scoreboard);
    expect(result).toEqual(scoreboard);
  });

  it('should POST to /api/scoreboard/reset with no body on resetScoreboard (E7)', () => {
    api.resetScoreboard().subscribe();

    const req = http.expectOne('/api/scoreboard/reset');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush({ xWins: 0, oWins: 0, draws: 0 });
  });
});
