# T699 Closeout

## 起始状态

- HEAD：`3e041a41c06cba251bd613ce9ad11aa116572668`
- Unity：`6000.5.1f1`
- 起始时保留的用户改动：`Design/Config/~$GameConfig.xlsx` 为已跟踪删除；本任务不修改、不暂存。
- 根因：生产世界把所有攻击的距离条件写死为真并在攻击执行时直接扣玩家生命；敌人移动又使用关卡累计时间，导致晚出生敌人首帧跳到路径中后段。

## 预计改动白名单

- `Assets/_Game/Scripts/Actors/EnemyMovementStrategy.cs`
- `Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`
- `Assets/_Game/Scripts/Bootstrap/ProductionBattleWorld.cs`
- `Assets/_Game/Tests/EditMode/T699/**`
- `Assets/_Game/Tests/PlayMode/T699/**`
- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `docs/TECH_SPEC.md`
- `project-index.yaml`
- `artifacts/evals/T699/**`

## 验证摘要

- T699 EditMode：2/2通过。
- T699 PlayMode：1/1通过；生产路径验证轮行尸从右半屏向左移动、远处攻击不扣血、身体相交后按`contactDamage`扣血。
- 最终全量EditMode：210/210通过。
- 最终全量PlayMode：57/57通过。
- 真实Unity Game视图：Bootstrap → MainMenu → 教程Battle；两只火鱼从右半区向左推进，未接触时HUD保持100/100。
- 配置、工作簿、场景、Prefab、Registry和ProjectSettings未修改；未运行与本任务无关的配置导出门。
- 一次PlayMode初始化在Unity Test Framework `PlayModeRunTask`空引用并卡于`ExitPlayModeTask`，0/1未执行；清理MCP孤儿任务后专项通过。
- 首次全量PlayMode中T660一项因Unity Game视图失焦触发配置的自动暂停而失败；聚焦Game视图后该项1/1，完整PlayMode重跑57/57通过。
- 最后一次产品变更后已冻结验证边界；此后仅更新本收尾文档。
