# Architecture — Unity 6 + C#

## Stack
- Unity 6 LTS, URP
- C# game code
- Netcode for GameObjects + Unity Transport (host-migratable host model for v1)
- Input System package
- Cinemachine for cameras
- TextMeshPro for UI

## Repo layout (create on main via microcommits)
```
BlindRunway/
  Assets/
    _Project/
      Art/           # meshes, materials, textures
      Audio/
      Prefabs/
        Roles/
        Plane/
        Cabin/
        UI/
      Scenes/
        Boot.unity
        Lobby.unity
        Level_01.unity ... Level_05.unity
      Scripts/
        Core/        # GameSession, WinLose, Timer
        Net/         # NetworkBootstrap, PlayerSpawner
        Roles/       # PilotController, NavigatorHud, CabinAgent
        Plane/       # FlightModel, Instruments, Gear
        Cabin/       # Passenger, Cargo, PanicMeter
        UI/          # Menus, HUD, SharedWidgets
      ScriptableObjects/
        Levels/      # LevelDefinition assets for L01–L05
      Settings/      # URP, Input
  Packages/
  ProjectSettings/
  docs/              # copy from this zip
```

## Networking rules (v1)
- Exactly 3 player slots: Pilot, Navigator, Cabin — assigned in lobby
- Host is Player 0; clients join via join code
- Authority: Pilot owns plane rigidbody transforms; Navigator owns ping markers; Cabin owns cabin interactables
- Instruments are **server-authoritative values** replicated to Navigator only (Pilot HUD strips numbers)

## Flight model (arcade, not sim)
- Forces: thrust, lift proxy, drag, gravity
- Clamp extreme attitudes so comedy > realism
- Autostabilize lightly when no input (helps Pilot who’s flying blind)

## Save / meta
- Local PlayerPrefs for unlocks L1→L5
- Steam achievements later (stub interfaces now)

## Performance budget (M-series + low-end Windows)
- <80 draw calls cabin, <50k tris plane exterior target
- 60 fps target on integrated GPU

## As built (Act I)
Editor: Unity 6000.6.7f1, Netcode for GameObjects 2.13.3 (2.1.1 does not compile on 6000.6).

Where the shipped code differs from the plan above, and why:

- **One scene, everything code-built.** `Boot.unity` is empty; `Bootstrap` (RuntimeInitializeOnLoadMethod)
  creates the camera, UI canvases, NetworkManager and per-seat worlds. No Lobby/Level scenes and no prefabs,
  so nothing merges badly. Levels are data: `LevelDefinition` assets in `ScriptableObjects/Resources/Levels`.
- **Netcode without NetworkObjects.** Netcode for GameObjects + Unity Transport handle connections and
  approval (exactly 3). Gameplay runs over JSON named messages: clients send 20 Hz input for their seat, and
  the server simulates `Match` (FlightModel + CabinSim) at 50 Hz and sends **per-seat snapshots** at 20 Hz.
  This differs from the plan's "Pilot owns plane rigidbody": the server owns all state. That makes the
  ownership rules (a seat only drives its own inputs) and the information rules (only the Navigator's
  snapshot carries instrument numbers) one check each in `Session.OnInput` and `Match.SnapshotFor`.
- **Join code** = host LAN IPv4 as hex. Unity Relay is the upgrade path for internet play.
- **UI** is uGUI with the built-in font and code-generated rounded/circle/star sprites, coloured by
  `Tokens` from the design system. TMP with Space Grotesk/Inter/JetBrains Mono is the upgrade path once
  font assets are imported.
- **Input** uses the legacy Input Manager (zero setup). Keep *Project Settings > Player > Active Input
  Handling* on *Input Manager (Old)* or *Both*. Move to the Input System package for remapping.
- **No Cinemachine**: two hand-rolled cameras (chase for the Pilot, fixed tilting cabin view) were less code.
- **Audio** is synthesized at startup (`AudioBus`); replace with recorded clips and an AudioMixer later.
- **Save** is PlayerPrefs (`Progress`): clears, best stars, highest unlocked level. Steam achievements remain
  a later stub.
