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
- `BattleFlowTests`
- `TutorialFlowTests`
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
- `NormalLevelE2EPlayModeTests`
- `BossLevelE2EPlayModeTests`
- `RestartThreeTimesPlayModeTests`
- `EnemyGalleryPlayModeTests`
- `BossBattlePlayModeTests`

T510专项必须覆盖：配置倒计时到Playing的delta切分；统一时间缩放精确到期；Countdown暂停保留进度；FocusLost/ApplicationPaused叠加后完整恢复；Ultimate输入窗包含边界、严格超时只取消、旧gestureEventId不能重放；大delta不能跨PlayerConfirmed；同帧死亡/到时/完成只产生一次互斥结算。PlayMode从Bootstrap真实配置路径验证生命周期与有效终极事件，不能只构造表外设置。

T520专项必须覆盖：正式配置映射6步/6波/15怪与180秒上限；错误触发、未来完成事件和计时器单独推进均不改变步骤；正确动作可在最短展示前锁存并于边界完成；`StrokeHitCount>=3`严格拒绝2并接受3；Active步骤阻止波次结算而Waiting步骤不阻塞。PlayMode必须从Bootstrap真实配置走完普通斩、弱点、同笔三目标、切弹、实际架势切换及配置Circle终极，断言6次开始、6次完成、1次教程完成、15次出生、能量实际扣除和最终Victory。

T530专项必须覆盖：正式配置映射`lv_002_cave`的8波、23条出生行、45个敌人、六种非Boss原型和第5/7/8波精英修饰请求；四个双波战术段人口递进，`maxAlive`足以承载配置组合，不同架势危险目标至少错开1秒。EditMode必须用内存配置变体证明出生时间、数量与容量只改表即可改变重载结果；PlayMode必须从Bootstrap真实配置经T500/T510流程实际出生并击败45怪，覆盖投射物、冲撞、近战和支援动作，断言210秒内Victory且敌人池活动租约为0。

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
