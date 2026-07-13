# CONFIG_SCHEMA：配置表冻结契约

## 1. 版本与唯一真相源

- 当前冻结版本：`schemaVersion = 4`、`contentVersion = 0.5.2-sample`。
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

## 18. T450敌人原型目录与内容装配语义

- T450不修改JSON字段形状或内容版本。`IConfigProvider.GetEnemies()`以只读快照枚举`Enemies`；`EnemyArchetypeCatalog`排除`tier=Boss`后按`enemyId` Ordinal稳定排序，并沿外键聚合`EnemyDefinition`、移动策略、有序攻击、防御、弱点、中英文名称和资源类型。业务层不维护六怪ID列表或每怪子类。
- 非Boss原型必须至少有一个攻击；每个攻击必须有正前摇，且配置打断窗必须跨过`Windup -> Attack`执行边界。教学特征摘要只组合层级、移动类型、架势易伤、防御笔势/架势、弱点窗和攻击/弹体交互语义，不包含敌人/攻击ID或数值；当前非Boss目录中重复摘要会拒绝装配，防止只换名称的重复内容。
- `EnemyArchetypePool`为目录中每个原型注册T440敌人池，预热仍只读`Enemies.poolPrewarm`。`AssetManifest.assetType=Sprite`时在通用容器上绑定Registry Sprite，`Prefab`时实例化Registry Prefab；两条路径都只补齐通用`Damageable/EnemyController/EnemyArchetypeActor`和配置需要的弱点组件，不把HP、速度、攻击或半径写入Inspector。
- `EnemyArchetypeActor`在获取精确池租约后才能Spawn，由同一原型快照创建`EnemyStrategyRuntime`并把移动样本写入调用方参考空间。回收/重开/最旧复用先释放策略订阅，再委托`EnemyController.ReleaseToPool`完整清空运行态；旧租约不能释放重用后实例。
- 当前五个Sprite键和一个Prefab键继续使用T240的类型正确占位引用；T450只证明内容规则与运行时装配，不把占位资源伪报为正式动画/美术。Boss阶段覆盖属于T460，刷怪时间轴与触发事实属于T500，正式原型资源替换属于T630。

## 19. T460配置驱动Boss阶段与切换语义

- T460不改变JSON字段形状或schema，content升级为`0.5.1-sample`。`BossPhaseCatalog`只从`BossPhases`及其移动、攻击、防御、弱点、进入效果和文案外键构造不可变阶段；所属敌人必须是`tier=Boss`，order必须从1连续，首阶段从HP比例1进入、末阶段退出到0，相邻阶段阈值必须精确相接且严格下降。
- 每个阶段通过`EnemyDefinitionFactory.CreateBossPhase`覆盖`movementPatternId/attackSetId/defenseRuleId/weakpointRuleId`，同时保留同一Boss的HP上限、基础速度、资源和层级。当前镇墓玄甲王三阶段分别使用`move_boss_ground/move_boss_phase2/move_boss_phase3`，表内速度倍率为`0.5/0.8/1.2`；最终参考速度由`Enemies.moveSpeedRefPxSec × MovePatterns.speedMultiplier`计算，不在Boss代码或Inspector复制。
- `BossPhaseStateMachine`是无`MonoBehaviour`依赖的纯规则，只比较调用方传入的当前HP比例与表内退出阈值。等于边界时进入下一阶段；重复观察或HP回升不重复事件；一次非致死伤害跨越多个阈值时仍按配置顺序逐阶段发布进入事件，不能跳过中间进入动作；致死伤害直接进入死亡语义，不在尸体上执行后续阶段动作。
- `BossPhaseController`监听`EnemyController`的HP变化并在阶段边界取消旧攻击/眩晕状态，切回`Move`后原子应用新阶段定义。当前HP和HP上限保持不变，护甲按新阶段防御规则重置，弱点规则立即替换，旧策略订阅先释放，再从新攻击集和移动模板创建运行时。
- `onEnterEffectGroupId`统一经T410 `SkillService.ExecuteEffectGroup`按配置order执行，并把Boss作为明确主目标；Boss阶段事件在配置更新、策略重建和进入效果完成后每阶段只发布一次。切换时刻不读取动画时长，动画仅可消费阶段/状态事件做表现。
- 当前样例的三段HP区间为`[1,0.67]`、`[0.67,0.34]`、`[0.34,0]`，护甲为`120/60/0`，弱点由无弱点切为封印弱点；这些都是当前配置内容而非C#常量。修改阈值、速度、防御或弱点只需重导配置并通过连续覆盖校验。

