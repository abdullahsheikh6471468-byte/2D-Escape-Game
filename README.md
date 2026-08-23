# Escape: The Last Exit

A 10-level top-down escape/adventure game built in C# with MonoGame.

**Author:** Abdullah

---

## About the Project

The player is trapped inside a series of dangerous environments and must
find keys, avoid traps and enemies, and reach the exit to escape — level
by level, all the way to the Ultimate Escape.

## Story / Theme

Each level drops the player into a new location, each harder and more
dangerous than the last:

1. **Tutorial Escape** — a quiet house, one key, one door
2. **Locked Rooms** — multiple rooms, multiple keys
3. **Trap House** — traps enter the picture
4. **Enemy Facility** — patrolling enemies appear
5. **Underground Escape** — a larger, tunnel-like layout
6. **Security Base** — a sci-fi facility with tougher security
7. **Dark Escape** — the lights go out; only a small area around the
   player is visible
8. **Prison Escape** — a large, guarded layout
9. **Final Facility** — everything gets harder at once
10. **Ultimate Escape** — the final, hardest run to the last exit

## Features

- **10 levels**, each with a unique room layout, objective, and theme
- **Player** with smooth 4-direction movement, collision, and health
- **Colored keys & doors** — normal, locked (needs the matching key), and
  exit doors
- **Traps** — spikes, moving blocks, timed lasers, and fire/lava zones
- **Enemies** that patrol, detect the player, and give chase
- **HUD** showing level, health, keys collected, objective, and a
  countdown timer
- **Mini-map** showing the player, enemies, items, and the exit
- **Main Menu → Level Select → Gameplay → Level Complete → Next Level**
  flow, with Pause and Game Over screens and a final Game Complete screen
  after level 10
- **Level unlocking** — levels unlock as you complete the one before them,
  with progress saved between sessions
- **Scoring** — time, health, and objective bonuses, with a running total

## Art & Sound

Built using a custom top-down dungeon/facility art pack — hand-picked
tiles, doors, traps, keys, and character sprites — plus reused sound
effects for pickups, damage, and level completion.

## Tech

Built in **C#** using **MonoGame**, structured around a screen-manager
architecture (Main Menu, Level Select, Gameplay, Pause, Settings, About)
with a procedural level generator that builds each level's rooms, keys,
doors, traps, and enemies.
