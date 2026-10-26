# How to play Blind Runway

Three friends land a plane together. Each of you sees only part of what's happening.

## The seats

| Seat | You can | You cannot |
|---|---|---|
| **Pilot** | Fly the plane: pitch, turn, throttle, gear, flaps | See altitude, speed, heading, fuel or warnings |
| **Navigator** | Read every instrument, the runway map, wind and fuel | Touch the controls. You can only talk and ping |
| **Cabin** | Walk the aisle, strap and calm passengers, secure cargo, fight fires | See outside or the instruments |

Talk over voice chat (Discord or similar). In-game callouts cover everything else: they show on the other
players' screens.

## Controls

**Pilot**: `W`/`S` pitch (S pulls the nose up) · `A`/`D` turn · `Shift`/`Ctrl` throttle up/down ·
`G` gear · `F` flaps · `1` "Say again?" · `2` "How high are we?" · `3` "Copy!"

**Navigator**: `1` PULL UP · `2` NOSE DOWN · `3` LEFT · `4` RIGHT · `5` MORE POWER · `6` LESS POWER ·
`7` GEAR DOWN! · `8` FLAPS! · `9` IGNORE ALT · `0` CUT THROTTLE ·
`Space` read altitude aloud · `V` read speed · `B` read fuel · `R` ping the gear switch · `F` ping flaps

**Cabin**: `WASD` walk · hold `E` (or `Space`) to interact · `1` "Cabin secure!" · `2` "Fire!" ·
`3` "Stop wobbling!"

**Everyone**: `Esc` pause/settings. The plane keeps flying while you're paused.

## Winning a level

1. Get the gear down before touchdown. Flaps help you fly slowly.
2. Touch down gently on the runway.
3. **Cut the throttle.** Brakes only work at idle.
4. Stop inside the **green gate box**.
5. Meet the cabin rules for the level: cargo secured, enough passengers strapped in, fire out, panic
   under the cap.

Stars: 1 for landing, +1 for a 100% secure cabin, +1 for a soft touchdown on the centerline.

You fail if you crash, run out of fuel, hit 100% cabin panic, run out the timer, or stop outside the gate.
The blame cards then tell you whose fault it was (affectionately).

## The campaign (Act I)

| # | Route | New trouble |
|---|---|---|
| 1 | Clear Skies (Lying) | Tutorial: the voice loop, the gear, a loose bag |
| 2 | Crosswind Banter | 4 m/s crosswind; a thirsty passenger blocks the aisle |
| 3 | Banana in the Hold | Loose banana crates shift the centre of gravity |
| 4 | Fog & Fibs | Fog; the altimeter lies twice for 8 s |
| 5 | Short Final Circus | Short runway, rain, fire, tight fuel, required fuel callout |

Clearing a route unlocks the next one for the player who hosted it. Routes 6–30 are on the backlog.

## Playing alone

**Main Menu > PLAY ALONE**, pick an unlocked route, **FLY SOLO**.

You hold all three seats, but only one is active at a time. Switch seats with `F1` Pilot, `F2` Navigator,
`F3` Cabin, or with the seat bar at the bottom of the screen. The seat you leave goes idle: no stick held,
nobody walking. The rules don't change:

- The Pilot seat still shows **no numbers**. To read altitude, switch to the Navigator (F2), then hop back.
- Same levels, timers, gate boxes, cabin rules and blame cards as with friends.
- Clearing a route alone unlocks the next one, same as hosting.
- `Esc` in flight: Resume, switch seat, Settings, Quit to menu. The plane keeps flying while paused.

Solo runs a host-only server on your own computer (loopback). No network, no firewall prompt, and it
doesn't clash with a friend hosting on the same machine.

## Playing with friends

### Same network (LAN)
1. Host: **PLAY WITH FRIENDS > HOST A FLIGHT** (or **LEVELS** > route > **HOST FOR FRIENDS**). Share the **join code**
   shown top-right, for example `C0A8-0105`.
2. Friends: **PLAY WITH FRIENDS**, type the code, then **JOIN**. A raw IP address also works.
3. Everyone takes a seat and presses **READY**. The host presses **START**.

The host's firewall must allow UDP port **7777**. Internet play needs Unity Relay, which is not wired up.

### Three windows on one computer (testing)
Build once with **Blind Runway > Build > macOS (dev)** or **Windows (dev)**, then launch the build three
times. On macOS use `open -n Builds/macOS-dev/BlindRunway.app`. Leave the join code blank in the joining
windows. See [QA-SMOKE.md](QA-SMOKE.md) for the full checklist.

### Dev shortcut
In the editor or a dev build, a lobby with only you in it also shows **SOLO TEST**. It turns that lobby
into a solo flight. Players should use **PLAY ALONE**.
