# T400 Player Combat Contract

- 配置入口：`Players.player_moyan`提供100 HP、100能量上限、默认刀架势、0.35秒受击无敌与终极技能ID；产品代码只映射字段，不复制这些值。
- 能量入口：T360解析出的`DamageResult.energyAward`进入当前能量并按玩家上限饱和；技能扣能从目标`Skills.energyCost`读取，同时检查`requiredStanceId`。能量不足、架势不符和死亡都不部分扣能。
- 架势入口：初始`stance_blade`；成功切入目标架势后，使用目标行的`switchCooldownSec`，并发布目标行`onSwitchEffectGroupId`意图。等于冷却边界允许切换，同架势和冷却内请求不发布事件。
- 下游影响：同一`Current.StanceId`传给T340轨迹、T360伤害和T370投射物。真实配置路径得到轨迹宽度18→28参考像素、公式`damage_player_default`→`damage_talisman_default`，`proj_seal_bolt`由刀架势不匹配变为符架势反弹；切弹倍率快照0.8→1.4。
- HP与死亡：有效伤害夹到0，成功受击后按配置无敌；第一次HP归零依次发布`HpChanged`和`Died`，同帧第二次伤害只返回`AlreadyDead`，不再发布死亡。死亡后拒绝能量获取/消耗和架势切换。
- T400边界：不执行`onSwitchEffectGroupId`或其他Skill Effect，不实现技能CD、敌人状态、战斗流程、HUD、自由移动、场景或Prefab接线。
