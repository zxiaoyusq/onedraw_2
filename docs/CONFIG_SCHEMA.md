# CONFIG_SCHEMA：配置表冻结契约

## 1. 版本与唯一真相源

- 当前冻结版本：`schemaVersion = 4`、`contentVersion = 0.5.0-sample`。
- 正式内容唯一源：`Design/Config/GameConfig.xlsx`。
- `config/一笔镇妖_游戏配置表模板.xlsx` 只是随正式源同步的示例镜像，不接受独立内容修改。
- `Assets/_Game/Config/Generated/gameplay_config.json`和`gameplay_config.hash`由T250导出器生成，是可审查、可构建的只读Runtime快照与hash旁车。
- `Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`由同一模型生成，并位于`OneStrokeDemon.Config`程序集作用域；生成文件不得手工编辑。
- Unity Runtime只读取JSON，不解析xlsx。Inspector、ScriptableObject和C#都不能保存第二套平衡数据。
- Unity对象引用由后续 `AssetRegistrySO` 按 `assetKey` 提供；配置只保存稳定键，不保存GUID、资源路径或对象引用。

## 2. 数据所有权

| 数据 | 负责人 | 变更规则 |
|---|---|---|
| Sheet、表头、字段类型、主键、ID/枚举/外键契约 | 程序与策划共同审批 | 属于API变更，必须同步工作簿、FieldDictionary、Schema、导出器、DTO、校验、文档和测试。 |
| 玩法内容、数值、关卡、波次、敌人、技能、教程和文案 | 策划 | 只修改正式工作簿，通过导出和校验进入Runtime。 |
| `assetKey`命名与AssetManifest记录 | 策划与美术共同维护 | 键必须稳定；Unity对象绑定由AssetRegistry维护。 |
| JSON、hash、生成ID常量 | 构建工具 | 只生成、不可手工编辑；同输入必须字节一致。 |
| 规则算法、DTO和策略注册表 | 程序 | 不复制工作簿中的平衡值。 |
| Inspector/ScriptableObject | 程序与美术 | 仅保存Unity资源/场景引用和明确的调试兜底。 |

## 3. 工作簿物理结构

- 工作簿固定29个Sheet：1个 `README` 加28个数据Sheet；名称和顺序属于契约。
- 每个数据Sheet第1行为标题、第2行为说明、第3行留空、第4行为唯一表头、第5行起为数据。
- 首字段为空的整行视为空白行并忽略；数据区中间不得插入“看似空白但带业务值”的行。
- `README!E5:E18` 的14个 `COUNTA` 公式只用于人工摘要，公式引用必须带Sheet单引号且限定 `A5:A1000`；公式与公式结果都不进入JSON。
- `FieldDictionary` 有250条记录，完整描述其余27个数据Sheet（包括 `Enums`），但不递归描述自身。`FieldDictionaryRow` 的JSON结构由Schema直接定义。
- 表头是API，顺序必须与Schema `$defs/*Row.properties`、`required`及样例JSON对象字段一致。

## 4. Sheet、主键与分组

