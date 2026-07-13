# T420 Static Audit

## Configuration ownership

- Enemy HP/tier/movement ID/speed/attack set/contact damage/score/resource/prewarm are mapped from `Enemies`.
- Armor and break-effect intent are mapped from `DefenseRules`.
- Weakpoint timing/radius/multiplier/interrupt/reward/VFX are mapped from `WeakpointRules`.
- Attack timing/gesture/interrupt/effect values are mapped from the selected `EnemyAttacks` row.
- Enemy Buff behavior consumes `Buffs`; the Skills adapter consumes values already supplied by T410 effects.
- No workbook, FieldDictionary, schema, exporter, DTO, generated JSON/hash/ConfigIds, scene, Prefab, package, Input Actions, ProjectSettings or Combat source remains changed.

## Architecture

- `EnemyStateMachine`, `EnemyDamageModel`, `EnemyBuffContainer`, definition/timeline factories and snapshots contain no `MonoBehaviour` dependency and are covered by EditMode tests.
- `Damageable`, `WeakpointController` and `EnemyController` are thin Unity lifecycle/adaptation layers.
- Actors references Config/Combat only. `EnemySkillEffectTarget` lives in Skills, so Actors does not reverse-depend on Skills.
- No enemy-specific subclass, reflection registry, managed thread, `Task.Run`, xlsx runtime read, scene/prefab YAML edit or direct WeChat SDK call was introduced.

## Lifecycle and boundaries

- States: None, Spawn, Move, Windup, Attack, Recovery, Stun, Dead.
- Attack/weakpoint/interrupt timing is derived from config and external monotonic timestamps; boundaries are tested from the loaded float-backed values rather than duplicate decimal constants.
- Repeated death, interrupt recovery and release are idempotent. Release clears damage, Buff, counter, weakpoint, attack and clock state before reuse.
- Armor absorbs first and overflows to HP; positive-to-zero armor break and death events publish once per lifecycle.

## Scope

- T430 movement/attack/defense/support strategies and Telegraph: not implemented.
- T440 generic object pool: not implemented.
- T450 enemy archetype assembly, T460 Boss phases, T510 battle flow, scene/prefab integration and platform work: not implemented.
