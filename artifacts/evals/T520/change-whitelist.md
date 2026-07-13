# T520 Change Whitelist

- Git基线：`d9a4f456010e7d723a9f9fd38b1b4104e21c1eb9`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：把`lv_001_tutorial`完成为配置驱动的约3分钟入门关，使普通斩、一笔连斩、切弹、架势切换和终极封印都由当前步骤要求的玩家事件推进，并与T500/T510关卡流程协调。
- 明确不做：不实现T530/T540/T550，不制作T600/T650正式HUD、教程遮罩/跳过/回看或复杂剧情，不强制精确书法，不修改场景/Prefab/正式资源，不恢复T120/T130微信平台工作。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：同步扩展教学关波次、出生和Tutorials/Texts内容，保持镜像字节一致及原有工作簿格式。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：只允许由配置导出器根据正式工作簿更新的受管生成差异和字节一致样例镜像。
- `Assets/_Game/Scripts/Levels/**`：新增无`MonoBehaviour`依赖的Tutorial目录/事件门/运行时协调逻辑，必要时仅做通用T500/T510适配。
- `Assets/_Game/Tests/EditMode/T520/**`、`Assets/_Game/Tests/PlayMode/T520/**`：新增教程配置、事件门和完整入门关路径测试及Unity生成的`.meta`。
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`、`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`、`Assets/_Game/Tests/EditMode/T500/SpawnTimelineTests.cs`、`Assets/_Game/Tests/PlayMode/T500/WaveRunnerPlayModeTests.cs`：只同步配置内容版本、记录数、教学波次/出生数和现有T500玩家路径断言，不改旧任务规则。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ExporterCliTests.cs`、`ConfigPipelineE2ETests.cs`、`Fixtures/invalid-config-cases.json`：只同步新内容hash/ID计数冻结值，并使“关卡无波次”坏配置在6波教学内容下仍隔离到目标诊断；不改导出或校验规则。
- `artifacts/evals/T520/**`：Git基线、工作簿审计/渲染、配置校验、Unity测试XML/日志、真实玩家路径和最终验证。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：冻结T520 Tutorials事件语义、步骤门、错误事件拒绝和关卡协调合同。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验收通过后更新T520状态、证据、测试计数与下一任务。

## 条件性白名单

- `Tools/ConfigExporter/**`、`config/gameplay-config.schema.json`、`Assets/_Game/Scripts/Config/GameplayConfigDocument.cs`、`Assets/_Game/Scripts/Config/IConfigProvider.cs`：仅当现有Tutorials字段无法表达T520必需事件时才允许扩展，并必须同步完整Schema/FieldDictionary/导出/校验/DTO/测试闭环。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅当新测试需要当前未引用程序集时追加引用。

## 禁止改动

- 不修改`.unity`、`.prefab`、AssetRegistry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不提前实现T530/T540/T550关卡和结算、T600/T650正式教程UI、T620表现或T630正式资源。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
