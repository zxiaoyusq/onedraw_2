# T520 Static Audit

- `TutorialFlow.cs`与`TutorialLevelCoordinator.cs`不引用`UnityEngine`、`MonoBehaviour`、Unity `Time`、`Task.Run`或自建线程；规则可在EditMode直接回放。
- 产品实现未出现`lv_001_tutorial`、`tutorial_level_001`、`wave_001_*`、`spawn_001_*`、敌人ID、180秒或教学最短展示数值；这些只存在于工作簿/生成配置及测试断言。
- C#中的事件/手势字符串只位于显式`TutorialProtocol`注册表，用于解释配置协议，不选择关卡内容或保存数值；未知协议显式失败。
- `LevelRunner.SetProgressBlocked`只影响当前Wave结束条件求值；出生、活动实体、关卡时间和输入仍由原T500/T510合同推进。
- `TutorialLevelCoordinator`仅在步骤Active时同步`blockProgress`；Waiting不阻塞；只有整个教程完成且当前波配置为`PlayerConfirmed`才转发确认。
- `BattleFlowSettings.UltimateGestureType`直接来自`Players.ultimateSkillId -> Skills.gestureType`，未新增终极手势常量或第二配置库。
- 双工作簿字节一致；runtime JSON与sample JSON字节一致；三受管生成物与正式工作簿只读重导结果一致。
- `git diff --check` PASS；无`.unity`、`.prefab`、Registry、Input Actions、Packages、ProjectSettings或微信SDK改动。
