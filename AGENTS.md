# Face Snatchers - Codex Agent Notes

This repository is a Unity game jam project for Global Game Jam. The goal is to ship a playable build in ~72 hours, so prefer pragmatic, minimal changes over perfect architecture.

## Project Snapshot
- Engine: Unity `6000.0.28f1`
- Main scene: `Assets/Scenes/Scene01.unity`
- View: top-down, dark/shadowy city, single arena with walled boundaries
- Characters: capsule hosts/snatchers (everything exists at start; no runtime spawn/despawn except projectiles/FX)

## Authoritative Gameplay Rules
- 4 snatchers total, each with a distinct color.
- Game starts with each snatcher already inhabiting a host body.
- Possessed body visuals: snatcher color + black eye mask.
- Neutral hosts wander the city.
- Snatcher jump is a mask projectile (fast shot).
  - If a snatcher shoots and misses (hits wall/ground/anything non-host): snatcher dies immediately.
  - If projectile hits a neutral host: snatcher possesses that host.
  - If projectile hits a host currently inhabited by another snatcher: that snatcher dies, attacker possesses the host.
- Host decay: stay in a host too long -> host dies and you die (forces hopping).
- Leaving a host:
  - Host does not die when you leave.
  - You cannot re-enter the same host again.
  - Host becomes claimed and changes to your color (no mask).
  - Claimed hosts count toward your snatched score.
- Win condition:
  - Game ends when only one snatcher is alive.
  - Track stats (e.g., #snatches) for end screen; last alive wins.

If unsure, follow these rules as the source of truth and update both code and documentation when changing gameplay.

## Cameras / Presentation
- Always 4-way split screen (2x2), regardless of player count.
- Each camera follows one snatcher slot (current host).
- Do NOT parent cameras to hosts; use a follow script targeting the host transform.
- Only one AudioListener enabled.

## Player / AI Configuration
- Default: 1 human + 3 AI (free-for-all).
- Stretch: allow all AI mode.
- Optional: difficulty per AI; never teams.

## Key Runtime Scripts
- `Assets/Scripts/HostSpawner.cs`
  - Spawns hosts at unique `WanderPoint` objects under `WanderPoints`.
  - Assigns `HostWander.zone` from the spawn point.
  - Can assign `FaceSnatcherCamera` targets by zone and enable human control.
- `Assets/Scripts/HostWander.cs`
  - NavMesh wandering for hosts.
  - Reserves `WanderPoint` targets and only wanders within its `zone`.
- `Assets/Scripts/WanderPoint.cs`
  - Zone + reservation capacity for wander targets.
- `Assets/Scripts/FaceSnatcherCamera.cs`
  - Smooth follow + look-at for assigned target.
- `Assets/Scripts/FaceSnatcherHumanController.cs`
  - Simple WASD/arrow movement and space to fire mask projectile.
- `Assets/Scripts/MaskProjectile.cs`
  - Simple forward-moving projectile with lifetime.

## NavMesh / Host Wandering
- NavMesh is baked from level geometry only (exclude players/hosts via layer mask).
- Scene contains a `WanderPoints` root with child points.
- Each `WanderPoint` has a `zone` (int) and reservation (capacity 1 by default).
- `HostSpawner` spawns at unique wander points and assigns host zones.
- `HostWander` chooses available points only within matching zone.

## Implementation Priorities
1. Playable core loop: wandering hosts + possession + miss = death + host decay + win condition.
2. Split-screen cameras following each snatcher slot.
3. AI snatchers (simple target selection + rate limiting + difficulty knobs).
4. UI/HUD polish and scoring summary screen.
5. Dash / advanced AI behaviors only if time remains.

## Working Conventions
- Keep scripts small and composable; prefer Unity built-ins.
- Avoid heavy refactors or new frameworks.
- Determinism where it matters (spawning/assignment); randomness is OK for wandering/AI tuning.
- Add debug logs only when needed; remove spam before final build.

## Agent Guidance
- Make targeted changes with minimal disruption.
- Prefer reliable behavior over cleverness.
- When changing gameplay rules, update code and documentation together.
