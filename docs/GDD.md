# Blind Runway — Game Design Document (v1)

## One-line pitch
Three friends land a cursed plane together — but none of them has the full picture.

## Platform
- Steam (Windows primary, Mac secondary)
- Online co-op, exactly **3 players** (no more, no less for v1)
- Session length: 4–8 minutes per level
- Price target later: $6.99–$9.99

## Core fantasy
Miscommunication comedy + competence fantasy. When it works, you feel like a crew. When it fails, you blame each other and queue again.

## Roles (asymmetric information)

### Pilot
- **Can:** pitch/yaw/throttle (simplified arcade flight), flaps, gear toggle, callouts via emote/voice
- **Cannot:** see altitude, airspeed, heading, ILS needles, fuel, warnings
- **UI:** dirty windshield view + haptic/audio only cues; huge “LISTEN TO NAV” prompt if silent too long

### Navigator
- **Can:** full instrument panel, runway diagram, hazard radar, wind, fuel, checklist
- **Cannot:** touch flight controls; can only shout / ping markers on Pilot’s view
- **UI:** glass-cockpit beauty — dark navy, cyan needles, amber warnings

### Cabin
- **Can:** walk cabin, strap passengers, shove cargo, put out fires, slap panic buttons
- **Cannot:** see outside or instruments (small porthole vibes only)
- **UI:** warm cabin lighting, chaos meters, passenger faces, cargo straps

## Win / Lose
- **Win:** wheels on runway + stop inside gate box before timer + cabin “secure” ≥ threshold
- **Fail:** crash, fuel empty, cabin panic 100%, or timer 0
- Soft fail comedy: passengers ejected, cargo spilled — still can win if landed

## Camera / tone
Stylized low-poly + hand-painted accents (BOMBANANA / friendslop readable). Big silhouettes. Readable chaos. No gore.

## Progression (now vs later)
- **Now:** Tutorial + Levels 1–5 (campaign)
- **Later:** Levels 6–30, Endless, Custom lobby modifiers

## Voice / chat
Steam voice + text. Mute role still gets emote wheel (for future Mute challenge mode).

## Accessibility
Colorblind instrument palettes, scalable UI, remappable keys, screen shake toggle.
