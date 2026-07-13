# T540 Change Whitelist

- Git基线：`f3607c434adf0bae998065ff3f94af84ecc562d4`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：完成`lv_003_boss`约4分钟的配置驱动整关，复用T460三阶段Boss、T500时间轴与T510胜负流程，验证阶段提示、失败重试和胜利稳定。
- 明确不做：不增加第二Boss，不依赖正式过场，不实现T550结算/存档、T600 HUD、T620表现、T630正式资源、T650教程UI，也不恢复T120/T130微信平台工作。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：只同步`lv_003_boss`的Levels/Waves/Spawns及必要Boss阶段/攻击/文案内容版本，保持29 Sheet结构、字段和既有格式不变，镜像字节一致。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：只允许配置导出器根据正式工作簿生成的受管差异和样例镜像。
- `Assets/_Game/Tests/EditMode/T540/**`、`Assets/_Game/Tests/PlayMode/T540/**`：新增Boss整关结构、阶段可读性、表驱动调整、胜利/失败/重试和池清理测试及Unity生成的`.meta`。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/EditMode/T500/SpawnTimelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T500/WaveRunnerPlayModeTests.cs`：仅同步配置版本、记录数、ID计数及Boss关冻结值，不改变旧任务规则。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ExporterCliTests.cs`、`ConfigPipelineE2ETests.cs`：仅同步内容hash/ID计数冻结值，不改导出或校验规则。
- `artifacts/evals/T540/**`：Git基线、工作簿前后审计/渲染、配置校验、Unity测试XML/日志、玩家路径和最终验证。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：只冻结T540 Boss整关内容、阶段提示和胜负/重试验收语义，并修正当前配置版本摘要。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：验收通过后更新T540状态、证据、配置/测试统计与下一任务。

## 条件性白名单

- `Assets/_Game/Scripts/Levels/**`或`Assets/_Game/Scripts/Skills/**`：只有现有T460/T500/T510确实无法表达Boss整关胜负/重试时，才允许最小通用协调修复；不得按Boss、关卡或阶段ID分支。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅当新测试需要当前未引用程序集时追加引用。
- `Tools/ConfigExporter/**`、`config/gameplay-config.schema.json`、`Assets/_Game/Scripts/Config/GameplayConfigDocument.cs`、`Assets/_Game/Scripts/Config/IConfigProvider.cs`：只有现有字段无法表达验收时才允许扩展，并必须同步完整Schema/FieldDictionary/导出/校验/DTO/测试闭环。

## 禁止改动

- 不修改`.unity`、`.prefab`、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不提前实现T550/T600/T610/T620/T630/T640/T650/T700及平台任务。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
