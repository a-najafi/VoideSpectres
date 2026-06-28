# Ship Navigation & Custom-Part Physics — Design Journey

This document records how VoideSpectres approaches **ships built from custom parts**, how we simulate their movement with thrusters, and what we learned while building the space-sector autopilot. It is written for future us: why the system looks the way it does, and what we are *not* trying to build.

---

## 1. What kind of game this is

VoideSpectres is a **simulation game for entertainment**, not a physics reference model.

The simulation should go **as far as the player can see and feel**:

- Adding, moving, or changing thrusters should clearly change how the ship handles and how fast it burns fuel.
- Ship size, mass, shape, and thruster placement should matter in ways the player can reason about.
- The player should be able to **predict outcomes** before committing to a maneuver—not because we model every real-world detail, but because **what we show in a plan is what will happen**.

We are **not** building:

- A high-fidelity orbital mechanics sandbox for engineers.
- A physics engine where open-loop control and closed-loop reality diverge and the player learns to distrust the UI.
- A reactive AI pilot that “fights” the ship every frame and produces spirals, freezes, or mystery behavior.

We **are** building:

- A **plan-and-execute** flight system where the green preview path is a contract.
- Physics that is **simple enough to plan cheaply** and **aligned enough with runtime** that execution matches the preview.
- Replanning only when **something outside the current plan** enters the world—not because our own pilot and physics disagree.

> **Core principle:** The physics during execution must be **100% what we thought it would be when we planned**—inputs *and* results. Only unplanned external events (another entity appearing, a collision, fuel cutout, etc.) should invalidate the plan and trigger a new one.

---

## 2. How a ship is made from parts

A “ship” in the sector is not one monolithic rigid body with hard-coded stats. It is a **root entity** plus **child part entities** wired together through components.

### 2.1 Structure

```
Ship (root entity)
├── ShipAggregateComponent      ← rebuilt each frame from parts
├── SpacePosition / Orientation / Move
├── ShipNavigationGoalComponent ← where autopilot wants to go
├── ShipManeuverPlanComponent   ← preview path + control timeline
├── ShipPlanExecutionComponent  ← elapsed time, plan id, status
└── …

Part entities (engines, thrusters, hull, cockpit, …)
├── ShipPartComponent           ← ParentShip → root
├── LocalTransformComponent     ← position/orientation on the hull
├── GeometryVolumesComponent    ← size / collision / volume
├── MassSourceComponent         ← mass contributed to the ship
├── ThrusterComponent           ← (optional) thrust, ramp, fuel rate
└── GimbalThrusterComponent     ← (optional) gimbal axis & angle
```

**Configuration:** `ShipPartsConfigComponent` lists part archetypes and local placements. `ShipPartFactory` expands that into live entities. `ShipAggregateSystem` rolls part mass, volume, centre of mass, bounds, and inertia into `ShipAggregateComponent` and the root `MassComponent`.

### 2.2 Why this matters for movement

Every thruster’s effect depends on:

| Factor | Source |
|--------|--------|
| Thrust magnitude | `ThrusterComponent.MaxThrustNewtons` × power |
| Thrust direction | Part local transform + optional gimbal |
| Torque | Lever arm from thruster position to ship centre of mass |
| Ship response to force | Root `MassComponent` (from aggregated part mass) |
| Ship response to torque | `ShipAggregateComponent.ApproximateMomentOfInertia` |
| Fuel use | `FuelLitersPerSecondAtFullPower` × current power, via ship engine fuel tank |

So when the player moves a thruster, changes its stats, or adds mass elsewhere on the hull, **both** the allocator/plant model and the live force systems see the update—there is no separate “hand-tuned handling” layer.

### 2.3 Bootstrap and validation

`ShipEntityBootstrapUtility` ensures root and part components exist and logs **movement readiness**: thruster count, forward force capacity, torque authority, missing mass on parts, fuel module presence. This came directly from early debugging when ships with four rear mains **did not move** because mass was zero, torque was zero, or required navigation components were missing.

---

## 3. Runtime physics pipeline (space sector)

When autopilot is **not** driving the ship, or when we need the same math the planner mirrors, forces flow through a fixed system order:

```
P1  ShipPlanningSystem          → build/replan maneuver when goal or world changes
P2  ShipPlanExecutorSystem      → replay plan throttle timeline → TargetPower
P3  ShipControlAllocatorSystem  → (skipped during plan playback) wrench → throttles
P5  ThrusterGimbalSystem        → ramp gimbal angles
P8  ThrusterPowerSystem         → ramp CurrentPower toward TargetPower
P10 GravitySystem               → external acceleration (if GravityAffected)
P15 ThrusterForceSystem         → world force + body torque from each firing thruster
P18 FuelConsumptionSystem       → consume fuel; cut thrust if tank empty
    SpaceOrientationSystem      → integrate angular velocity → orientation
    SpaceMovementSystem         → integrate accumulated forces → velocity → position
```

