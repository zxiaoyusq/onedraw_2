# T360 Change Whitelist

- Git基线：`066720dd3d8ab394376f4337c37917531bfc4a5a`（`T350: implement nonalloc stroke hit resolution`），分支`main`，任务开始时已跟踪文件干净。
- 需要保护的用户已有改动：无；任务开始时仅本任务证据目录`artifacts/evals/T360/`为未跟踪文件。
- 任务目标：配置驱动地实现纯C#伤害公式、方向/弱点奖励、连斩、评分与能量增量，并补齐独立断言和真实输入到多目标结算的PlayMode路径。
- 明确不做：不实现T370投射物；不实现T400玩家HP/当前能量/架势切换；不实现T420敌人状态机、HP扣减或Weakpoint生命周期；不修改场景、Prefab、资源、Package、ProjectSettings或微信SDK。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：为`Stances`增加必填`damageFormulaId`外键，同步FieldDictionary与Schema/内容版本；两份工作簿保持字节一致。
- `config/schema/gameplay.schema.json`、`config/examples/gameplay_config.sample.json`、`config/README.md`：同步架势到伤害公式的契约、生成样例与版本说明。
- `Tools/ConfigExporter/Tests/{ConfigPipelineE2ETests.cs,ExporterDeterminismTests.cs,Fixtures/invalid-config-cases.json}`：同步新hash并增加非法伤害公式外键拒绝用例。
- `Assets/_Game/Config/Generated/{gameplay_config.json,gameplay_config.hash}`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`：仅由配置导出流水线更新的生成物。
- `Assets/_Game/Scripts/Config/{GameplayConfigRows.cs,GameplayConfigCompatibility.cs}`：DTO增加`damageFormulaId`，运行时兼容窗口升级到schema 2/content 0.2.x。
- `Assets/_Game/Scripts/Combat/{IRandomSource.cs,DamageContext.cs,DamageRuleSet.cs,DamageRuleSetFactory.cs,DamageCalculator.cs,ComboService.cs,ScoreService.cs}`及Unity生成的同名`.meta`：新增无MonoBehaviour依赖的T360规则与配置映射。
- `Assets/_Game/Tests/EditMode/T230/{InvalidConfigTests.cs,RuntimeConfigLoadTests.cs}`、`Assets/_Game/Tests/EditMode/T250/GeneratedConfigPipelineTests.cs`：同步版本/hash/记录数和配置契约断言。
- `Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`：同步运行时版本/hash/记录数断言。
- `Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`：同步资产注册启动日志中的配置hash/记录数断言；不修改资产清单或注册实现。
- `Assets/_Game/Tests/EditMode/T360/**`、`Assets/_Game/Tests/EditMode/T360.meta`：新增`DamageFormulaTests`与`ComboScoreTests`专项测试。
- `Assets/_Game/Tests/PlayMode/T360/**`、`Assets/_Game/Tests/PlayMode/T360.meta`：新增真实输入、几何、分类、命中到多目标结算的专项玩家路径。
- `docs/{CONFIG_SCHEMA.md,CONFIG_PIPELINE.md,DECISIONS.md,TASKS.md,PROGRESS.md}`、`project-index.yaml`：同步配置契约、版本决策、任务状态、验证总数与下一任务。
- `artifacts/evals/T360/**`：保存任务基线、白名单、专项/全量测试、Unity日志、工作簿渲染核验和最终验证报告。
- `outputs/T360-config/**`、`artifacts/tmp/T360-spreadsheet/**`：Spreadsheets技能的忽略型交付副本、渲染预览与临时脚本，不暂存。

## 禁止改动

- 上述白名单外的文件、资源和外部状态；尤其禁止修改场景、Prefab、Input Actions、Packages、ProjectSettings、微信SDK和T370/T400/T420实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`（54个文件，无未暂存项）。
