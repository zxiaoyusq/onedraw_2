# T410 Change Whitelist

- Git基线：`0a33e90a576764c284cbaca7bce58a6a6c9c45f3`（`main`，任务开始时工作树干净）。
- 需要保护的用户已有改动：基线无未提交改动；执行中出现白名单外差异立即停下审查，不覆盖或吸收。
- 任务目标：实现配置驱动的Skill→EffectGroup→有序Effect执行链、触发/架势/能量/CD门、显式执行器注册表与伤害/治疗/Buff/击退/清弹/慢动作等现有配置效果；完成终极有效笔势玩家路径。
- 明确不做：不为每个技能创建MonoBehaviour，不用运行时反射；不实现T420敌人状态机、T430攻击策略、T440通用对象池、T510完整战斗流程、HUD或微信平台任务。

## 预计改动白名单

- `Assets/_Game/Scripts/Skills/**`：新增SkillService、不可变规则/请求/结果、显式IEffectExecutor注册表、目标选择与现有效果执行器及Unity `.meta`。
- `Assets/_Game/Scripts/Actors/PlayerCombatModel.cs`、`Assets/_Game/Scripts/Actors/PlayerCombatController.cs`：为T410治疗执行器补充不复活的HP恢复入口与战斗事件，不改变T400伤害/死亡合同。
- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：补齐运行时明确支持但Enums尚未登记的`Heal`/`ClearProjectiles`，并把清弹步骤加入终极效果链；两份工作簿保持字节一致。
- `config/examples/gameplay_config.sample.json`、`Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`：由受管导出器同步生成物与哈希，不手工编辑。
- `config/schema/gameplay.schema.json`、`Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`、`config/README.md`、`docs/CONFIG_PIPELINE.md`：现有生产校验要求Schema枚举与Enums精确镜像，因此同步EffectType集合，并把运行时兼容窗口升级为schema 3/content 0.3.x。
- `Tools/ConfigExporter/**`、`Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T250/**`、`Assets/_Game/Tests/PlayMode/T230/**`、`Assets/_Game/Tests/PlayMode/T240/**`：仅在配置闭环需要时补充语义校验/更新冻结记录数、内容版本与哈希断言。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/EditMode/T410/**`：接入Skills程序集并新增有序效果链、门控、目标过滤和执行器专项测试与`.meta`。
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`、`Assets/_Game/Tests/PlayMode/T410/**`：接入Skills程序集并新增终极有效笔势运行时玩家路径与`.meta`。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：冻结T410运行时语义并同步任务状态/索引。
- `artifacts/evals/T410/**`：保存Git基线、白名单、配置静态检查、Unity专项/全量测试、玩家路径与最终结论。

## 审计后扩展说明

- 开始时计划保持配置只读；审计发现`EffectType`缺少任务明确要求的`Heal`与`ClearProjectiles`，且终极链未配置清弹。若只在C#注册执行器会制造Enums与运行时注册表分叉，因此按项目配置闭环规则扩展本白名单。
- 首次导出验证以`CFG002`证明EffectType属于JSON Schema冻结API：Schema枚举必须与Enums精确镜像。因而升级为schema `3` / content `0.3.0-sample`；本次仍不改变字段、Sheet、DTO属性或FieldDictionary行，`GameplayConfigRows.cs`继续只读。

## 禁止改动

- `FieldDictionary`和配置DTO字段结构；除非后续验证证明现有合同无法表达本任务并先再次更新白名单。
- `Assets/_Game/Scripts/Combat/**`、场景、Prefab、Input Actions、`Packages/**`、`ProjectSettings/**`、微信SDK和Build产物。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