## 20. T500关卡、波次与出生时间轴语义

- T500不改变JSON字段形状、schema或content版本。`LevelCatalog`只沿`Levels -> Waves -> Spawns -> SpawnPoints/EnemyModifiers`构造不可变关卡定义；波次必须按关卡内`order`从1连续，第一波不能使用`PreviousWaveEnd`，后续波不能重新使用`LevelStart`，出生点只能属于当前关卡或使用受控通配符`*`。`BossDefeated`波必须生成`Levels.bossEnemyId`指定的Boss。
- `LevelStart`和`TimeElapsed`的`startDelaySec`都从关卡时钟零点解释；`PreviousWaveEnd`从上一波完成时刻解释；`PlayerConfirmed`必须先收到当前等待门的显式玩家确认，再从确认时刻计算`startDelaySec`。提前确认、暂停期间确认和用于其他波次的旧确认都不能被缓存到未来动作门。
- `AllEnemiesDefeated`只在本波全部计划出生已成功提交且活动实体为0时成立；`BossDefeated`只在世界端口回报本波配置Boss实体死亡时成立；二者和`PlayerConfirmed`成立后再等待`endDelaySec`。由于Waves没有独立持续时间字段，`TimeElapsed`下的`endDelaySec`明确解释为从本波开始到结束的持续时间，不再叠加第二段结束延迟。
- 每个出生时刻为`spawnTimeSec + occurrenceIndex × intervalSec`，跨多行按到期时刻、`spawnId` Ordinal和行内序号稳定排序。`maxAlive`形成背压：容量满或世界端口暂时拒绝时保留当前请求，槽位释放后重试，不跳过、不重排；世界接受后必须返回正且当前唯一的实体ID。
- `Single/Line/Scatter/Stagger`只决定如何在配置的`normalizedX/Y ± jitterX/Y`矩形内取点；最终坐标夹在`[0,1]`，保持`lane/facing`并不读取设备DPI。Scatter使用`spawnId + occurrenceIndex`确定性采样，回放相同配置得到相同位置；出生请求完整携带`EnemyModifiers`的HP、伤害、速度、评分、染色和额外Buff，具体敌人世界适配器不得忽略或另写数值。
- `LevelRunner`只由调用方delta推进单调关卡时钟；暂停时delta、出生、波次条件和玩家确认都不推进。`Levels.durationLimitSec`只公开为到时事实，Countdown、暂停/失焦策略、Victory/Defeat互斥和清场属于T510，不由T500时间轴提前裁决。

## 21. T510战斗流程、时间域与玩家事件门语义

- T510不改变JSON字段形状、schema或content版本。`BattleFlowSettingsFactory`只读取`Global.battle_countdown_sec`、`Global.pause_on_focus_lost`，再沿调用方`playerId -> Players.ultimateSkillId -> Skills`取得终极ID、`triggerType`、`gestureType`和`inputWindowSec`；终极必须是`Ultimate`触发、配置非空笔势和正输入窗。流程代码、Inspector和场景不复制倒计时、时间缩放或输入窗数值。
- `BattleTimeSource`统一维护未暂停流程时间、未缩放战斗时间和受效果缩放的战斗时间。Countdown只推进流程时间；Playing与UltimateDrawing把同一个受缩放delta交给LevelRunner及后续战斗消费者；Paused和Victory/Defeat不推进任何时间。`SkillEffects.TimeScale.value1/durationSec`由T410世界端口显式传入并在配置持续时间后恢复1倍，暂停期间不消耗持续时间。
- `PlayerConfirmed`只允许在Playing由显式调用转发到T500当前门。Countdown、UltimateDrawing、Paused、终态和提前确认都不能锁存或跨过未来门。倒计时允许用计时器转入Playing；终极输入窗超过边界只能取消，不能发布成功或替代玩家有效笔势事件。
- UltimateDrawing先用`CanAcceptUltimateGestureEvent`拒绝零值、旧值或非当前绘制事件，再把通过门的单调`gestureEventId`交给T410；只有相同事件对应当前`Players.ultimateSkillId`的`SkillActivationResult.Activated`才发布成功并回到Playing。同一笔迹事件不能跨绘制重放；无效笔势、超窗、冷却、架势、能量或死亡拒绝均只取消本次绘制。等于`Skills.inputWindowSec`的有效事件仍可接受，严格超过才超时。
- `pause_on_focus_lost=true`时FocusLost与ApplicationPaused是可叠加原因：首次进入暂停发布一次活动笔迹取消请求，全部原因解除后才恢复。暂停Countdown保留已走时间；暂停UltimateDrawing取消当前笔迹并以Playing为恢复目标，不能恢复过期的终极输入。
- 同一次结算事实中PlayerDied或`Levels.durationLimitSec`到时优先于LevelCompleted，结果为Defeat；否则LevelCompleted为Victory。首次进入终态后冻结流程并拒绝后续结算，因此Victory/Defeat互斥且`Settled`每局最多一次。T510不负责T550奖励/存档或T600结算UI。