| Sheet | 行主键或组合键 | 分组/引用语义 | 内容 |
|---|---|---|---|
| Global | key | — | 全局阈值、预算、时间、上限 |
| Players | playerId | — | 玩家基础值与默认架势 |
| Stances | stanceId | `damageFormulaId`引用伤害公式 | 刀/符架势倍率、公式与资源 |
| StrokeRules | ruleId | — | 笔势阈值与命中半径 |
| DamageFormulas | formulaId | — | 伤害与连斩公式参数 |
| DefenseRules | defenseRuleId | — | 护甲、方向、破甲 |
| WeakpointRules | weakpointRuleId | — | 弱点位置、窗口、倍率 |
| MovePatterns | movePatternId | — | 移动策略参数 |
| Enemies | enemyId | attackSetId引用攻击分组 | 敌人主数据与策略ID |
| EnemyAttacks | attackId | attackSetId+order形成连续攻击组 | 前摇、伤害、弹幕、打断窗口 |
| Projectiles | projectileId | — | 弹道、切断、反弹 |
| Buffs | buffId | — | 状态效果与叠层 |
| Skills | skillId | effectGroupId引用效果组 | 触发、CD、能量、效果组 |
| SkillEffects | effectGroupId+order | order在组内从1连续 | 有序效果链 |
| Levels | levelId | rewardTableId/tutorialId引用分组 | 关卡入口、背景、奖励和星级 |
| Waves | waveId | levelId+order形成关卡内连续波次 | 波次条件、时间和顺序 |
| SpawnPoints | spawnPointId | levelId允许受控通配符 | 归一化出生位置、抖动和朝向 |
| EnemyModifiers | modifierId | — | 普通/精英出生修饰倍率 |
| Spawns | spawnId | — | 出生时间、位置、数量和模式 |
| BossPhases | bossPhaseId | enemyId+order形成连续阶段 | 阶段条件、策略与进入动作 |
| Rewards | rewardTableId+order | order在奖励表内从1连续 | 评分、解锁和预留货币 |
| Tutorials | tutorialId+order | order在教程内从1连续 | 事件驱动教学步骤 |
| Texts | textKey | — | 中文和英文文案 |
| AudioCues | audioKey | — | 音频资源键与并发参数 |
| VfxCues | vfxKey | — | VFX资源键、生命期和池预热 |
| AssetManifest | assetKey | — | 配置期预期资源键与类型 |
| Enums | enumType+value | enumType内value唯一 | 策划枚举字典 |
| FieldDictionary | sheet+field | 不自描述 | 字段类型、约束与说明 |

`attackSetId`、`effectGroupId`、`rewardTableId`和 `tutorialId` 是分组ID；引用校验要求目标组至少存在一行，不能把分组字段单独误判为行主键。

## 5. 字段、空值与类型

- Schema `required` 表示JSON对象必须包含该属性；即使业务字段可空，属性也必须输出。
- FieldDictionary的字符串列 `required` 只允许 `"true"` 或 `"false"`，表示Excel单元格是否必须非空。这与Schema的属性存在性不是同一概念。
- FieldDictionary `type` 只允许 `string`、`int`、`float`、`bool`；`min/max`允许小数。
- 可空字符串导出为 `""`；可空 `int/float/bool` 导出为 `null`。不得把空字符串静默转换为0或false。
- 所有字符串先Trim；小数点固定为 `.`；数值解析使用InvariantCulture；时间统一为秒。
- Safe Area归一化坐标范围为0～1；参考像素字段以1920×1080参考坐标解释，不使用设备DPI。
- `Global` 是带判别字段的联合：每行 `valueType` 对应的 `intValue/floatValue/stringValue/boolValue` 中必须恰好一个非空，且必须是与 `valueType` 同名的值列。因此这四列在FieldDictionary中均为可空。
- 一对多关系使用子表，不在单元格中嵌套JSON。仅明确标注的轻量字符串列表可使用英文逗号，导出时逐项Trim并保持声明顺序。

## 6. ID与命名空间

- 稳定ID/Key必须匹配 `^[a-z][a-z0-9_]*$`，区分大小写，发布后不可复用为其他语义。
- 枚举值不是ID；枚举值使用 `Enums` 中声明的大小写，禁止自动改写大小写或接受未知值。
- 不同命名空间互相独立。`enemyId`、`displayNameKey`和 `assetKey` 不要求同名；修正玩法ID不得顺带改文案键或资源键。
- GAME_DESIGN当前权威关卡ID固定为 `lv_001_tutorial`、`lv_002_cave`、`lv_003_boss`；Boss敌人ID固定为 `boss_tomb_king`。
- `text_level_001`、`reward_level_001`、`tutorial_level_001`和 `boss_tomb_armor_king`（资源键）属于各自独立命名空间，不因上述玩法ID修正而改名。
- `*` 不是普通ID，只允许作为 `SpawnPoints.levelId`，表示所有关卡可使用该出生点；其他Sheet/字段出现 `*` 必须失败。

## 7. 外键契约

FieldDictionary `foreignKey` 使用以下三种形式：

