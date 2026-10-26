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
