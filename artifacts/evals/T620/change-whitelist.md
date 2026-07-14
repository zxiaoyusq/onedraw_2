# T620 Change Whitelist

- Git基线：`main@ddc548b69124666daae1402a92e69a6d3a726060`，任务开始时工作树干净。
- 需要保护的用户已有改动：无；后续出现的非白名单差异一律停止并审查。
- 任务目标：实现配置驱动的受击停顿、闪白、震屏、池化伤害数字/VFX、预载音效、可关闭震动和慢动作反馈；不改变T360/T420/T370战斗结算真相。
- 明确不做：不实现T630正式美术/音频替换，不修改场景或Prefab YAML，不恢复T120/T130、微信开发者工具、转换或打包，不提前做T640/T650。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`、`config/配置表预览.png`：新增并同步FeedbackCues、FieldDictionary、版本与工作簿预览。
- `config/schema/gameplay.schema.json`、`config/examples/gameplay_config.sample.json`、`config/README.md`：同步schema 5、完整稳定样例与配置入口说明。
- `Tools/ConfigExporter/Model/ConfigContract.cs`、`Tools/ConfigExporter/Generation/ConfigIdsGenerator.cs`、`Tools/ConfigExporter/Validation/*.cs`、`Tools/ConfigExporter/Tests/*.cs`、`Tools/ConfigExporter/Tests/Fixtures/invalid-config-cases.json`、`Tools/ConfigExporter/README.md`：登记新表、语义校验、坏配置与确定性测试。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`：由导出器生成的三份受管产物。
- `Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`、`GameplayConfigDocument.cs`、`GameplayConfigLoadSummary.cs`、`GameplayConfigRows.cs`、`GameplayConfigSnapshot.cs`、`GameplayConfigService.cs`、`IConfigProvider.cs`：Runtime schema 5 DTO、索引、日志计数与查询合同。
- `Assets/_Game/Scripts/Presentation/CombatFeedback*.cs`及Unity `.meta`：反馈纯编排、配置快照、Unity输出适配、音频预载/并发、目标闪白、震屏、时间与震动端口。
- `Assets/_Game/Scripts/Presentation/VfxPoolItem.cs`、`DamageNumberPoolItem.cs`：让T440池项实际渲染、推进、淡出并完整重置。
- `Assets/_Game/Tests/EditMode/T620/**`、`Assets/_Game/Tests/PlayMode/T620/**`：配置映射、事件优先级、禁震、池化、时间缩放、视觉玩家路径和截图测试。
- 既有T230/T250 EditMode及T230/T240/T600/T610 PlayMode配置断言：仅同步schema、版本、记录/索引/表计数、hash与启动日志正则，不改变原任务行为覆盖。
- `docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/TECH_SPEC.md`、`docs/TEST_PLAN.md`、`docs/DECISIONS.md`、`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：记录T620合同、验证、状态和下一任务。
- `artifacts/evals/T620/**`：基线、白名单、工作簿渲染、专项/全量测试、截图、日志与最终验证报告。

## 禁止改动

- Unity场景、Prefab、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK与`Builds/**`。
- T630正式资源、T640适配、T650教程遮罩及其他任务实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单；实现展开后补充了受schema计数影响的既有测试、`GameplayConfigLoadSummary.cs`、`config/README.md`与`docs/CONFIG_PIPELINE.md`，未扩展玩法范围。
- [x] `git diff --check`通过（2026-07-14，Unity验证前检查）。
- [x] 仅暂存白名单文件，并审查`git diff --cached`；无Scene、Prefab、Packages、ProjectSettings、Builds、`outputs/**`或其他非白名单路径。
