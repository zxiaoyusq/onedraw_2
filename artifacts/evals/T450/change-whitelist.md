# T450 Change Whitelist

- Git基线：`06a68d05a313031419ffdb1afc0f66645b315246` (`main`)。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：只使用现有敌人配置、策略注册表与对象池，装配 5 种普通怪和 1 种精英怪，并验证六者的独立教学特征、前摇和数值配置化。
- 明确不做：不创建每怪业务子类；不实现 T460 Boss 阶段、T500 关卡时间轴、T510 战斗流程或 T630 正式美术；不修改微信 SDK/打包。

## 预计改动白名单

- `Assets/_Game/Scripts/Config/IConfigProvider.cs`、`GameplayConfigSnapshot.cs`、`GameplayConfigService.cs`：为内容装配提供只读的全敌人配置枚举，不改 JSON 合同。
- `Assets/_Game/Scripts/Actors/EnemyArchetype*.cs`：新增通用原型目录、教学特征摘要、资源类型装配、对象池注册和策略生命周期；允许 Unity 自动生成对应 `.meta`。
- `Assets/_Game/Tests/EditMode/T450/**`：新增 `EnemyArchetypeConfigTests` 及对应 `.meta`。
- `Assets/_Game/Tests/PlayMode/T450/**`：新增 `EnemyGalleryPlayModeTests` 及对应 `.meta`。
- `artifacts/evals/T450/**`：基线、日志、XML、玩家路径与最终验证证据。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：记录 T450 只读内容装配、资源类型路由、对象池/策略生命周期与验证边界。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在 T450 验收通过后更新状态、证据、数量与下一任务就绪信息。

## 禁止改动

- 不修改 Excel/FieldDictionary/Schema/导出器/受管 JSON/hash/ConfigIds，除非后续审查发现现有六怪配置无法满足已冻结合同；若发生，必须先更新本白名单并完整同步配置闭环。
- 审查已确认不需要新建或修改 `.unity`/`.prefab`/`.asset`；不修改现有 AssetRegistry 绑定、场景、Packages、ProjectSettings、微信 SDK、Builds，不改 T460 及后续玩法。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
