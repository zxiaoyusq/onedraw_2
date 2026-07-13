# T370 Projectile Contract

## 配置边界

- `ProjectileRuleSetFactory`只读取`IConfigProvider.GetProjectile(projectileId)`；现有schema 2 / content 0.2.0-sample已经包含本任务全部字段，因此没有修改xlsx、FieldDictionary、Schema、DTO、导出器、JSON/hash或ConfigIds。
- 运行时映射字段：`movePatternId`、`speedRefPxSec`、`lifeSec`、`damage`、`cuttable`、`reflectable`、`requiredStanceId`、`hitRadiusRefPx`、`assetKey`、`vfxKey`。
- 交互优先级：`requiredStanceId`门 → `reflectable` → `cuttable` → 不可切断。两个开关都为true时只反弹；两个都为false时弹体保持活动。

## 归属与伤害来源

- 初始敌方实例7001生成`proj_ghost_fire`；反弹者为玩家101。
- 反弹后`currentOwner=Player/101`，`originalOwner=Enemy/7001`保持不变，`reflectionCount=1`，方向由左变右。
- `ProjectileDamageSource`发布表内`projectileId=proj_ghost_fire`与`damage=8`，同时保留当前/原始归属和反弹次数；当前玩家归属使弹体可命中Enemy/7001而不能伤Player阵营。

## 运动与回收

- 控制器只按显式单位方向、表内参考像素速度和调用方delta移动Transform；没有Rigidbody/AddForce、随机力或Inspector数值。
- 真实路径中反弹后0.5秒位移`260 × 0.5 = 130`参考像素。
- Cut / Impact / LifetimeExpired / Manual四条释放路径都生成不可变快照；随后清空规则、归属、参考空间、位置、方向、时间和命中ID，禁用Collider，重置Transform并停用GameObject。
- PlayMode从尚未激活的GameObject创建控制器并直接`Spawn`，首次激活不会由`Awake`覆盖刚写入的配置Collider状态。
- 同一个组件随后以`proj_rockfall`、新敌方9001、新targetId 5002、半径34和向下方向重新生成，反弹次数/旧归属/旧时间均为初始值。

## 明确后续边界

- T400提供真实玩家HP、当前能量和当前架势状态。
- T420消费`ProjectileDamageSource`扣除敌人HP。
- T430负责敌方攻击/移动策略注册和生成方向。
- T440负责通用池容量、预热、借还和泄漏检测；T370只保证单体可安全复用。