1. `Sheet.field`：普通或分组外键。空值仅在该字段 `required=false` 时允许；非空值必须在目标字段集合中存在。
2. `conditional`：仅用于 `Rewards.rewardId`，按同一行 `rewardType` 分派：
   - `UnlockLevel`：必须存在于 `Levels.levelId`；
   - `UnlockFeature`：MVP保留的外部命名空间，必须匹配 `feature_[a-z0-9_]+`；
   - `ScoreToken`：MVP保留的外部命名空间，必须匹配 `token_[a-z0-9_]+`；
   - 未登记的rewardType必须失败，不能跳过校验。
3. `SpawnPoints.levelId -> Levels.levelId` 的唯一例外：值为 `*` 时跳过目标存在性检查，但仍执行通配符作用域检查。

外键必须在整份工作簿解析完成后统一校验；任何缺失都阻止整包进入战斗，不能半应用。

T360新增的`Stances.damageFormulaId -> DamageFormulas.formulaId`是必填普通外键。调用方只提供架势ID，Runtime规则工厂必须沿此外键选择公式；禁止在C#维护“刀/符→公式ID”的第二映射。

## 8. 稳定排序与contentHash

- JSON顶层属性顺序固定为：`schemaVersion`、`contentVersion`、`contentHash`，随后按工作簿中的28个数据Sheet顺序使用lowerCamelCase数组名。
- 简单表按行主键Ordinal升序；组合表按组合键依次升序。
- 玩法分组表的稳定顺序为：EnemyAttacks按 `attackSetId, order, attackId`；SkillEffects按 `effectGroupId, order`；Waves按 `levelId, order, waveId`；BossPhases按 `enemyId, order, bossPhaseId`；Rewards按 `rewardTableId, order`；Tutorials按 `tutorialId, order`。
- Enums按 `enumType, value`；FieldDictionary按固定Sheet顺序和该Sheet表头顺序。排序不得依赖当前Excel行号、系统区域设置或字典遍历顺序。
- `contentHash` 为小写64位SHA-256，输入是排除 `contentHash` 属性后的完整配置对象：对象键递归按Ordinal升序、数组保持上述稳定导出顺序、UTF-8无BOM、紧凑JSON无多余空白、Unicode直接写入UTF-8并只做JSON必要转义、数字使用不带区域格式的最短合法表示。
- 生成时间只写日志，不写入内容或hash。相同输入连续两次导出必须字节完全相同。
- hash旁车固定为64位小写`contentHash`加单个LF，不带BOM或其他元数据。
- `ConfigIds.g.cs`固定由配置定义表的稳定主键/Key及四类分组ID生成；集合和值均按Ordinal排序，标识符冲突必须失败。文件同时嵌入schema/content/hash和当前ID组/常量计数，使Unity编译测试可核对三生成物同源。

## 9. 必须校验

- 29个Sheet名称/顺序、每个表头、FieldDictionary覆盖和Schema定义一致。
- 主键/组合键唯一；FieldDictionary必填字段非空；类型、范围、布尔、枚举和Trim规则有效。
- 全部普通、分组、通配符和conditional外键符合第7节。
- SkillEffects、EnemyAttacks、Waves、Rewards、Tutorials和BossPhases的组内order从1连续且不重复。
- Level → Wave → Spawn完整；时间非负；星级阈值严格递增。
- Boss阶段覆盖1到0，阈值严格递减、前后相接且无重叠/空洞。
- 敌人引用的策略ID、`EffectType`执行器和`TargetType`选择器由代码显式注册；工作簿Enums、JSON Schema和导出器注册集合必须精确一致；文案、音频、VFX和资源键存在。
- 任意错误都阻止整份配置进入战斗；错误必须定位Sheet、Excel行、字段和稳定错误码。

## 10. Runtime约定

- JSON根包含 `schemaVersion`、`contentVersion`、`contentHash` 和28个表数组。
- `GameplayConfigService` 启动时一次性反序列化、校验版本并构建只读索引。
- 业务代码通过 `IConfigProvider.GetEnemy(id)` 等API访问，不直接遍历或修改原始DTO。
- 启动日志打印配置来源、版本、hash、记录数和校验摘要。
- 业务代码可使用`ConfigIds`避免魔法字符串，但常量只表达稳定ID，不复制配置数值或对象引用；Runtime仍以JSON索引为内容真相。

## 11. T360命中结算公式

