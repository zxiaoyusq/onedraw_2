# T420 Player Path

Test: `EnemyWeakpointCombatPlayModeTests.DiagonalMouseStrokeHitsConfiguredBossWeakpointAndInterruptsAttack`

1. Load the real Bootstrap scene and wait for the runtime configuration/registry path to reach MainMenu.
2. Spawn a generic `EnemyController` as `boss_tomb_king` with target ID `42001`; all HP, armor, attack-set and weakpoint values come from the runtime config.
3. Complete Spawn, start configured phase-1 attack `atk_boss_rockfall`, and advance its explicit simulation clock to `0.5s`, inside both the configured Boss weakpoint window and attack interrupt window.
4. Drive a real Input System Mouse diagonal stroke across the screen center.
5. Process the stroke through T300 pointer input, T310 sampling, T320 geometry, T330 gesture classification, T350 physics hit resolution and T360 damage calculation.
6. Resolve exactly one target, confirm `GestureType.Diagonal`, target `42001`, and `IsWeakpoint=true`.
7. Apply the resolved T360 result to the T420 enemy: configured armor changes `120→119`, interrupt status is `Interrupted`, state changes to `Stun`, and the weakpoint collider closes.

Result: PASS in PlayMode job `f0aed474f6b3462aacb344ce3eb863c2` and again in full PlayMode job `4d26df95c0374021a0b7dc4a263d2cfa`.
