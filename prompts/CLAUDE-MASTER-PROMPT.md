# CLAUDE MASTER PROMPT — Blind Runway (ONE agent only)

You are the **only** coding agent on this project. Do not spawn helpers. Do not ask the user to write code. Execute end-to-end.

## Mission
Build **Blind Runway**, a 3-player asymmetric co-op Unity game:
- Pilot flies but cannot see instruments
- Navigator sees instruments but cannot fly
- Cabin handles passengers/cargo chaos, cannot see cockpit

Ship **Levels 1–5** polished, with awesome UI, on branch **`main`**, using **microcommits** pushed regularly.

## Context pack (already in repo / this zip)
Read and obey:
- `docs/GDD.md`
- `docs/ARCHITECTURE.md`
- `docs/UI-DESIGN-SYSTEM.md`
- `docs/MICROCOMMIT-PLAN.md`
- `levels/L01` … `levels/L05`
- `assets/placeholders/` for temporary UI art

## Environment (user already installed)
Mac M5 Pro · Unity 6 LTS · C# · .NET SDK · Git · C++ optional  
Project name: `BlindRunway`  
Remote: push to **`main`** (create repo if missing; never force-push)

## Hard rules
1. **One agent.** No parallel coding agents.
2. **Microcommits** exactly in the spirit of `docs/MICROCOMMIT-PLAN.md` — small, buildable, named `feat|fix|chore(scope): …`
3. **Push to `main`** after each milestone (A→H). Prefer `git pull --rebase` then push.
4. **Playable vertical slice before polish:** Lobby → 3 roles → Level 1 win/lose must work before Levels 2–5.
5. **UI must look intentional** per `UI-DESIGN-SYSTEM.md` — no default gray rectangles as “final”.
6. **Do not implement Levels 6–30** — only leave backlog file.
7. **Secrets:** never commit Steam keys, tokens, or `.env` with credentials.
8. **Tests:** after Level 1 and Level 5, add a short `QA-SMOKE.md` note: how to host + 2 editor clients / builds.
9. If Unity project isn’t created yet: create URP 3D project in-repo, commit `.gitignore` first.
10. Prefer Netcode for GameObjects + Unity Transport. Exactly 3 slots.

## Definition of Done (Act I)
- [ ] Boot → Main Menu → Lobby (3 role cards, join code, ready)
- [ ] Start match only with 3 ready players
- [ ] Pilot blind flight; Navigator instruments; Cabin panic/cargo
- [ ] Levels 1–5 load from level select; unlocks sequential
- [ ] Win + Fail screens with funny blame cards
- [ ] UI matches design tokens (cyan/amber/cabin gold on void navy)
- [ ] All work on `main` with clean microcommit history
- [ ] `QA-SMOKE.md` written
- [ ] Windows + Mac build profiles exist (builds may be local-only)

## Execution loop
For each microcommit item:
1. Implement the smallest slice
2. Ensure project opens / scripts compile
3. Commit with message
4. Every milestone: push `main`
5. Brief status line: what works now

Start at Milestone A and do not skip ahead to Level 5 art before net + roles work.

## Out of scope
Mobile, single-player bots (optional stub OK), monetization, full Steam store page copy, Levels 6–30 gameplay.

BEGIN with Milestone A now.