**Thruster force** is applied in **world space** from each part’s live direction (including gimbal), but torque is accumulated in **body space** about the aggregated centre of mass—matching how off-centre mains and RCS feel different.

**Fuel** is a first-class consequence of design: higher `MaxThrustNewtons`, more thrusters, or longer burns show up in consumption. Empty fuel forces `TargetPower` to zero—an external plan invalidation the player can anticipate.

---

## 4. Journey: from “ship won’t move” to plan-and-execute

This section is the chronological story of the autopilot feature—not every commit, but the problems that shaped the architecture.

### Phase A — Ship won’t move

**Symptoms:** Four rear main thrusters, autopilot engaged, nothing happens.

**Causes we found:**

- Parts without `MassSourceComponent` → ship mass stayed **0** → `SpaceMovementSystem` ignored forces.
- Missing navigation/bootstrap components on the ship root.
- Thrusters producing **force but no torque** (inline with centre of mass) → ship could not rotate to point at the target.
- Thrusters not producing **forward (+Z body) force** → planner could not accelerate toward a world target.

**What we did:** Bootstrap utility, demo archetype validator, readiness logging, and explicit warnings for mass / torque / forward capacity.

**Lesson:** Custom-part ships fail silently unless aggregation and per-part archetypes are correct. Validation is part of the feature, not polish.

---

### Phase B — Reactive pilot (guidance heuristics)

**Goal:** Fly toward a target point in space (`ShipAutopilotTargetMB` sets `ShipNavigationGoalComponent`).

**First approach:** Frame-by-frame guidance—desired wrench from errors, allocate to thrusters, hope it converges.

**Symptoms:**

- Over-rotation, swinging, death spirals.
- Editor freezes (replanning huge simulations every frame).
- Sometimes no movement at all.

**Lesson:** A **reactive pilot + live physics** is hard to tune, hard to bound, and impossible to **show the player a path they can trust**. Heuristic control is the wrong primary architecture for our goals.

---

### Phase C — Plan-and-execute autopilot

**Goal:** Deterministic **plan → preview → execute** with a drawable green path; replan when the target moves.

**Architecture:**

```
Goal (target point)
  → ShipManeuverPlanner + ShipManeuverSimulator (forward sim)
  → ShipManeuverPlanComponent (preview samples + control timeline)
  → ShipPlanExecutorSystem (replay timeline)
  → Thruster TargetPower → existing physics systems
```

**Planner phases (simplified):** rotate toward bearing → accelerate → coast → flip → decelerate → hold.

**Plant model (`ShipPlantModel`):** Snapshot of the ship as a control matrix **B**—each thruster’s body-frame force and torque at full throttle, from live part transforms and gimbals. **Allocator (`ShipControlAllocator`)** solves for throttles: minimize ‖B·t − w‖² with 0 ≤ t ≤ 1.

**Preview:** `ShipAutopilotTargetMB` draws the polyline and orientation ticks from `PreviewSamples`.

**Early wins:** Visible path, bounded simulation time, no per-frame full replan on unchanged goals.

---

### Phase D — Plan and reality diverged (the spiral)

**Symptoms:** Ship followed the path briefly, then spiraled or drifted off the green line.

**Root causes (plan ≠ live):**

| Issue | Why it hurt |
|-------|-------------|
| Body-frame wrench replay | Small attitude error rotated thrust into the wrong world direction every frame. |
| Frozen plant at plan time | Live sim rebuilt **B** with moving gimbals; planner used a snapshot. |
| Different allocator settings | Planner used fewer iterations than runtime → different throttles for same wrench. |
| Different timestep | Planner used coarse dt; runtime used ~16 ms variable dt. |
| No gravity in forward sim | Planned coasting ignored pull from gravity sources. |
| Replan only on target move | Tracking error grew without correction. |

**Lesson:** For our game, “close enough” open-loop control is **not** close enough. The player sees the green line—that line is a **promise**.

---

### Phase E — Aligning plan with execution (current approach)

We implemented four concrete fixes:

1. **World-frame control intent** — Store desired force/torque in **world space** on the plan timeline so attitude drift does not corrupt thrust direction during replay (throttles remain primary).
2. **Throttle playback** — Record per-thruster throttles from the same allocator used in planning; executor sets `TargetPower` directly; runtime allocator **skips** during playback.
3. **Richer forward sim** — Mirror gimbal ramp, gravity snapshot, same allocator iteration count, configurable `PlanSimDeltaTime` (default 1/60 s).
4. **Periodic / error-triggered replan** — Replan from **current state** on interval or when position/attitude error vs preview exceeds thresholds—not only when the target GameObject moves.

