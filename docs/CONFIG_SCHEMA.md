# CONFIG_SCHEMA：配置表冻结契约

## 1. 版本与唯一真相源

- 当前冻结版本：`schemaVersion = 1`、`contentVersion = 0.1.1-sample`。
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
- `FieldDictionary` 有248条记录，完整描述其余27个数据Sheet（包括 `Enums`），但不递归描述自身。`FieldDictionaryRow` 的JSON结构由Schema直接定义。
- 表头是API，顺序必须与Schema `$defs/*Row.properties`、`required`及样例JSON对象字段一致。

## 4. Sheet、主键与分组

| Sheet | 行主键或组合键 | 分组/引用语义 | 内容 |
|---|---|---|---|
| Global | key | — | 全局阈值、预算、时间、上限 |
| Players | playerId | — | 玩家基础值与默认架势 |
| Stances | stanceId | — | 刀/符架势倍率与资源 |
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
- 敌人引用的策略ID由代码注册；文案、音频、VFX和资源键存在。
- 任意错误都阻止整份配置进入战斗；错误必须定位Sheet、Excel行、字段和稳定错误码。

## 10. Runtime约定

- JSON根包含 `schemaVersion`、`contentVersion`、`contentHash` 和28个表数组。
- `GameplayConfigService` 启动时一次性反序列化、校验版本并构建只读索引。
- 业务代码通过 `IConfigProvider.GetEnemy(id)` 等API访问，不直接遍历或修改原始DTO。
- 启动日志打印配置来源、版本、hash、记录数和校验摘要。
- 业务代码可使用`ConfigIds`避免魔法字符串，但常量只表达稳定ID，不复制配置数值或对象引用；Runtime仍以JSON索引为内容真相。
