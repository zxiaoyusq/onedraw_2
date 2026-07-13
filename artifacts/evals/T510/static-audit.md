# T510 Static Audit

- `BattleFlowStateMachine`、`BattleTimeSource`和`BattleFlowCoordinator`均位于Levels程序集，不依赖`MonoBehaviour`、`UnityEngine.Time`、线程、反射或xlsx运行时解析。
- 产品代码没有关卡、玩家、技能、波次或内容ID字面量；倒计时读取`Global.battle_countdown_sec`，生命周期暂停读取`Global.pause_on_focus_lost`，终极ID沿`Players.ultimateSkillId`解析，输入窗沿对应`Skills.inputWindowSec`读取。
- 统一时间源分离未缩放流程时钟、未缩放战斗时钟和受配置Effect控制的战斗时钟。Countdown只消费流程时钟；Playing/UltimateDrawing向T500传递同一个受缩放delta；Paused和终态全部冻结。暂停恢复保留倒计时已用时，重叠FocusLost/ApplicationPaused全部解除后才恢复。
- 终极只有在`UltimateDrawing`内收到本局单调非零且未消费的`gestureEventId`，以及T410产生且技能ID匹配的`SkillActivationResult.Activated`，才发布`UltimateResolved`；输入窗边界包含有效，超过边界只发布取消。同一事件不能跨绘制重放，取消、无效、超时和暂停不会扣能或伪造成功。
- `BattleOutcomeFacts`在单次解析中固定PlayerDied/DurationLimitReached优先于LevelCompleted；终态后拒绝后续结算，`Settled`事件每局最多一次。
- `BattleFlowCoordinator`只在Playing/UltimateDrawing推进T500 `LevelRunner`，只在Playing转发PlayerConfirmed；Countdown、Paused、终态和提前事件都不能跨越玩家门。
- `git diff --check`通过；没有修改场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。PlayMode造成的Enter Play Mode临时位差异已通过Unity Editor API恢复。