**Replan triggers today (`ShipNavigationGoalComponent`):**

- Target moved beyond `ReplanPositionThreshold`
- `ReplanIntervalSeconds` elapsed during execution
- Cross-track error vs preview ≥ `ReplanTrackingErrorThreshold`
- Attitude error vs preview ≥ `ReplanAttitudeErrorDegrees`

**External invalidation (by design, future-hardening):**

- Fuel exhaustion mid-burn
- Collision / impulse from another entity
- Gravity field change (new source, teleported mass)
- Player manual override

When any of these occur, we **discard the old contract**, simulate forward from **now**, and show a **new** green path.

---

## 5. What we learned about thrusters

### 5.1 Thrusters are not “engines” in the abstract

Each thruster is a **part** with:

- A **direction** (part mount + gimbal),
- A **location** (lever arm → torque),
- **Ramp** (spool-up time),
- **Fuel rate** at full power.

Main engines and RCS can share the same component type; behavior comes from placement and stats.

### 5.2 Control allocation is mandatory for custom layouts

With four mains on the back, there is no simple “forward throttle = go” mapping. The ship needs a **wrench solver** (force + torque in body frame → per-thruster throttles). The same **B** matrix is used in:

- Live flight (`ShipControlAllocatorSystem`) when not on plan playback,
- Planning simulation (`ShipManeuverPlanner` / `ShipManeuverSimulator`).

### 5.3 Rotation and translation are coupled

Autopilot must **rotate before it burns**, **coast** when appropriate, and **flip for retro** before decelerating. Skipping torque authority checks produces ships that “have thrust” but never point where needed.

### 5.4 Gimbals and aggregation matter in the plant

`ShipThrustModel.BuildRows` walks all `ShipPartComponent` children with `ThrusterComponent`, using live gimbal angles. The planner’s forward sim updates gimbal state and rebuilds effectiveness each step so the plan does not assume frozen nozzles while runtime moves them.

### 5.5 Fuel is part of the player-facing sim

Fuel is not decorative. It feeds back into physics (`FuelConsumptionSystem` cuts thrust). Longer plans, heavier ships, and more/larger thrusters should **feel** expensive. That belongs in the same “predictable contract” story: if fuel runs out mid-plan, that is an **external** event → replan or abort.

---

## 6. Design philosophy: predictable physics for players who build ships

### 6.1 The contract model

| Layer | Player sees | System guarantees |
|-------|-------------|-------------------|
| **Design** | Parts, mass, thruster layout | Aggregate + plant model update immediately |
| **Intent** | Target marker, arrival radius, max speed | Goal component |
| **Plan** | Green path + orientation ticks | Forward sim with same rules as execution |
| **Execute** | Ship follows path | Throttle timeline replay through real physics systems |
| **Disturbance** | Something unexpected | Replan from current state; new green path |

The player should think: *“If nothing else happens, that path is where I go.”*

### 6.2 Sim depth vs stability

We deliberately limit simulation depth to what supports **feel** and **planning**:

- **In scope:** Mass, COM, inertia approximation, thruster forces/torques, ramp, gimbal, gravity from sector sources, fuel drain.
- **Out of scope (unless it becomes visible):** High-precision multi-body orbit propagation, atmospheric drag, structural flex, turbopump lag, etc.

Depth is added only when the player can **see it on the path** or **feel it in handling**, not when it makes plans untrustworthy.

### 6.3 How players feel design choices without chaos

| Player action | What they should feel | How the system supports it |
|---------------|----------------------|----------------------------|
| Add rear mains | Faster accel, more fuel burn | ↑ forward force capacity in plant; preview shortens |
| Move thruster off-centre | Stronger pitch/yaw, maybe weaker pure forward | Torque row changes; rotate phase length changes |
| Add heavy hull | Sluggish translation and rotation | ↑ mass, ↑ inertia in aggregate; longer plan |
| Add RCS pods | Crisper pointing | ↑ torque authority; faster rotate segments |
| Remove fuel / add consumption | Shorter useful burn | Fuel system cuts thrust; triggers replan or stop |
| Drag target in scene | New path immediately | Goal change → replan → new preview |

**Stability comes from plan-and-execute**, not from hiding physics from the player. They get depth **because** the preview updates when they change the ship or the target.

### 6.4 When replanning is correct

Replanning is **not** a failure—it is how we keep the contract honest:

