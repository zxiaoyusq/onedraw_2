# T530 Player Path

## 入口与配置

- Unity `6000.5.1f1`从Bootstrap加载正式Runtime配置：schema 4、content `0.5.3-sample`、hash `50ff7874...11fe`、28表689条。
- `BattleFlowCoordinator`使用`lv_002_cave`正式配置；`EnemyArchetypePool`从T450六种非Boss原型与T240 Registry资源创建实际Actor，未构造关卡专用敌人或表外波次。

## 八波路径

1. 配置倒计时结束进入Playing；八波均按各自`startDelaySec +`最后一次出生时刻推进，实际世界接受全部出生请求。
2. 每波出生数均不超过该波`maxAlive`；所有归一化出生坐标、lane、facing和修饰器请求通过运行时校验。八波总计出生45个敌人，六种非Boss原型各至少出现一次。
3. 每种原型第一次出现时都经实际`EnemyArchetypeActor/EnemyStrategyRuntime`打开Telegraph并推进到Attack。动作合计6次：Projectile 2、Charge 1、Melee 2、Support 1。
4. 玩家机制沿正式配置验证：刀架势反弹幽火；Charged匹配石甲龟防御；魂符在刀架势被拒绝，实际切换到符架势后可切断。
5. 每个敌人经实际`EnemyController.ApplyDamage`死亡、通知Level并归还精确池租约；每波只在全部计划出生完成且活动实体归零后结束。第5/7/8波共观察到3次`modifier_elite`请求。

## 结果

- `WaveStarted=8`、`WaveCompleted=8`、`LevelCompleted=1`；45次出生和45次击败后Level为Completed、Battle为Victory。
- 关卡用时不超过配置210秒；玩家最终架势为符；世界活动敌人0，`EnemyArchetypePool`活动租约0且`AssertNoLeaks`通过。
- 路径没有计时自动杀敌、表外完成接口或测试专用关卡分支；修饰器在T530范围内验证到T500世界请求边界，未声称Actor统计已应用修饰器。
