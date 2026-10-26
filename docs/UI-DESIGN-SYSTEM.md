# UI Design System — Blind Runway

## Brand
- **Name lockup:** BLIND RUNWAY — condensed sans, tracking wide
- **Tagline:** “Three seats. Zero shared truth.”
- **Mood:** night ops + comedy — dark glass, cyan instruments, banana-warm cabin accents

## Color tokens
| Token | Hex | Use |
|-------|-----|-----|
| `bg.void` | `#0B1020` | Menus, nav panel chrome |
| `bg.panel` | `#141C2E` | Cards, modals |
| `accent.cyan` | `#3DE7FF` | Instruments, primary CTA |
| `accent.amber` | `#FFB020` | Warnings |
| `accent.danger` | `#FF3B5C` | Fail / fire |
| `accent.cabin` | `#FFD56A` | Cabin warmth / bananas |
| `text.primary` | `#F4F7FF` | Titles |
| `text.muted` | `#8B95B0` | Hints |
| `success` | `#3DFF9A` | Secure / landed |

## Type
- **Display:** Space Grotesk / similar geometric (TMP)
- **UI body:** Inter
- **Instruments:** JetBrains Mono / Share Tech Mono (readability)

## Screens (must all look “shipped”, not prototype gray)
1. **Boot splash** — logo + plane silhouette silhouette night sky
2. **Main menu** — Play / Levels / Settings / Quit — parallax clouds
3. **Lobby** — 3 role cards (Pilot / Navigator / Cabin), join code, Ready
4. **Role intro** — full-bleed role art + 3 bullet abilities + “Got it”
5. **In-level HUD** — role-specific (never show Pilot the numbers)
6. **Checklist overlay** (Navigator) — animated checkmarks
7. **Cabin panic bar** + passenger speech bubbles
8. **Success** — gate taxi celebration, star rating 1–3
9. **Fail** — freeze-frame crash comedy + “Why we died” blame cards (funny, not mean)
10. **Level select** — 5 nodes on a route map (airport lights)

## Motion
- CTAs: 120ms ease-out scale
- Warnings: pulse amber 1.2s
- Role cards: stagger in 80ms
- Landing success: camera dolly + confetti (tasteful, not casino)

## Layout rules
- Safe margins 48px desktop
- All primary actions bottom-right or center — thumb/streamer readable
- Never put critical Pilot numbers anywhere on Pilot HUD
