---
name: ai-workflow-log
description: Append a structured entry for the phase just completed to docs/AI_WORKFLOW.md (prompts used, what the AI generated, what the human reviewed and changed, verification). Run manually at the end of each phase.
disable-model-invocation: true
argument-hint: "[phase name, e.g. 'Phase 2 – Domain TDD']"
allowed-tools: Read, Edit, Bash(git status *), Bash(git diff *), Bash(git log *)
---

# Log an AI workflow entry for: $ARGUMENTS

## Context gathered automatically
- Working tree: !`git status --short`
- Changed since last commit: !`git diff --stat HEAD`
- Recent commits: !`git log --oneline -5`

## Instructions
1. Re-read the prompts I sent in this session for this phase. Quote each one **verbatim** in a fenced block.
   If the session was compacted and you cannot see a prompt, write `[prompt not available – see exported transcript]`
   rather than reconstructing it.
2. Summarise what you (the AI) generated: files created or changed, and the key design choices you made.
3. Then **ask me** these questions and wait for my answers. Never fill them in yourself:
   - What did you review line by line?
   - What did you change manually, and why?
   - What AI suggestions did you reject, and why?
   - Any new assumptions or trade-offs?
4. Append one entry to the end of `docs/AI_WORKFLOW.md` using exactly this template. Do not modify earlier entries.

```markdown
### <phase name> — <YYYY-MM-DD HH:MM>
**Goal:** <one line>
**Prompts used:**
<verbatim prompts in fenced blocks>
**AI generated:** <bullets: files and key choices>
**Human review:** <my answer>
**Manual changes:** <my answer>
**Rejected AI suggestions:** <my answer>
**Assumptions / trade-offs:** <my answer, or "none new">
**Verification:** <commands run + results, e.g. `dotnet test backend` → 41 passed>
```
