# Face Snatchers Project Plan (72-hour game jam)

## One-Sentence Summary
A top-down, dark city arena where four snatchers possess hosts by shooting mask projectiles miss and you die; stay too long and you die; last snatcher alive wins.

---

## Current Technical Foundation
- Unity top-down scene with city streets and boundary walls.
- NavMesh baked from environment geometry.
- `WanderPoints` root with child empty points.
- Hosts:
  - `NavMeshAgent`
  - `HostWander` that reserves `WanderPoint`s (capacity 1) and moves between them.
- Spawning:
  - `HostSpawner` spawns hosts at unique wander points and assigns host `zone` from point `zone`.

---

## Game Entities & Responsibilities

### Host (capsule)
- Physical body in the world.
- States (conceptual; can implement gradually):
  - Neutral (wander)
  - Possessed (controlled by snatcher)
  - Claimed (colored by snatcher, cannot be re-possessed by same snatcher)
  - Dead (optional; could disable renderer/collider)
- Has NavMeshAgent for movement (both wandering and snatcher-driven movement).

### Snatcher Slot (not necessarily a world object)
- Logical controller for one snatcher:
  - Color
  - Alive/dead
  - Current host reference
  - Stats: snatches, kills, survival time
  - Control mode: Human or AI
- Each snatcher slot has a dedicated camera (split-screen quadrant).

### WanderPoint
- Empty GO used for wandering and spawning.
- Fields:
  - `int zone`
  - reservation/capacity (default 1)

---

## Cameras
- 4 cameras, fixed viewport rects for 2x2 split.
- Each camera uses a follow script and tracks its snatcher slots `currentHost`.
- Never parent camera to host.

---

## Core Gameplay Rules (must ship)
1. 4 snatchers, distinct colors.
2. Start already in a host.
3. Shoot a mask projectile to possess:
   - Hit host  possess
   - Hit occupied host  kill snatcher, take host
   - Miss  shooter dies
4. Host decay timer:
   - Expires while possessed  host dies and snatcher dies
5. Leaving host:
   - Host survives, becomes claimed (snatcher color, no mask)
   - Shooter cannot re-possess that host again
6. Game ends when only one snatcher remains alive.
7. Track score stats (snatches/kills/time) and show at end.

---

## 72-Hour Milestones

### Milestone 1  Stable world simulation (done/near done)
- NavMesh bake correct (ignores players/hosts).
- Hosts spawn at wander points (unique).
- Hosts wander within their zone, using point reservation.

### Milestone 2  Snatcher slot + cameras (must have)
- Create 4 snatcher slots.
- Each slot owns:
  - color, alive flag, current host reference
- 4 split-screen cameras follow each slots current host.

### Milestone 3  Possession shooting + death rules (must have)
- Mask projectile:
  - Spawn from current host
  - Collision logic for host hit vs miss
- Transfer possession:
  - Update snatcher slot current host
  - Apply visuals (color + eye mask)
- Miss = immediate snatcher death (and stop its camera following? keep camera on corpse/last host).

### Milestone 4  Host decay timer (must have)
- Per-snatcher time in current host.
- On expiration:
  - kill snatcher
  - optionally kill host / mark dead

### Milestone 5  AI snatchers (must have)
- Simple loop:
  - find nearby neutral host
  - shoot only when safe (close range)
  - rate-limit shots
- Difficulty knobs:
  - accuracy, max shot distance, reaction delay

### Milestone 6  End condition + scoring (must have)
- Detect last snatcher alive.
- End screen:
  - winner
  - snatches/kills/time per slot

### Stretch (only if time)
- Dash to evade nearby snatcher.
- AI risk escalation (more aggressive later / if losing).
- Better VFX/audio polish.
- Claimed host behaviors (optional variations).

---

## Tuning Notes / Known Risks
- If too many hosts choose long paths, traffic concentrates; zoning solves this.
- If destinations overlap, clumping occurs; reservation solves this.
- Keep alleys wide enough for agent radius; tune agent radius/stopping distance.

---

## Definition of Playable
- Four cameras show four active snatchers.
- Human can move and shoot to possess.
- AI can possess and kill.
- Misses cause death reliably.
- Host decay causes forced hopping.
- Match ends correctly with a winner and score summary.