- `DamageContext`只携带一次已排序命中的stroke/target、已识别笔势、架势、弱点标记、连斩数和外部时间戳；不读取`Time`、全局随机或敌人MonoBehaviour状态。
- 方向成立条件为：`requiredGestureType=Any`或与本笔势相等，并且`requiredStanceId`为空或与当前架势相等。成立倍率取`DefenseRules.breakDamageMultiplier`；失败倍率取`DamageFormulas.wrongDirectionMultiplier × DefenseRules.wrongGestureDamageMultiplier`，同时发布该防御表行的`reflectDamage`。
- 弱点倍率仅在命中弱点时取`DamageFormulas.weakpointMultiplier × WeakpointRules.damageMultiplier`；弱点能量/评分加值和`interruptAttack`也只在该分支生效。
- 连斩倍率为`min(1 + (comboCount - 1) × comboStep, comboMaxMultiplier)`。`ComboService`按T350稳定命中顺序逐目标递增，同一笔多目标会形成连续计数；相邻命中间隔小于或等于`Global.combo_timeout_sec`时延续，超过时从1重启。
- 暴击由注入的`IRandomSource`在`[0,1)`取值并与`criticalChance`比较，只把`criticalMultiplier`乘到伤害；不允许调用Unity全局随机。
- 原始伤害为`baseDamage × Stances.damageMultiplier × 方向倍率 × 弱点倍率 × 连斩倍率 × 暴击倍率`。
- 原始评分为`(scorePerHit + 弱点评分加值) × 方向倍率 × 连斩倍率 + 原始伤害 × scorePerDamage`。因此伤害、命中、方向、弱点和同笔多目标都会进入T360评分；弹反、无伤与剩余时间由各自后续任务接入，不能在这里预判。
- 原始能量收益为`(energyPerHit + 弱点能量加值) × 方向倍率 × 连斩倍率`。T360只累计“已赚取能量”；玩家当前能量、上限和技能消耗归T400。
- 伤害、评分和能量分别在所有乘法/加法完成后用`MidpointRounding.AwayFromZero`取整为非负`long`；非有限值或溢出必须失败，不允许隐式截断或部分发布。

## 12. T370敌方投射物运行时语义

- `ProjectileRuleSetFactory`只通过`IConfigProvider.GetProjectile(projectileId)`映射`Projectiles`现有字段；速度、寿命、伤害、命中半径、切断/反弹开关、所需架势、移动策略ID和资源键均不得在Prefab、Inspector或控制器中复制。
- 玩家笔迹先检查`requiredStanceId`：为空时任意架势可交互，非空时必须与当前架势Ordinal相等。架势不匹配只发布`RequiredStanceMismatch`，投射物继续保持原归属、方向、寿命和伤害来源。
- 架势通过后按固定优先级解释两个独立布尔量：`reflectable=true`为反弹；否则`cuttable=true`为切断；两者都为false为不可切断。两者都为true时反弹优先，避免同一次笔迹既反弹又回收。
- 初始归属由生成攻击的运行时实体传入；来源保存`currentOwner`与不可变`originalOwner`。反弹把`currentOwner`切到划动玩家、保留原敌方实体并递增反弹次数，同时把显式运动方向取反；反弹后的玩家归属投射物不再接受同阵营笔迹。
- 伤害来源由`projectileId`、表内`damage`、当前归属、原始归属和反弹次数共同组成。敌方投射物只能伤玩家阵营；反弹后只能伤敌方阵营，命中同阵营不消费投射物。
- `ProjectileController`只按调用方给出的参考像素位置/单位方向、表内`speedRefPxSec`与外部`deltaSeconds`做确定性Transform位移，不使用`Rigidbody2D.AddForce`或随机物理力；达到`lifeSec`边界时回收。
- 切断、有效碰撞、寿命到期或显式回收都先生成不可变快照，再清空规则、归属、参考空间、位置、方向、已过时间与`ProjectileHitTarget` ID，禁用并清空Stroke Collider状态，重置Transform并停用GameObject。再次生成必须由新配置和新初始归属完整覆盖；通用池容量、预热与泄漏检测见第17节。

## 13. T400玩家战斗状态与架势切换语义

