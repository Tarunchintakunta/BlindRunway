# Microcommit plan — execute in order on `main`

Rules for the coding agent:
- One logical change per commit
- Commit message format: `feat(scope): what` or `fix(scope): what` or `chore(scope): what`
- Push to `main` after every 3–5 commits (or after each milestone below)
- Never squash away history; never force-push
- After each milestone: playmode smoke note in commit body

## Milestone A — Repo skeleton
1. `chore(repo): add gitignore unity + readme`
2. `chore(unity): create URP project settings baseline`
3. `chore(docs): import GDD architecture UI levels from design pack`
4. `feat(scenes): add Boot Lobby placeholder scenes`

## Milestone B — Net + lobby (3 players)
5. `feat(net): bootstrap Netcode host/client join code`
6. `feat(lobby): three role cards Pilot Navigator Cabin`
7. `feat(lobby): ready-up and start only when 3 ready`
8. `feat(net): spawn role prefabs with ownership rules`

## Milestone C — Plane + Pilot blind flight
9. `feat(plane): arcade flight model thrust lift drag`
10. `feat(pilot): input pitch yaw throttle gear flaps`
11. `feat(pilot): strip numeric HUD leave silhouette view`
12. `feat(nav): instrument panel altitude airspeed heading`

## Milestone D — Cabin chaos
13. `feat(cabin): passenger agent panic meter`
14. `feat(cabin): cargo crate secure interaction`
15. `feat(cabin): fire extinguisher event stub`

## Milestone E — Win/lose + shared session
16. `feat(core): level timer and gate trigger win`
17. `feat(core): fail states crash fuel panic`
18. `feat(ui): success and fail screens with blame cards`

## Milestone F — Level 1
19. `feat(level01): scene layout runway approach`
20. `feat(level01): LevelDefinition SO + spawn hooks`
21. `feat(level01): tutorial coach marks`
22. `test(level01): playmode smoke host 3 clients note`

## Milestone G — Levels 2–5
23. `feat(level02): crosswind + passenger distract`
24. `feat(level03): banana crates CoG coupling`
25. `feat(level04): instrument glitch windows`
26. `feat(level05): short runway composite challenge`
27. `feat(ui): level select route map five nodes`
28. `feat(meta): unlock progression L1 to L5`

## Milestone H — Polish pass (UI awesome)
29. `feat(ui): main menu parallax and logo`
30. `feat(ui): role intro full bleed screens`
31. `feat(audio): stub SFX bus engine wind alarm`
32. `chore(build): windows + mac build profiles`
33. `docs(ship): how to play local 3-client test`

## Later (NOT now — levels 6–30)
Track in `docs/BACKLOG-L06-L30.md` only; do not implement until Tarun says so.
