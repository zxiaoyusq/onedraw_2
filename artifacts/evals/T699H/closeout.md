# T699H 收尾证据

## 起始状态

- 分支：`main`，相对 `origin/main` ahead 5。
- 用户已有改动（必须保留且不纳入本任务）：`AGENTS.md`、`Packages/manifest.json`、`Packages/packages-lock.json`、删除的 `Assets/_Game/Art/Enemies/11.anim` 与 `.meta`、删除的 `Design/Config/~$GameConfig.xlsx`。

## 预计改动白名单

- 配置源与同源生成物：`Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`、`config/schema/gameplay.schema.json`、`config/examples/gameplay_config.sample.json`、`Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`。
- 配置运行时：`Assets/_Game/Scripts/Config/GameplayConfigRows.cs`、`Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`。
- 输入纯规则：`Assets/_Game/Scripts/Input/GestureType.cs`、`Assets/_Game/Scripts/Input/GestureRule.cs`、`Assets/_Game/Scripts/Input/GestureClassifier.cs`、新增 `Assets/_Game/Scripts/Input/TriangleGestureMatcher.cs` 及 Unity 自动生成的 `.meta`。
- 配置映射与生产接线：`Assets/_Game/Scripts/Combat/GestureRuleSetFactory.cs`、`Assets/_Game/Scripts/Actors/EnemyMovementStrategy.cs`、`Assets/_Game/Scripts/Skills/EnemySkillEffectTarget.cs`、`Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`、`Assets/_Game/Scripts/Bootstrap/ProductionBattleWorld.cs`。生产PlayMode首次暴露技能玩法时钟与敌人角色时钟不同域，白名单在同一原子任务内扩展到目标适配器，由该边界统一转换时间戳。
- 测试与冻结断言：`Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/EditMode/T330/GestureClassifierTests.cs`、`Assets/_Game/Tests/EditMode/T410/SkillEffectPipelineTests.cs`、`Assets/_Game/Tests/EditMode/T699/EnemyApproachRuleTests.cs`、新增 `Assets/_Game/Tests/EditMode/T699H/**`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T600/HudBindingPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T610/LocalizationGlyphPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T620/CombatFeedbackPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T650/TutorialSkipPlayModeTests.cs`、新增 `Assets/_Game/Tests/PlayMode/T699H/**`、`Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`Tools/ConfigExporter/Tests/ConfigPipelineE2ETests.cs`、`Tools/ConfigExporter/Tests/ExporterCliTests.cs`、`Tools/ConfigExporter/Tests/WorkbookDocumentationTests.cs`。
- 文档与索引：`docs/TASKS.md`、`docs/PROGRESS.md`、`docs/DECISIONS.md`、`docs/GAME_DESIGN_MVP.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`project-index.yaml`、本文件。
- 电子表格技能交付副本与忽略的验证产物：`outputs/T699H/GameConfig.xlsx`、`artifacts/tmp/T699H-spreadsheet/**`、本任务测试日志/XML；不会纳入产品提交，除非仓库既有规则明确追踪其中某项证据。

## 验证摘要

- 权威工作簿与镜像均为126,182字节，SHA-256均为`8b77e6054281e7a9bd471a7900c9606e0bec3927f11ea4c7355d0c1c8465d7e2`，字节完全一致；31个Sheet完成渲染目检，公式错误0，电子表格交付副本位于`outputs/T699H/GameConfig.xlsx`。
- 严格导出、同源生成与漂移检查通过；ConfigExporter 64/64。受管快照为schema 7/content `0.7.0-sample`/hash `e0b0dcecdcea50ad079c8b7880d0f7a7a0df6771d671fecf13bf57845dbe5448`，30表772条、29组385个ID常量。
- T699H EditMode 4/4通过，覆盖近似三角形正例、圆形/矩形/未闭合反例、技能全敌Buff链和减速移动连续性。
- T699H生产PlayMode首次1项因激活/效果上下文时间戳不同而失败；修正为技能服务保持玩法时钟、生产敌人目标适配器转换角色时钟后，同一测试1/1通过。该测试从真实Bootstrap/Battle组合输入三角形，并断言普通怪和Boss均获得配置Buff。
- 最终有效全量回归：EditMode 223/223（任务`69ac93a37525472e862b4bcff878a868`）、PlayMode 62/62（任务`d2ab1de299ee4cb09a805aecd03fadae`）。一次未指定程序集的EditMode任务在0项测试时初始化超时，不作为产品测试结果；随后显式运行完整测试程序集通过。
- Unity 6000.5.1f1刷新编译完成，最终Console Error/Warning为0。最后一次产品变更后验证边界冻结；后续仅更新文档与证据。

## 保留的用户改动

- `AGENTS.md`、`Packages/manifest.json`、`Packages/packages-lock.json`继续保持用户原有修改；`Assets/_Game/Art/Enemies/11.anim`及`.meta`、`Design/Config/~$GameConfig.xlsx`继续保持用户原有删除状态。以上路径不进入T699H暂存与提交。
