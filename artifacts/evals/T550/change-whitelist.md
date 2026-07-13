# T550 Change Whitelist

- Git基线：`e43cf73ee70141c5777a5da0b1cd341797223fb8`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：实现配置驱动的最终评分/星级/奖励、幂等结算、ProgressSave v1与迁移接口、坏存档回退，以及Restart/NextLevel会话替换；验证连续重开3次无活动池租约或旧会话状态。
- 明确不做：不做云存档、付费货币或T600结算UI；不直接调用PlayerPrefs/微信SDK，不恢复T120/T130，不提前实现T600及后续任务。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：新增最终评分中弹反、无伤和剩余整秒的Global系数并升级content版本；保持29 Sheet、字段和样式不变，镜像字节一致。
- `outputs/T550/GameConfig.xlsx`及其工作簿检查输出：保存与正式源字节一致的T550可交付工作簿，满足工作簿工具的输出与复核合同；不形成第三套内容源。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：只允许正式工作簿经现有导出器生成的受管差异。
- `Assets/_Game/Scripts/Config/IConfigProvider.cs`、`GameplayConfigService.cs`、`GameplayConfigSnapshot.cs`：只增加稳定只读Levels枚举，供初始解锁根与关卡图验证使用；不改变DTO、Schema或索引所有权。
- `Assets/_Game/Scripts/Levels/ResultScoring.cs`、`ProgressSave.cs`、`ResultService.cs`、`BattleResultNavigation.cs`及Unity生成`.meta`：新增无MonoBehaviour依赖的配置评分、星级/奖励、版本化JSON存档/迁移、存储端口与会话替换规则。
- `Assets/_Game/Scripts/Levels/OneStrokeDemon.Levels.asmdef`：仅允许为ProgressSave JSON编解码增加现有受管Newtonsoft.Json的显式程序集引用。
- `Assets/_Game/Tests/EditMode/T550/**`、`Assets/_Game/Tests/PlayMode/T550/**`及目录`.meta`：新增ResultTests、SaveMigrationTests、结算/存档重载和连续Restart/NextLevel三次生命周期测试。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`：仅同步配置版本、hash、记录/索引/ID冻结值，并覆盖只读Levels枚举。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ExporterCliTests.cs`、`ConfigPipelineE2ETests.cs`：仅同步content hash与ID计数冻结值；不改变导出/校验规则。
- `artifacts/evals/T550/**`：Git基线、工作簿前后渲染、配置校验、专项/全量测试XML与日志、玩家路径和最终验证证据。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：冻结T550评分、奖励、存档、迁移、幂等和会话替换语义，并同步当前配置版本。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：开始/完成状态、配置/测试统计、证据与下一个依赖满足任务。

## 条件性白名单

- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅当T550测试需要现有未引用程序集时追加引用。
- `Tools/ConfigExporter/**`（除上述冻结测试）、`config/gameplay-config.schema.json`、`Assets/_Game/Scripts/Config/GameplayConfigDocument.cs`、`GameplayConfigRows.cs`：仅当现有Global/Levels/Rewards无法表达验收时扩展，并同步完整Schema/FieldDictionary/导出/校验/DTO闭环。

## 禁止改动

- 不修改`.unity`、`.prefab`、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不实现T600 HUD/结算View、T610字体、T620表现、T630资源、T640适配、T650教程UI、T700质量任务或平台任务。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
