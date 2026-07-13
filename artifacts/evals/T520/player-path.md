# T520 Player Path

## 入口与配置

- Unity `6000.5.1f1`从`Bootstrap`进入`MainMenu`，Runtime日志确认schema 4、content 0.5.2、hash `f666feb2...e92`、28表668条，Registry 76键和统一Pointer输入均READY。
- `TutorialLevelCoordinator`只使用正式Runtime配置创建`lv_001_tutorial`：6波、6个当前教学步骤、15个敌人、180秒上限。

## 无计时捷径路径

1. 倒计时结束后只由`BattleReady`启动第1步；首波2只符火鱼妖全部击败后继续推进5秒，步骤与波次仍停在1，证明计时不能代替`ValidStroke`。
2. `ValidStroke`完成普通斩；第2波飞行符蝠由`EnemyWeakpointShown -> WeakpointHit`完成弱点教学。
3. 第3波同时3只轮车僵妖；`StrokeHitCount=2`被拒绝，`StrokeHitCount=3`在配置包含边界完成连斩教学。
4. 第4波2只符火鱼妖；只在`ProjectileSpawned -> ProjectileCut`且达到配置最短展示边界后完成切弹教学。
5. 第5波3只骷髅幽魂；真实`PlayerCombatController.TrySwitchStance(stance_talisman)`返回`Switched`后，`StanceChanged`完成步骤。
6. 第6波同时4只符火鱼妖；终极准备后由T510打开UltimateDrawing，T410使用配置`skill_ultimate_seal/Circle/2.5秒`激活。100能量实际扣为0，TimeScale与ClearProjectiles各执行1次，配置伤害击败4个目标；相同有效gestureEventId经T510解析后才发布`UltimateSucceeded`。

## 结果

- `StepStarted=6`、`StepCompleted=6`、`TutorialCompleted=1`、`TotalSpawned=15`、活动实体0。
- 最终教程解除进度门并只对末波`PlayerConfirmed`确认一次；Level为Completed、Battle为Victory，关卡时间小于180秒配置上限。
- 路径未调用表外“完成步骤”接口、未用固定秒数自动翻页，也未以测试专用Skill结果伪造终极成功。