- `PlayerCombatSettingsFactory`只从`Players`行映射`maxHp`、`maxEnergy`、`defaultStanceId`、`ultimateSkillId`和`hitInvulnSec`，并沿`ultimateSkillId`读取`Skills.energyCost`；玩家初始HP等于配置上限，当前能量从战斗语义的空槽开始，不允许Inspector或Prefab复制数值。
- `PlayerCombatModel`是无`MonoBehaviour`依赖的纯状态模型。有效伤害把HP夹到0；成功受击后在`hitInvulnSec`内拒绝后续伤害，等于边界时允许再次受击。HP第一次从正数变为0时结果中的`DeathTriggered`为true，之后同帧或后续伤害都只返回`AlreadyDead`。
- T360 `DamageResult.energyAward`进入玩家当前能量时按`Players.maxEnergy`饱和，不溢出；技能消耗只能读取目标`Skills.energyCost`，能量不足、所需架势不匹配或玩家已死亡均不得部分扣除。T400只预留消耗结果，不执行技能CD或EffectGroup，后者属于T410。
- `StanceService`每次从目标`Stances`行构造不可变快照，公开伤害公式/倍率、幽魂倍率、切弹倍率、轨迹宽度、切换冷却、即时效果组与资源键。首次切换可立即发生；成功切入目标架势后使用该目标行的`switchCooldownSec`计算下一次可切换时刻，等于边界允许，重复点击当前架势或冷却内请求不发布切换事件。
- 成功切换立即更新唯一当前架势，并发布带`onSwitchEffectGroupId`的`StanceChanged`意图；T410只消费该意图执行配置效果链，不得在T400控制器中硬编码即时效果。玩家死亡后不能再切换架势。
- 当前架势ID必须直接传给T340 `StrokeTrailSettingsFactory`、T360 `DamageRuleSetFactory`和T370 `ProjectileCutResolver`。因此同一玩家状态切换后，轨迹宽度、伤害公式/倍率、`projectileCutMultiplier`快照及投射物`requiredStanceId`门在同一次调用后立即生效，不维护刀/符第二映射。
- `PlayerCombatController`只把纯模型结果转换为单调序号的`HpChanged`、`EnergyChanged`、`StanceChanged`和`Died`战斗事件；致死顺序固定为HP变化后死亡，同一玩家生命周期最多发布一次死亡事件。T400不修改场景/Prefab，也不实现自由移动、HUD、敌人状态机或技能效果执行。

## 14. T410技能与效果链运行时语义

- `SkillService`只从`Skills`读取触发类型、所需架势、能量、CD、笔势、输入窗和`effectGroupId`，再从`SkillEffects`读取组内连续`order`；执行前按order稳定排序并再次拒绝空组、断号、未知执行器/目标选择器和非法条件语法。`ExecuteEffectGroup`消费T400 `onSwitchEffectGroupId`等已经产生的非Skill效果组意图，并复用同一排序、校验和执行路径。新增由现有效果组成的技能只增加配置行，不新增技能专属`MonoBehaviour`或C#分支。
- `Gesture`和`Ultimate`触发必须收到调用方明确传入的有效笔势事件：配置笔势非`Any`时还必须Ordinal相等，`inputElapsedSeconds <= inputWindowSec`的边界有效。无效、超时、触发类型错误、冷却、架势错误、能量不足或玩家死亡都不部分扣能、不执行任何Effect；计时器不能代替有效笔势。
- 成功激活在效果前通过T400原子扣能；相同技能在`timestamp < activatedAt + cooldownSec`时冷却，等于边界允许。效果异常视为配置/运行时致命错误，不静默跳过未知类型。
- `TargetType`解释固定为：`Target`取显式主目标；`NextStroke`与`Battle`为世界作用域；`EnemiesInRadius`、`LastStrokeTargets`、`EnemiesInsideGesture`使用调用方预计算标记；`AllEnemies`取存活敌方；`NormalEnemies`取非Boss敌方（含普通/精英）；`Boss`只取Boss。目标保持世界提供的稳定顺序，不按Unity对象遍历或反射重排。
- 显式`IEffectExecutor`注册表冻结12类：`Damage`、`Heal`、`ApplyBuff`、`RemoveArmor`、`Knockback`、`RepeatStroke`、`TimeScale`、`ExecuteBelowHpRatio`、`DamageMultiplier`、`IncrementCounter`、`PlayVfx`、`ClearProjectiles`。`ApplyBuff.durationSec > 0`覆盖Buff默认持续时间，否则读取`Buffs.durationSec`；其他伤害、倍率、时长、Buff/VFX/Audio键均只来自当前效果行。
- `condition`为空时执行；非空只允许`identifier`加`>=`、`<=`、`==`、`!=`、`>`或`<`和InvariantCulture数值（当前内容为`comboCount>=3`）。缺少调用方变量时条件不成立；语法非法必须在扣能前失败。
- `PlayerCombatModel.Heal`把存活玩家HP封顶到配置`Players.maxHp`并发布正数`HpChanged`；HP为0时返回`AlreadyDead`，治疗执行器不能复活。复活若进入范围必须另立配置和状态机合同。
- 当前终极`fx_ultimate_seal`顺序冻结为：`TimeScale(Battle)`→`ClearProjectiles(Battle)`→`Damage(AllEnemies)`→`ExecuteBelowHpRatio(NormalEnemies)`→`ApplyBuff(Boss)`。T410只提供执行管线和有效笔势入口；T420敌人适配与T440实际弹池已经提供，T510 `UltimateDrawing`流程与T600 HUD分别后续接入。

