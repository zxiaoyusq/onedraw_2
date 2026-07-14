# T600 Change Whitelist

- Git基线：`71d7e939690e8debe6b6721a8b58e9eff82b881b`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：实现生命、能量、连斩、评分、架势、终极、暂停与结算HUD；View只渲染Presenter提供的只读模型并转发按钮意图，文案来自配置，布局位于当前Safe Area内。
- 明确不做：不实现T610字体资产、T620战斗反馈、T630正式美术、T640多比例/左右手完整适配、T650教程遮罩；不恢复T120/T130或执行微信开发者工具/打包任务。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：新增T600 HUD通用文案并升级content版本；保持29 Sheet、字段、公式和既有样式不变，镜像与正式源字节一致。
- `outputs/T600/GameConfig.xlsx`：保存与正式源字节一致的T600工作簿交付副本，不形成第三套独立内容源。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：只允许由正式工作簿经既有导出器产生的受管差异。
- `Assets/_Game/Scripts/Presentation/BattleHud*.cs`及Unity生成`.meta`：新增只读HUD状态/ViewModel、配置文案解析、Presenter订阅/命令转发、uGUI/TMP View与运行时工厂；View不得读取或修改战斗Model。
- `Assets/_Game/Scripts/Presentation/OneStrokeDemon.Presentation.asmdef`：只允许补充T600所需的TMP、uGUI和Input System UI程序集引用。
- `Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`：将Runtime兼容线从content `0.5.x`同步到T600工作簿的`0.6.x`；schema保持4。
- `Assets/_Game/Scripts/Actors/PlayerCombatController.cs`、`Assets/_Game/Scripts/Combat/ComboService.cs`、`ScoreService.cs`、`Assets/_Game/Scripts/Levels/BattleFlow.cs`、`ResultService.cs`：仅在既有状态缺少可订阅通知时增加只读快照事件或公开只读配置，不改变玩法结果。
- `Assets/_Game/Tests/EditMode/T600/**`、`Assets/_Game/Tests/PlayMode/T600/**`及目录`.meta`：新增Presenter纯规则、事件订阅、按钮门、Safe Area布局和真实配置HUD玩家路径测试。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅追加T600测试所需的Presentation/TMP/uGUI引用。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`InvalidConfigTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`：仅同步配置版本、兼容线、hash、记录/索引/ID冻结值并断言HUD文案。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ExporterCliTests.cs`、`ConfigPipelineE2ETests.cs`：仅同步content hash、记录或ID计数冻结值；不改变导出和校验规则。
- `artifacts/evals/T600/**`：Git基线、工作簿渲染/公式扫描、配置门、专项/全量测试、玩家路径、Console和最终审查证据。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：冻结T600 HUD数据所有权、状态/按钮和Safe Area语义，并同步配置版本。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：开始/完成状态、测试统计、证据和下一任务。

## 条件性白名单

- `Assets/_Game/Scripts/Bootstrap/**`：仅当现有Battle装配边界可安全注入HUD时增加组合根；不得硬编码玩法数值/文案或直接接微信SDK。
- `Assets/_Game/Scenes/Battle.unity`及其证据：仅在运行时工厂无法满足真实玩家路径时，通过Unity Editor API添加无数值的HUD组合根；禁止手工编辑YAML。

## 禁止改动

- 不修改Prefab、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不新增字体、震屏/音效/震动/慢动作、正式PSD导出资源、教程遮罩、平台存储适配或微信打包内容。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
