# TEST_PLAN：测试与证据

## 验证金字塔

| 层 | 范围 | 示例 |
|---|---|---|
| L0 结构 | 文件、asmdef、schema、静态扫描 | 字段漂移、硬编码数值 |
| L1 EditMode | 纯算法和公式 | RDP、闭合、方向、伤害、配置校验 |
| L2 PlayMode | Unity接线和生命周期 | Collider、输入、Prefab、对象池、场景状态 |
| L3 玩家路径 | 真实场景操作 | 教学、终极画符、Boss、重开 |
| L4 稳定性 | 压力、暂停、重复进入 | 10分钟战斗、重开3次、前后台 |
| L5 平台 | Web、转换、DevTools、真机 | 四级平台门 |

## 必须有的EditMode测试

- `StrokeSamplerBoundaryTests`
- `StrokeGeometryTests`
- `GestureClassifierTests`
- `StrokeHitResolverTests`
- `DamageFormulaTests`
- `SkillEffectPipelineTests`
- `EnemyStateMachineTests`
- `PoolResetTests`
- `EnemyArchetypeConfigTests`
- `BossPhaseTests`
- `SpawnTimelineTests`
- `ConfigValidationTests`
- `SaveMigrationTests`

## 必须有的PlayMode测试

- `PointerCancelPlayModeTests`
- `MultiTargetHitPlayModeTests`
- `ProjectileReflectPlayModeTests`
- `StanceSwitchPlayModeTests`
- `WaveRunnerPlayModeTests`
- `NoAdvanceBeforePlayerActionTests`
- `TutorialLevelE2EPlayModeTests`
- `BossLevelE2EPlayModeTests`
- `RestartThreeTimesPlayModeTests`
- `EnemyGalleryPlayModeTests`
- `BossBattlePlayModeTests`

## 证据模板

每个任务写`artifacts/evals/TASK-ID/verification.md`。模板真相源为`templates/verification.md`和`templates/change-whitelist.md`，可用下列命令初始化且不会覆盖已有证据：

```bash
Tools/CI/new-task-evidence.sh TASK-ID
```

Unity测试必须用`Tools/CI/run-unity-tests.sh`分别执行EditMode和PlayMode，并各自保存NUnit XML。脚本会解析XML并把测试失败、零测试、缺失或损坏结果转换为非零退出码。完整命令、日志卫生、标准Web入口和提交步骤见`docs/WORKFLOW.md`。

verification至少覆盖：

```markdown
# TASK-ID Verification
- Git与Unity追溯、任务范围和明确不做
- 预计白名单、实际改动和用户已有改动保护
- EditMode/PlayMode总数、通过、失败和XML路径
- 玩家路径、可断言值、Console和平台分层结论
- 已知问题及PASS / REVIEW / BLOCKED / KNOWN ISSUE结论
```

证据包含可断言值，例如HP变化、命中数、阶段、配置hash和帧率，不只写“看起来正常”。
