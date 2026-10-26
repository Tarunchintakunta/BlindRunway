# QA smoke test: Blind Runway

## Status (be honest here)

| Check | Result |
|---|---|
| Compiles in Unity | Yes. Unity **6000.6.7f1** batchmode, no errors or warnings from project code. |
| EditMode tests (`Tests/EditMode`) | **30/30 pass** in Unity: flight model (incl. crosswind and the L05 fuel budget), cabin, rules, blame cards, progression, glitch privacy, join codes, and the real L01–L05 assets loading. |
| PlayMode smoke (`Tests/PlayMode`) | **1/1 pass** in Unity: boot, host over Netcode, SOLO TEST, intro, flight with live snapshots, swap all three seats, leave. |
| Three separate clients over the network | **Pass (automated).** `scripts/smoke-3clients.sh` runs three macOS player processes (host/Pilot, Navigator, Cabin) on 127.0.0.1 through lobby → ready → start → intro → flight. Each seat confirms it gets only its own data. The first run caught a race in the driver, now fixed. |
| Full human playthrough of L1–L5 | **Not done.** Feel, balance and the visuals still need a person at three keyboards (checklist below). |

The commit `feat(net): seat ownership rules for role input` says "Playmode smoke: host + 2 clients reach
lobby…". That claim was premature: no playmode run existed when it was written. The real-editor runs above
came later.

Re-run from a terminal:

```bash
U=/Applications/Unity/Hub/Editor/6000.6.7f1/Unity.app/Contents/MacOS/Unity
$U -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults edit.xml -logFile -
$U -batchmode -projectPath . -runTests -testPlatform PlayMode -testResults play.xml -logFile -
```

Automated three-client run:

```bash
# Blind Runway > Build > macOS (dev), then:
scripts/smoke-3clients.sh   # prints [SMOKE] PASS for seats 0,1,2; exit code 0
```

## First open

1. Install **Unity 6000.6.7f1** (the version in `ProjectSettings/ProjectVersion.txt`) through Unity Hub.
2. Unity Hub > **Add project from disk** > this repo's root folder.
3. Wait for packages to resolve: Netcode for GameObjects 2.13.3, Unity Transport, URP 17.6, uGUI,
   Test Framework (pinned in `Packages/packages-lock.json`).
4. Optional: menu **Blind Runway > Setup URP Pipeline**. The game also renders on the built-in pipeline.
   Keep *Project Settings > Player > Active Input Handling* on **Input Manager (Old)** or **Both**.
5. **Window > General > Test Runner > EditMode > Run All**. Expect all tests green.

Netcode 2.1.1 does not compile on 6000.6; 2.13.3 does. Don't downgrade.

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