- **Good replan:** Target moved, tracking error exceeded, timer elapsed, fuel gone, foreign force applied.
- **Bad replan (what we removed):** Every frame because pilot and physics disagree.

The ideal loop:

```
[Stable world] → Plan → Execute → Arrive
                      ↓
              [External change] → Replan from NOW → Execute → …
```

---

## 7. System map (reference)

```
ShipAutopilotTargetMB (Unity)
        ↓  TargetPoint, Mode, speeds
ShipNavigationGoalComponent
        ↓
ShipPlanningSystem
        ↓  TryBuildPlan
ShipManeuverPlanner ──→ ShipManeuverSimulator ──→ ShipGravityModel
        │                      ↑
        └── ShipPlantModel ←───┘ (B-matrix from parts)
        ↓
ShipManeuverPlanComponent  (PreviewSamples + ControlSamples)
        ↓
ShipPlanExecutorSystem     (throttle playback)
        ↓
ThrusterPowerSystem → ThrusterForceSystem → SpaceOrientation / SpaceMovement
        ↑
FuelConsumptionSystem, ThrusterGimbalSystem, GravitySystem
```

**Key files:**

| Area | Path |
|------|------|
| Parts & aggregate | `Gameplay/Ship/Parts/`, `ShipAggregateSystem` |
| Thrusters | `Gameplay/Ship/Thrusters/`, `ThrusterForceSystem` |
| Navigation | `Gameplay/Ship/Navigation/` |
| Systems | `Gameplay/Ship/Systems/ShipPlanningSystem`, `ShipPlanExecutorSystem`, … |
| Demo target | `VoidSpectreUnity/Demo/ShipAutopilotTargetMB.cs` |
| Bootstrap | `Gameplay/Ship/Bootstrap/ShipEntityBootstrapUtility.cs` |

---

## 8. Open directions (not blockers)

These follow from the philosophy above; they are not required to understand what we built today.

1. **Explicit plan invalidation events** — Central “disturbance bus” (collision, fuel, tractor beam) instead of only heuristic replan thresholds.
2. **Player manual flight** — Same plant/allocator; optional switch off autopilot without a different physics path.
3. **Interior vs sector** — This journey focused on **space sector** movement; interior locomotion may use simpler kinematic rules if the player never needs orbital preview there.
4. **Fuel on the preview** — Show estimated Δv or fuel cost along the green path so design trade-offs are visible before committing.
5. **Stricter determinism** — Fixed tick for plan sim and runtime sector step when we need bit-identical replay (e.g. replays, networking).

---

## 9. Kinodynamic refactor (current architecture)

Planning and execution now share a **single propagation function**: `ShipStepSimulation.StepShipSim`.

| Concept | Type / system |
|---------|----------------|
| Ship state | `ShipSimState` (position, velocity, orientation, thrusters, fuel, mass snapshot) |
| Controls | `ShipControlInput` (per-thruster target power + gimbal targets) |
| Plan storage | `ShipPlanTick[]` — tick index, controls, expected state after `StepShipSim` |
| Context | `ShipContextSnapshot` — plant, gravity, fuel rates, validity hash |
| Planner (default) | `ShipBeamSearchPlanner` over ship-specific `ShipManeuverPrimitive`s |
| Planner (legacy) | Phase FSM when `ShipNavigationGoalComponent.UseLegacyPhasePlanner` is true |
| Validation | `ShipValidator` (general readiness) + `GoalReachabilityValidator` (per-goal) |
| Performance | `ShipPlanningLodComponent` tiers + `ShipPlanningBudgetSystem` per-frame cap |
| Execution | `ShipPlanExecutorSystem` replays plan ticks through `StepShipSim` at fixed dt |

**Contract:** the green preview path is the recorded output of the same `StepShipSim` used at runtime. Tracking error triggers replan; reactive steering does not fight the plan during playback.

**Contract tests:** enable `Run Navigation Contract Tests` on `GameBootstrapMB` or call `ShipStepSimulationContractTests.RunAll()`.

---

## 10. One-paragraph summary

VoideSpectres treats ships as **aggregated custom parts** whose thrusters produce force and torque through a shared control plant. We tried reactive piloting and learned it cannot give players a **trustworthy preview**. We moved to **plan-and-execute**: forward-simulate with the same allocator, timestep policy, gimbals, and gravity we execute, store **world intent and throttle timelines**, draw the path, replay it through real physics, and **replan only when the goal or the world changes**. The simulation is deep enough that **layout, mass, and thrusters matter** for handling and fuel, but bounded enough that **what we plan is what happens**—which is exactly what an entertainment sim needs when the player is the engineer.
