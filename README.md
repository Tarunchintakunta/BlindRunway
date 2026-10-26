# Blind Runway

3-player asymmetric co-op airplane game (BOMBANANA-style roles + Dear Passengers chaos).

**Roles**
- **Pilot** — can fly the plane, cannot see instruments
- **Navigator** — can see instruments / runway / hazards, cannot touch controls
- **Cabin** — sees passengers/cargo chaos inside, cannot see cockpit

**Scope now:** 5 polished levels + lobby + core loop  
**Later:** levels 6–30

## What's in this zip
| Path | Purpose |
|------|---------|
| `prompts/CLAUDE-MASTER-PROMPT.md` | Paste this into Claude. One agent. Microcommits. Push to `main`. |
| `docs/GDD.md` | Full game design |
| `docs/ARCHITECTURE.md` | Unity / C# / networking / folder layout |
| `docs/UI-DESIGN-SYSTEM.md` | Colors, type, screens, motion |
| `docs/MICROCOMMIT-PLAN.md` | Ordered commits for levels 1–5 |
| `levels/L01`–`L05` | Per-level design + win/fail + UI beats |
| `assets/placeholders/` | SVG/PNG placeholders for UI until art is final |
| `scripts/` | Helper shell notes for git/Unity |

## Your machine
Mac M5 Pro · Unity 6 LTS · C# · .NET SDK · Git · (C++ optional)

## How to run Claude on this
1. Create empty GitHub repo `BlindRunway` (private or public).
2. Open Unity Hub → New 3D (URP) project named `BlindRunway` inside a clone of that repo.
3. Open the repo in Claude / Cursor.
4. Paste **only** `prompts/CLAUDE-MASTER-PROMPT.md` as the task.
5. Let **one** agent execute the microcommit plan until Level 5 ships on `main`.

Do **not** spin a second coding agent. Do **not** force-push. Do **not** skip the commit order.
