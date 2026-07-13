# T540 Player Path

执行环境：Unity `6000.5.1f1`隔离批处理项目，输入来自当前工程的`Assets/Packages/ProjectSettings`；通过Bootstrap加载真实受管JSON和AssetRegistry。对应测试为`BossLevelE2EPlayModeTests`，结果见`playmode-results.xml`。

## 胜利路径

1. 从Countdown进入Playing，推进到第一波最后一个配置出生时刻。
2. 实际活动敌人为11，Boss为0；敌人集合覆盖符火鱼妖、飞行符蝠、石甲龟妖、骷髅幽魂和摄魂道傀。
3. 逐个击败并释放前置敌人，等待配置结束延迟与Boss波开始延迟后，只生成1个镇墓玄甲王并绑定阶段控制器。
4. 存活Boss的`BossDefeated`通知返回false；按配置依次完成`atk_boss_rockfall`、`atk_boss_seal_wave`、`atk_boss_charge`并把HP推进到阶段阈值。
5. 阶段事件精确3次、阶段进入VFX精确3次；第三段配置中文提示包含“处决”。
6. 处决造成真实死亡后通知击败，下一次流程裁决为Victory；总出生12、用时不超过配置240秒。
7. 释放世界实体后活动数0，敌人池`AssertNoLeaks`通过。

## 失败与重试路径

1. 首局走到Boss第一阶段，阶段事件计数为1。
2. 对玩家施加等于当前HP的伤害，死亡事实触发，下一次流程裁决为Defeat；Boss阶段控制器为空且不再报告活动Boss。
3. 结算后继续伤害旧Boss，阶段事件仍为1，证明HP/阶段订阅已释放。
4. 显式释放首局实体并验证池无泄漏；创建全新的玩家、世界和`BossLevelCoordinator`。
5. 重试局重新走完前置波与三阶段，阶段事件精确3次，处决后Victory。
6. 重试世界活动数0，敌人池再次无泄漏。

## 证据边界

本文件记录可重复断言的自动化原型玩家路径。macOS登录会话锁定，未执行或声称手工UI、正式演出可读性、微信开发者工具或真机玩家路径。