## 22. T520事件驱动教学步骤与关卡门控语义

- T520不改变JSON字段形状或schema，只把content升级为`0.5.2-sample`。`lv_001_tutorial`固定从`Levels.tutorialId -> Tutorials`取得教学组，从`Texts`取得提示文案；当前内容为6个连续步骤、6波、6条出生行、15个敌人和180秒配置上限。波次、敌人、提示、最短展示、触发事件、完成事件、手势及是否阻塞均不得复制到Inspector或产品C#。
- `triggerEvent`与`completeEvent`使用显式协议注册表；当前支持战斗就绪、弱点出现、多目标波、投射物、重甲、幽魂和终极就绪等触发，以及有效笔迹、弱点命中、命中数阈值、切弹、破甲、架势切换和终极成功等完成事件。阈值语法只允许`event>=positiveInteger`，当前`StrokeHitCount>=3`在值2时拒绝、值3时成立；未知事件、未知手势或非法阈值在创建运行时定义时失败。
- 每一步先处于`WaitingForTrigger`，只接受当前配置触发；触发后进入`Active`并开始累计未暂停的玩法展示时间。只有当前配置完成事件和手势匹配才锁存完成事实；若玩家动作早于`minDisplaySec`，事实保持锁存并在达到边界时完成，不要求玩家重复。单独推进任意长时间、错误事件或未来步骤事件都不能完成当前步骤。
- `blockProgress=true`只在步骤已经触发且仍为Active时阻止当前波次结算，不冻结关卡时钟、出生、敌人或战斗输入；步骤完成并等待下个触发时立即解除。这样教学提示可以观察真实玩法事件，又不会让计时器或清空敌人跨过尚未完成的动作。暂停时T510提供的玩法未缩放delta为0，教学展示时间也不推进；慢动作只缩放战斗世界，不延长配置的真实展示下限。
- 最后一波使用`PlayerConfirmed`结束条件，但只有整个教学序列因配置终极技能的有效Circle结果完成后，协调器才向T500当前门发送一次确认。终极仍必须先通过T510单调gestureEventId门和T410技能/能量/输入窗门；教学事件不能伪造技能成功。T520只实现纯编排与原型玩家路径，不制作T650正式遮罩、手势示意、跳过/回看或复杂剧情。

## 23. T530混合怪物普通关编排语义

- T530不改变JSON字段形状或schema，content升级为`0.5.3-sample`。`lv_002_cave`完全沿既有`Levels -> Waves -> Spawns -> SpawnPoints/EnemyModifiers`合同编排为8波、23条出生行和45个敌人实例；只引用T450目录中5种普通怪与1种精英怪，不增加关卡专用敌人、策略分支或Inspector数值。
- 八波按四个双波战术段递进：先建立基础近战/投射物混合，再加入耐久前排，然后引入精英支援，最后组合六种原型。第5、7、8波的摄魂道傀出生行引用既有精英修饰器；修饰器由T500请求透传到世界端口，关卡表不复制敌人基础属性。
- `maxAlive`按波次从5递进到8，并不得低于该波配置可能达到的合理并发需求；容量满时继续服从T500背压合同，不吞怪。需要不同架势处理的危险目标，其配置出生时刻至少错开当前最大架势切换冷却1秒，避免把同时互斥输入制造成不可解组合。
- 当前普通关时限为210秒，星级阈值为6500/9500/13000。波次起止延迟、出生时间、数量、间隔、组合、修饰器、容量、时限和评分阈值都只允许在工作簿调整；重新导出并加载同一运行时代码后必须产生对应的新节奏和人口结果。
