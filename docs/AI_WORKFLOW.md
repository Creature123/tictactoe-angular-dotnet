# AI-Assisted Development Workflow

<!-- Before submission, edit the Workflow and Guardrails sections so they describe exactly what you did. -->

This file records how the solution was built with AI assistance, so that the panel can see the prompts, the
AI output, and my own review and changes. Each phase entry is appended at the end of that phase with the
`/ai-workflow-log` skill. The "Human review" and "Manual changes" fields are written by me, not the AI.

## Tools
| Tool | Used for |
|------|----------|
| Claude (chat / Projects) | Reading the brief, planning phases, drafting the Claude Code configuration |
| Claude Code (VS Code extension + CLI) | Spec generation, TDD implementation, refactoring, reviews |
| Skills: `modern-csharp-coding-standards`, `dependency-injection-patterns` (Aaron Stannard, MIT) | C# conventions |
| Skills: `angular-developer`, `angular-new-app` (Angular team, MIT) | Modern Angular conventions and scaffolding |
| Skill: `code-reviewer` (Jeffallan, MIT), loaded into the `code-reviewer` subagent | Independent, read-only reviews |
| Custom: `spec-verifier` subagent, `/ai-workflow-log` skill, guard hooks | Traceability, logging, guardrails |

## Workflow
1. **Requirements → Spec.** Claude turned the brief into `docs/SPEC.md`, with requirement IDs, Given/When/Then
   criteria, the API contract and an ambiguity log. I resolved each ambiguity in `docs/DECISIONS.md`.
2. **Plan before code.** Every multi-file change started in Plan Mode. I approved or edited the plan before any edits.
3. **Test-first domain.** The tests were generated first from SPEC IDs. I reviewed and corrected them, then locked them (`.claude/tests.lock`,
   enforced by a PreToolUse hook) while the AI implemented the code to make them pass.
4. **Thin vertical slices.** I committed each slice myself after reviewing the diff and running the tests.
5. **Independent review.** The `code-reviewer` subagent ran in a fresh context against SPEC. I triaged each finding
   (fix / won't fix + reason).
6. **Verification.** Automated tests, the `spec-verifier` traceability report, and a manual walk-through of every
   acceptance criterion in the browser.

## Guardrails
- `.claude/settings.json` blocks edits to `docs/requirements/**` and `.env*`, and asks before commits, pushes and new packages.
- `.claude/hooks/guard-tests.mjs` blocks edits to test files while `.claude/tests.lock` exists (prevents the
  AI from weakening tests to get to green).
- `.claude/hooks/format-on-edit.mjs` formats edited files deterministically (`dotnet format`, Prettier) instead of
  relying on the model.

## Notable AI mistakes I caught
<!-- Keep a running list as you go. This is the most valuable section for the panel. -->
| Phase | What the AI produced | Why it was wrong | How I caught it | Fix |
|-------|----------------------|------------------|-----------------|-----|

## Phase log
<!-- Entries are appended below by /ai-workflow-log -->
