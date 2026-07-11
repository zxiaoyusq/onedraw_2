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
- `BossPhaseTests`
- `ConfigValidationTests`
- `SaveMigrationTests`

## 必须有的PlayMode测试

- `PointerCancelPlayModeTests`
- `MultiTargetHitPlayModeTests`
- `ProjectileReflectPlayModeTests`
- `StanceSwitchPlayModeTests`
- `PoolResetTests`
- `WaveRunnerPlayModeTests`
- `NoAdvanceBeforePlayerActionTests`
- `TutorialLevelE2EPlayModeTests`
- `BossLevelE2EPlayModeTests`
- `RestartThreeTimesPlayModeTests`

## 证据模板

每个任务写 `artifacts/evals/TASK-ID/verification.md`：

```markdown
# TASK-ID Verification
- 日期：
- Git commit/branch：
- Unity精确版本：
- SDK/DevTools/设备：
- 范围与明确不做：
- 修改文件白名单：
- 配置版本/hash：
- EditMode：总数/通过/失败：
- PlayMode：总数/通过/失败：
- 玩家路径与观察值：
- Console新增Error/Warning：
- 性能/包体：
- 截图/日志：
- 已知问题：
- 结论：PASS / REVIEW / BLOCKED / KNOWN ISSUE
```

证据包含可断言值，例如HP变化、命中数、阶段、配置hash和帧率，不只写“看起来正常”。
