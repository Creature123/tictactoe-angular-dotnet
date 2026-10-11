---
paths:
  - "backend/tests/**"
  - "frontend/**/*.spec.ts"
---

# Testing rules

- Each test proves one behaviour. Name it `Method_Scenario_ExpectedResult` (C#) or `it('should …')` (TS).
- Tag every backend test with the SPEC requirement it proves: `[Trait("Req", "FR-03")]`.
- Use the Arrange / Act / Assert structure. Build boards from explicit move sequences so a reader can replay
  them, e.g. `Play(game, (0,0), (1,1), (0,1))`.
- Computer-strategy tests: one test per priority rule, plus a tie-break test, plus "does not move after game over".
  Each test uses a board where only that rule decides the answer.
- Undo tests assert the board, the current player, the move history count *and* the status after undo.
- Scoreboard tests assert that a completed game counts exactly once, even if further moves or undos are attempted afterwards.
- API integration tests (`WebApplicationFactory<Program>`): a happy path per endpoint, plus one test per error status code
  and `errorCode`.
- Frontend: test component rendering (9 cells, highlight, disabled states, messages) and
  `HttpTestingController` expectations for every `GameApiService` method.
- Forbidden: `[Fact(Skip=…)]`, `xit`/`fit`, `Thread.Sleep`/`Task.Delay`, empty catch blocks, asserting on
  private state through reflection, and relaxing an assertion to make a test pass.
