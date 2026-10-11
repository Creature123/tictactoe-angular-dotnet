# Vendored skills (provenance)

Third-party skills are copied into the repo instead of being installed as global plugins, so that:
- reviewers can see exactly what context the AI had,
- versions are pinned (supply-chain hygiene: every SKILL.md was read before it was added), and
- the context stays lean (only skills relevant to this project are loaded, not 30+ Akka/Aspire skills).

| Skill folder | Source | Commit | License |
|---|---|---|---|
| `modern-csharp-coding-standards` | github.com/Aaronontheweb/dotnet-skills `skills/csharp-coding-standards` | 29878f2 | MIT |
| `dependency-injection-patterns` | github.com/Aaronontheweb/dotnet-skills `skills/microsoft-extensions-dependency-injection` | 29878f2 | MIT |
| `code-reviewer` | github.com/Jeffallan/claude-skills `skills/code-reviewer` | 1be15d8 | MIT |
| `angular-developer`, `angular-new-app` | github.com/angular/skills (installed with `npx skills add angular/skills`) | 0dfaf19 | MIT |
| `ai-workflow-log` | Written for this project | – | – |

Deliberately **not** used: `csharp-concurrency-patterns`, because it steers towards Channels/Akka, which is over-engineering
for a single-process in-memory store where a short per-game `lock` is the simplest correct choice.
