# T460 Change Whitelist

- Git基线：`2bf7fe1fce829839542dfdb5075d67fea4983c4c` (`main`)。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：实现配置驱动Boss阶段、连续HP阈值、攻击/速度/护甲/弱点覆盖、进入效果和一次性切换，并完成镇墓玄甲王三阶段玩家路径。
- 明确不做：不实现T500关卡时间轴、T510战斗流程、T540完整Boss关、T630正式美术或T120/T130微信平台工作；不修改场景、Prefab、Registry、Packages、ProjectSettings或微信SDK。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`及`config/一笔镇妖_游戏配置表模板.xlsx`：content升级到`0.5.1-sample`，新增Boss二/三阶段移动模板并由BossPhases引用；保持字节一致镜像。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：由同一正式工作簿重新生成并保持同源。
- `Tools/ConfigExporter/Tests/{ExporterDeterminismTests,ConfigPipelineE2ETests,ExporterCliTests}.cs`及必要配置说明：同步冻结hash、ID数量和内容版本断言；不改Schema、FieldDictionary、DTO或导出算法。
- `Assets/_Game/Scripts/Actors/{EnemyDefinition,EnemyMovementStrategy,EnemyDamageable,EnemyStateMachine,EnemyController,EnemyStrategyRuntime}.cs`：提供阶段配置覆盖、阶段换防、攻击状态取消和按当前定义重建策略的通用能力，不硬编码Boss阈值或数值。
- `Assets/_Game/Scripts/Actors/BossPhaseDefinition.cs`及`.meta`：新增无MonoBehaviour依赖的阶段目录、连续阈值校验和纯状态机。
- `Assets/_Game/Scripts/Skills/BossPhaseController.cs`及`.meta`：接入EnemyController、T430策略与T410进入效果，管理阶段生命周期及一次性事件。
- `Assets/_Game/Tests/EditMode/T460/**`和`Assets/_Game/Tests/PlayMode/T460/**`：新增`BossPhaseTests`、`BossBattlePlayModeTests`及Unity生成`.meta`。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`：同步新content/hash/记录数/ID数的冻结运行时断言。
- `artifacts/evals/T460/**`：基线、工作簿审查、测试XML、Unity/配置日志和最终验证证据。
- `config/README.md`、`Tools/ConfigExporter/README.md`、`docs/CONFIG_PIPELINE.md`、`docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：同步T460配置/运行时合同。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验收通过后更新T460状态、证据、计数和下一任务。

## 禁止改动

- 不改变29 Sheet、FieldDictionary、JSON Schema、DTO、枚举、导出器算法或AssetManifest；若实现中证明字段合同不足，必须先修订本白名单并完成全配置闭环。
- 不修改`.unity`、`.prefab`、`.asset`、Input Actions、Packages、ProjectSettings、微信SDK或Builds；不提前实现T500及后续流程。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
