# T530 Change Whitelist

- Git基线：`7fb86e7cd60cbf10e34c1b85c9e3f3c159c0674a`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：把`lv_002_cave`扩展为约8波的配置驱动普通关，只用既有5种普通怪和1种精英怪形成可解的战术组合与递进难度，并证明节奏可只改表调整。
- 明确不做：不新增代码型敌人或产品玩法策略，不实现T540完整Boss关、T550结算、T600 HUD、T620表现、T630正式资源、T650教程UI，也不恢复T120/T130微信平台工作。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：只同步`lv_002_cave`的Levels/Waves/Spawns内容版本及必要README摘要，保持29 Sheet结构、字段和格式不变，镜像字节一致。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：只允许配置导出器根据正式工作簿生成的受管差异和样例镜像。
- `Assets/_Game/Tests/EditMode/T530/**`、`Assets/_Game/Tests/PlayMode/T530/**`：新增普通关8波结构、机制覆盖、可解组合、节奏递进和完整玩家路径测试及Unity生成的`.meta`。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/EditMode/T500/SpawnTimelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T500/WaveRunnerPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T520/TutorialLevelE2EPlayModeTests.cs`：仅同步配置版本、记录数、项目/普通关波次与出生冻结值，不改变旧任务规则。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ExporterCliTests.cs`、`ConfigPipelineE2ETests.cs`、`Fixtures/invalid-config-cases.json`：仅同步内容hash/ID计数冻结值，并在需要时隔离现有坏配置目标；不改导出或校验规则。
- `artifacts/evals/T530/**`：Git基线、工作簿前后审计/渲染、配置校验、Unity测试XML/日志、玩家路径和最终验证。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：只冻结T530普通关内容组合、难度曲线和可解性验收语义。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：验收通过后更新T530状态、证据、配置/测试统计与下一任务。

## 条件性白名单

- `Assets/_Game/Scripts/Levels/**`：只有现有T500/T510流程无法表达T530已配置内容时才允许最小通用修复；不得按关卡或敌人ID分支。
- `Tools/ConfigExporter/**`、`config/gameplay-config.schema.json`、`Assets/_Game/Scripts/Config/GameplayConfigDocument.cs`、`Assets/_Game/Scripts/Config/IConfigProvider.cs`：只有现有字段确实无法表达T530验收时才允许扩展，并必须同步完整Schema/FieldDictionary/导出/校验/DTO/测试闭环。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅当新测试需要当前未引用程序集时追加引用。

## 禁止改动

- 不修改`.unity`、`.prefab`、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不提前实现T540/T550/T600/T620/T630/T650；不添加新敌人、攻击、技能或策略类型。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
