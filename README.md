# BlindRunway

3-player asymmetric co-op airplane game (BOMBANANA-style roles + Dear Passengers chaos).
*Three seats. Zero shared truth.*

**Roles**
- **Pilot**: flies the plane, cannot see instruments
- **Navigator**: sees instruments, runway and hazards, cannot touch the controls
- **Cabin**: handles the passenger and cargo chaos inside, cannot see the cockpit

**Shipped (Act I):** Boot → Main Menu (Play Alone / Play With Friends) → Route map → Lobby (join code, 3 role cards, ready) → Role intro →
Levels 1–5 → Success/Fail with blame cards, unlock progression, pause/settings, procedural SFX.
**Later:** levels 6–30 ([backlog](docs/BACKLOG-L06-L30.md)).

## Run it
1. Unity Hub > install **Unity 6000.6.7f1** > **Add project from disk** > this folder.
2. Press Play from `Assets/_Project/Scenes/Boot.unity` (any scene works; everything is built in code).
3. Alone: **PLAY ALONE** > route > **FLY SOLO**, then F1/F2/F3 (or the seat bar) to switch seats. With friends: **PLAY WITH FRIENDS**.

- How to play, controls and LAN setup: [HOW-TO-PLAY.md](HOW-TO-PLAY.md)
- Host + 2 clients test checklist and current verification status: [QA-SMOKE.md](QA-SMOKE.md)

## Layout
| Path | Purpose |
|------|---------|
| `Assets/_Project/Scripts/` | Game code (`BlindRunway` assembly): Core, Net, Plane, Cabin, Roles, UI, World, Audio |
| `Assets/_Project/ScriptableObjects/Resources/Levels/` | `L01`–`L05` LevelDefinition assets |
| `Assets/_Project/Tests/EditMode/` | Flight, cabin, rules, progression, join-code and snapshot-privacy tests |
| `Assets/_Project/Editor/` | URP setup + Windows/macOS build menu |
| `docs/` | GDD, architecture (with as-built notes), UI design system, microcommit plan, placeholders |
| `levels/L01`–`L05` | Per-level design notes |
| `prompts/CLAUDE-MASTER-PROMPT.md` | The brief this repo was built from |

## Your machine
Mac M5 Pro · Unity 6 LTS · C# · .NET SDK · Git · (C++ optional)
