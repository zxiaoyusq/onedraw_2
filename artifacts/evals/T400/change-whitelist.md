# T400 Change Whitelist

- Git基线：`b5b86678869f1badd5b38e12cb2749e95134c805`（`main`，任务开始时工作树干净）。
- 需要保护的用户已有改动：基线无未提交改动；执行中若出现白名单外差异立即停下审查，不覆盖或吸收。
- 任务目标：实现配置驱动的玩家HP、当前能量、刀/符架势、切换冷却、即时效果意图和一次性死亡事件，并证明当前架势驱动既有轨迹、伤害和切弹入口。
- 明确不做：不实现自由移动、技能效果执行链、技能CD、敌人HP/状态机、通用对象池、HUD/场景接线或微信平台任务；不新增Inspector玩法数值。

## 预计改动白名单

- `Assets/_Game/Scripts/Actors/**`：新增`PlayerCombatModel`、`PlayerCombatController`、`StanceService`及不可变状态/事件/配置映射类型与Unity `.meta`。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/EditMode/T400/**`：接入Actors程序集并新增玩家纯规则专项测试与`.meta`。
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`、`Assets/_Game/Tests/PlayMode/T400/**`：接入Actors程序集并新增架势切换真实运行时链路测试与`.meta`。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：冻结T400运行时语义、记录边界并同步任务状态/索引。
- `artifacts/evals/T400/**`：保存Git基线、白名单、静态/Unity验证、玩家路径与最终结论。

## 禁止改动

- 配置工作簿、Schema、FieldDictionary、导出器、受管JSON/hash/`ConfigIds.g.cs`与配置版本；现有字段足以承载本任务，保持只读。
- `Assets/_Game/Scenes/**`、Prefab、Input Actions、`Packages/**`、`ProjectSettings/**`、微信SDK、Build产物与所有其他任务文件。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
