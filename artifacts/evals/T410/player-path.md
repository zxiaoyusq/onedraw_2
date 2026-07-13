# T410 Player Path

专项PlayMode Job `88c2d4842dd045ebbdfbfb6f51e03331`在实际Unity运行时创建`GameObject`并挂载、初始化`PlayerCombatController`，从受管JSON加载终极技能配置后走玩家技能入口。

1. 玩家能量填充至100。
2. 在`t=1`发送无效Circle事件：返回`GestureInvalid`，能量仍为100，世界效果为0。
3. 在`t=2`发送明确有效的Circle事件，并把`inputElapsedSeconds`设为配置边界`2.5`秒：激活成功，能量降为0。
4. 核心调用顺序精确为`timescale → clear → normal:damage → boss:damage → normal:execute → boss:buff`。
5. 可断言结果：时缩`0.25/0.8s`、清除2枚敌方弹、普通敌和Boss各受50伤害、20%血普通敌被25%阈值处决、Boss获得`buff_vulnerable` 2秒、5步各发布配置VFX/Audio。

结论：PASS。T410只验证运行时玩家入口与抽象世界效果合同；T420敌人适配、T440真实弹池和T510完整战斗流程不属于本任务。
