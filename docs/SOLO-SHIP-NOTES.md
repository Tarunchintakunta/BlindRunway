# Single-player mode: ship notes

Brief: `CLAUDE-SINGLE-PLAYER-PROMPT.md`. All work landed on `main` as small commits, from
`feat(solo): main menu Play Alone entry` through this note.

## What shipped
- **Main menu**: PLAY ALONE, PLAY WITH FRIENDS, LEVELS. LEVELS offers FLY SOLO or HOST FOR FRIENDS
  per route.
- **`MatchMode`** (Multiplayer | Solo) on `LobbyState`. Solo skips the three-ready gate. Multiplayer is
  unchanged: exactly 3 players, all ready.
- **Ownership**: `Session.SeatFor` is the single rule. In multiplayer a client drives one seat. In solo
  the host-only player holds all three and names the seat it is driving.
- **Asymmetry holds in solo**: the server sends a snapshot for the *driven* seat only, so the Pilot view
  never receives instrument values.
- **Seat switching**: F1/F2/F3, the bottom hot-seat bar, and Esc > switch seat. The seat you leave gets a
  neutral input and goes idle.
- **Solo intro**: three seat cards before the first solo flight of a session (GOT IT / SKIP). Later
  flights skip it. L01 has a one-time "Solo:" coach mark.
- **Solo end paths**: RETRY / NEXT ROUTE / MENU, never "waiting for crew". Pause: Quit to menu.
- **Networking**: solo is a host-only Netcode listen server on **loopback, random high port**. It works
  offline, never shows the firewall prompt, and never collides with a friends' host on 7777. `-port N`
  overrides the friends port for tests.
- **Progress**: solo clears unlock routes through the same `Progress.Record` path as hosted games.

## Verification (Unity 6000.6.7f1, batchmode, this Mac)
- EditMode **34/34** (4 new solo tests: ready gate, multiplayer ownership, solo ownership, solo Pilot
  privacy).
- PlayMode **2/2**: Play Alone → intro → flight → seat swaps, each seat getting only its own data → menu;
  and L01 → timer fail → three blame cards → retry → menu.
- Three-process multiplayer regression (`scripts/smoke-3clients.sh`, port 7790): PASS for all seats.
- Manual click-through (menu → Play Alone → L01 → land) **not done by the agent**. The PlayMode tests
  drive the same code paths, but not the mouse.

## Files touched
`Scripts/Net/Session.cs`, `Scripts/Net/Net.cs`, `Scripts/UI/Screens.cs`, `Scripts/UI/Hud.cs`,
`Scripts/UI/Overlay.cs`, `ScriptableObjects/Resources/Levels/L01.asset`,
`Tests/EditMode/SoloModeTests.cs` (+ meta), `Tests/PlayMode/SoloFlightSmoke.cs`, `HOW-TO-PLAY.md`,
`QA-SMOKE.md`, `README.md`, `scripts/smoke-3clients.sh`.

## Residual risks
- **Seat-switch snapshot lag**: right after a switch, the next ~50 ms of snapshots can still be for the
  old seat. HUDs guard on `snap.nav` / `snap.cabin`, so you see a blank frame, not wrong data.
- **Solo is hard by design**: the plane flies itself only lightly (autostabilize) while you work the
  cabin. No AI copilot exists; the brief keeps it out of scope and off by default.
- **The solo intro "seen" flag** lasts one app session and is not saved.
- **The solo coach mark** shares the L01 Pilot "seen" flag. A player who already saw the L01 Pilot coach
  marks in multiplayer won't see the solo line.
- **The PlayMode fail test** edits the in-memory L01 asset (`timeLimit = 3`) and restores it in `finally`.
  An aborted editor run could leave the asset dirty; don't save the project mid-test.
- **The dev SOLO TEST lobby button** still exists in editor and dev builds, and binds 7777 like any
  friends' host.
