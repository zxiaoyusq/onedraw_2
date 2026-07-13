# T370 Change Whitelist

- Git基线：`f45989bacabb8538fc6e22b57df216b26de56e29`（`T360: implement configured combat resolution`），分支`main`，任务开始时工作树干净。
- 需要保护的用户已有改动：无；初始化证据前已跟踪与未跟踪文件均为空。
- 任务目标：只实现T370配置驱动的敌方投射物运动、可切断/不可切断/可反弹规则、稳定归属与伤害来源、Stroke命中适配和完整回收状态。
- 明确不做：不实现T400玩家HP/架势状态、不实现T420敌人状态机、不实现T430攻击策略、不建立T440通用对象池；不恢复T120/T130或执行微信构建。

## 预计改动白名单

- `Assets/_Game/Scripts/Combat/Projectile*.cs`及Unity生成的同名`.meta`：新增配置映射、纯交互规则、不可控物理力无关的运动控制器、Stroke命中目标和回收快照/事件。
- `Assets/_Game/Tests/EditMode/T370/**`与目录`.meta`：新增`ProjectileCutTests`，覆盖可切、不可切、架势门、反弹归属/伤害来源、生命周期和重复事件。
- `Assets/_Game/Tests/PlayMode/T370/**`与目录`.meta`：新增`ProjectileReflectPlayModeTests`，走真实Collider2D/StrokeHitResolver到反弹运动和回收复用路径。
- `docs/CONFIG_SCHEMA.md`：冻结T370对现有`Projectiles`字段的运行时语义，不新增数值库。
- `docs/DECISIONS.md`：必要时记录归属、反弹方向和回收边界决策。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T370完成状态、验证计数与下一项READY任务。
- `artifacts/evals/T370/**`：保存Git基线、白名单、Unity专项/全量结果、玩家路径和最终验证报告。

## 禁止改动

- 白名单外的文件、资源与外部状态；尤其不修改xlsx/Schema/导出器/生成JSON、场景、Prefab、Input Actions、Packages、ProjectSettings、微信SDK和T400/T420/T430/T440实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`（29个文件，无未暂存项）。