## 15. T420通用敌人状态、伤害与弱点运行时语义

- T420不升级配置版本。`EnemyDefinitionFactory`只从`Enemies`及其`defenseRuleId/weakpointRuleId`外键映射HP、层级、移动/攻击策略ID、护甲、破甲效果、弱点窗/半径/倍率/奖励和资源键；`EnemyAttackTimelineFactory`只从敌人当前`attackSetId`分组内的`EnemyAttacks`行映射前摇、有效段、打断笔势/窗口与效果组。Prefab、Inspector和控制器不保存这些数值的副本。
- `EnemyStateMachine`是无`MonoBehaviour`依赖的纯规则，状态集合固定为`None/Spawn/Move/Windup/Attack/Recovery/Stun/Dead`。`cooldownSec`解释为从攻击开始到再次可攻击的完整周期，恢复时长为`cooldownSec - windupSec - activeSec`；配置若不能覆盖前摇与有效段则拒绝。外部单调时间戳可一次追赶多个状态边界。
- 弱点窗和攻击打断窗都以当前攻击开始时刻为零点，起止边界均包含。弱点Collider只在`WeakpointRules.windowStartSec <= elapsed <= windowEndSec`且状态为Windup/Attack时启用；一次T360弱点命中只有同时满足`interruptAttack=true`、当前攻击的`gestureInterruptType`和`interruptStartSec/interruptEndSec`才打断。
- 弱点/攻击行没有眩晕持续时间，因此弱点打断进入无限期`Stun`，必须由后续战斗/策略流程显式调用恢复；配置`Buffs.type=Stun`仍使用效果传入或Buff默认的配置时长，并在边界自动恢复。不得以Inspector常量或动画长度猜测替代缺失的配置时长。
- `EnemyDamageModel`先用`DefenseRules.armorHp`吸收来伤，溢出伤害进入HP；护甲从正数首次降为0时只发布一次该行`breakEffectGroupId`意图。HP首次归零只触发一次死亡，死亡后伤害与治疗不改变状态且治疗不能复活；回收清空HP、护甲、Buff、计数、攻击、弱点、时钟和目标ID后才允许复用。
- `EnemySkillEffectTarget`在Skills程序集把T410目标端口适配到通用敌人，支持伤害、治疗、Buff、破甲、击退意图、低血处决和计数；Actors不反向依赖Skills。移动/攻击/防御/支援策略见T430，对象池见第17节，Boss阶段覆盖攻击集/防御/弱点归T460。

## 16. T430敌人移动、攻击、防御与支援策略语义

