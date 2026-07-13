# T420 Change Whitelist

- Git基线：`9dfb805ccbb6f301411d69edbe7dd9c23afad2c9`（`main`，任务开始时工作树干净）。
- 需要保护的用户已有改动：基线无未提交改动；白名单外差异立即停下审查，不覆盖或吸收。
- 任务目标：实现配置驱动的通用敌人运行时、Spawn/Move/Windup/Attack/Recovery/Stun/Dead状态机、Damageable和Weakpoint，并冻结死亡/打断/回收幂等语义。
- 明确不做：不创建每怪空壳子类；不提前实现T430移动/攻击/防御策略注册表、T440通用对象池、T450六类具体敌人装配、T460 Boss阶段、场景/Prefab接线或微信平台任务。

## 预计改动白名单

- `Assets/_Game/Scripts/Actors/Enemy*.cs`及对应`.meta`：新增敌人配置快照、纯状态机、Damageable、通用EnemyController与生命周期事件。
- `Assets/_Game/Scripts/Actors/WeakpointController.cs`及对应`.meta`：用配置WeakpointRules驱动弱点窗口、命中代理和状态重置；不保存第二套数值。
- `Assets/_Game/Scripts/Skills/EnemySkillEffectTarget.cs`及对应`.meta`：在Skills程序集适配T410 `ISkillEffectTarget`，不让Actors反向依赖Skills。
- `Assets/_Game/Tests/EditMode/T420/**`及`.meta`：新增纯状态机、伤害、弱点、打断、死亡/回收幂等专项测试。
- `Assets/_Game/Tests/PlayMode/T420/**`及`.meta`：新增真实GameObject敌人运行时玩家命中路径；如程序集引用不足，仅修改对应测试asmdef。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：冻结T420运行时语义并同步任务状态、测试数量和下一任务。
- `artifacts/evals/T420/**`：保存Git基线、白名单、静态/Unity测试、玩家路径和最终结论。

## 禁止改动

- 不修改xlsx、FieldDictionary、Schema JSON、导出器、受管JSON/hash/ConfigIds或配置DTO；现有字段足以表达T420。
- 不修改`Assets/_Game/Scripts/Combat/**`、场景、Prefab、Input Actions、`Packages/**`、`ProjectSettings/**`、微信SDK、Build产物或任何外部平台状态。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
