# QA smoke test: Blind Runway

## Status (be honest here)

| Check | Result |
|---|---|
| C# compiles | Yes, against hand-written Unity API stubs with .NET 8 (C# 9). Not yet compiled by the Unity editor. |
| EditMode tests (`Assets/_Project/Tests/EditMode`) | 29 pass under the same stub harness (flight model incl. crosswind and L05 fuel budget, cabin, rules, blame cards, progression, glitch privacy, join codes). |
| Playmode with 3 real clients | **Not run yet.** The Unity editor was not installed on the machine that wrote this code. |

The commit `feat(net): seat ownership rules for role input` says "Playmode smoke: host + 2 clients reach
lobby…". No smoke test was actually run when that line was written, so ignore it until someone runs the
checklist below.

## First open

1. Install **Unity 6000.0 LTS** through Unity Hub (6000.0.23f1 or newer 6000.0.x).
2. Unity Hub > **Add project from disk** > this repo's root folder.
3. Wait for packages to resolve: Netcode for GameObjects 2.1.1, Unity Transport 2.3.0, URP 17.0.3,
   uGUI 2.0, Test Framework.
4. Optional: menu **Blind Runway > Setup URP Pipeline**. The game also renders on the built-in pipeline.
   Keep *Project Settings > Player > Active Input Handling* on **Input Manager (Old)** or **Both**.
5. **Window > General > Test Runner > EditMode > Run All**. Expect all tests green.

If the console shows compile errors, fix those first. Each one is a real bug, because the stub harness
only knows the API surface it was given.

## Solo smoke (one editor, 2 minutes)

1. Open `Assets/_Project/Scenes/Boot.unity` and press Play. (Play from any scene also works.)
2. Splash > Main Menu > **PLAY** > **HOST A FLIGHT**.
3. In the lobby, press **SOLO TEST (F1-F3 swap seats)**. This button only appears in the editor and dev
   builds.
4. During the level, F1 shows Pilot, F2 Navigator, F3 Cabin.
   - Pilot: no numbers anywhere. W/S pitch, A/D turn, Shift/Ctrl throttle, G gear, F flaps.
   - Navigator: altitude, speed and other values update. SPACE reads altitude, and the callout shows on
     the Pilot HUD.
   - Cabin: WASD walks. Hold E near a passenger to strap them in, and near the bag to secure it.
5. Land with gear down and cut the throttle (Ctrl). Stop between the green bars to see the **LANDED**
   screen. With gear up you get **WHY WE DIED** and a BELLY LANDER card.

## Three-client smoke (the real test)

Netcode needs three separate processes. The quickest way:

1. Menu **Blind Runway > Build > macOS (dev)** or **Windows (dev)**. Output goes to `Builds/`.
2. Start the build **three times**. On macOS: `open -n Builds/macOS-dev/BlindRunway.app` three times.
   The game runs in the background, so unfocused windows keep simulating.
3. Window A: **PLAY > HOST A FLIGHT**. Note the join code (top right).
4. Windows B and C: **PLAY**, leave the code blank (same machine) or type the host's code, then **JOIN**.
5. Each window takes a different seat and presses **READY**. START stays disabled until 3/3 are ready.
6. Host presses **START**. Everyone presses **GOT IT** on their role intro. Check that:
   - [ ] only the Navigator window shows numbers
   - [ ] Navigator ping (R) makes the Pilot's gear switch glow
   - [ ] Cabin panic bar moves, and strapping lowers it
   - [ ] closing one client mid-flight sends everyone back to the lobby with a toast
   - [ ] Esc opens PAUSED; the flight keeps running
7. Finish the level. All three windows show the same result and blame cards. Host: RETRY or LOBBY.

## LAN play

The host shares the join code, which is their LAN IPv4 written as hex (for example `C0A8-0105` =
192.168.1.5). A raw IP also works in the join field. Allow UDP port **7777** through the firewall.
Internet play would need Unity Relay, which is not wired up.