- 配置合同升级为schema `4` / content `0.4.x`。`BuffType`新增`DamageReduction`；`buff_shield_50`、`fx_puppet_shield`和`text_buff_shield`分别冻结护盾Buff、支援效果与显示文案。该Buff按`max(0, 1 - magnitude * stacks)`组合来伤倍率，当前`magnitude=0.5`即减伤50%；持续时间和叠层只读取Buff/Effect配置，并在配置边界到期。
- `MovementStrategyRegistry`显式登记`Linear/Sine/Dive/Hover/Boss`，未知类型立即失败。起止归一化坐标投影到`Global.reference_width/reference_height`，基础速度为`Enemies.moveSpeedRefPxSec * MovePatterns.speedMultiplier`；循环路径使用往返进度，`Sine/Hover`只读取配置振幅与频率，`Dive`使用确定性二次缓入。采样器不拥有时间缩放常量，调用方必须在Root时停止推进移动时钟，并按Slow等配置效果缩放外部delta。
- `AttackStrategyRegistry`显式登记`Cooldown/Distance/Support/HpThreshold`。触发上下文只接收调用方已经判定的距离、支援目标和HP阈值事实，不在策略层猜测表中不存在的距离或比例；候选按配置`order`稳定、按`weight`选择。动作从当前攻击效果与弹体配置推导为近战、投射物、冲撞或支援，投射物伤害必须与攻击伤害合同一致，支援动作必须携带明确目标。
- `EnemyAttackTelegraph`在`BeginAttack`成功时先公开攻击种类、打断笔势、效果组与预期执行时刻；`EnemyStrategyRuntime`在T420状态机的`Windup -> Attack`边界恰好执行一次动作并关闭预警。动画事件可消费状态做表现，但不是伤害或支援生效的唯一真相。
- `DefenseRuleService`只把配置的防御笔势、架势门、命中/失败倍率、反伤值和破甲效果映射为不可变结果，不复制T360伤害公式，也不直接结算HP。对象池合同见第17节；敌人内容装配与Boss阶段覆盖仍分别属于T450和T460。

## 17. T440通用对象池、预热、耗尽与完整重置语义

- T440不改变JSON字段形状，保持schema `4`并把content升级为`0.5.x`。四类池的共享活动容量分别读取`Global.max_active_enemies`、`max_active_projectiles`、`max_active_vfx`和`damage_number_pool_size`；耗尽策略分别读取新增的`enemy_pool_exhaustion_policy`、`projectile_pool_exhaustion_policy`、`vfx_pool_exhaustion_policy`和`damage_number_pool_exhaustion_policy`，值只能是Enums登记的`Reject`或`ReuseOldest`。
- 敌人每ID预热数读取`Enemies.poolPrewarm`，VFX每Key预热数读取`VfxCues.poolPrewarm`，投射物每ID统一读取新增的`Global.projectile_pool_prewarm_per_type`，伤害数字默认池按`damage_number_pool_size`完整预热。预热只分配并彻底回收对象，不占活动容量；同一family下的不同ID池共享活动上限。
- `ObjectPoolService`与`IPoolable`位于无`MonoBehaviour`依赖的Core程序集。每次成功获取都发出包含pool/family、generation和单调激活序号的租约；显式释放验证对象所有权与精确租约，重复释放、未知对象和旧租约均不得影响当前对象。重开先回收全部活动租约再递增generation，泄漏报告只列出仍持有活动租约的对象。
- `Reject`在family达到活动容量时不创建、不激活也不回收任何对象；`ReuseOldest`按family内激活序号确定性选择最旧对象，先执行其完整`ReleaseToPool`，再从请求的目标ID池获取。容量判断不以`GameObject.activeSelf`代替租约状态；弹体自行因命中/寿命停用后，拥有者仍必须把对应租约交还服务，才算释放family容量。
- 敌人回收清空配置定义、HP/护甲、Buff、计数、攻击/弱点/时钟/目标ID、外部`CombatEventPublished`订阅、事件序号和Transform；投射物回收清空规则、归属、参考空间、位置/方向/时间、命中ID、Collider和Transform。二者都恢复到捕获的池父节点并停用，下一次获取后必须由新生成参数完整覆盖。
- `VfxPoolItem`只保留由`VfxCues`确定的不可变池配置，回收清空目标、播放/完成状态、计时和Transform；`DamageNumberPoolItem`回收清空金额、目标、来源、可见状态和Transform。VFX生命期、跟随与排序仍只从配置读取；T440不实现T620反馈编排，也不装配T450敌人内容、T460 Boss阶段或T500关卡流程。
