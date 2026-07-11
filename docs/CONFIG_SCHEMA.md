# CONFIG_SCHEMA：配置表契约

## 1. 唯一真相源

`Design/Config/GameConfig.xlsx` 是策划内容源；导出的 `Assets/_Game/Config/Generated/gameplay_config.json` 是可审查、可构建的版本化快照。C#和Inspector都不能成为第二套平衡数据。

运行时只读取JSON。Unity对象引用由 `AssetRegistrySO` 通过 `assetKey` 提供，它不是数值主库。

## 2. 表清单

| Sheet | 主键或组合键 | 内容 |
|---|---|---|
| Global | key | 全局阈值、预算、时间、上限 |
| Players | playerId | 玩家基础值与默认架势 |
| Stances | stanceId | 刀/符架势倍率与资源 |
| StrokeRules | ruleId | 笔势阈值与命中半径 |
| DamageFormulas | formulaId | 伤害与连斩公式参数 |
| DefenseRules | defenseRuleId | 护甲、方向、破甲 |
| WeakpointRules | weakpointRuleId | 弱点位置、窗口、倍率 |
| MovePatterns | movePatternId | 线性、漂浮、俯冲等移动策略参数 |
| Enemies | enemyId | 敌人主数据与策略ID |
| EnemyAttacks | attackId | 前摇、伤害、弹幕、打断窗口 |
| Projectiles | projectileId | 弹道、切断、反弹 |
| Buffs | buffId | 状态效果与叠层 |
| Skills | skillId | 触发、CD、能量、效果组 |
| SkillEffects | effectGroupId+order | 有序效果链 |
| Levels | levelId | 关卡入口、背景、奖励和星级 |
| Waves | waveId | 波次条件、时间和顺序 |
| SpawnPoints | spawnPointId | 归一化出生位置、抖动、朝向和通用范围 |
| EnemyModifiers | modifierId | 普通/精英等出生修饰倍率 |
| Spawns | spawnId | 出生时间、位置、数量和模式 |
| BossPhases | bossPhaseId | 阶段条件、策略与进入动作 |
| Rewards | rewardTableId+order | 分数、解锁和预留货币 |
| Tutorials | tutorialId+order | 事件驱动教学步骤 |
| Texts | textKey | 中文和英文文案 |
| AudioCues | audioKey | 音频资源键与并发参数 |
| VfxCues | vfxKey | VFX资源键、生命期和池预热 |
| AssetManifest | assetKey | 配置期预期资源键与类型 |
| Enums | enumType+value | 策划枚举字典 |
| FieldDictionary | sheet+field | 字段类型、约束与说明 |

## 3. 编写规则

- ID只使用小写英文、数字和下划线；发布后不可复用为其他语义。
- 表头是API；改名必须同步导出器、DTO、校验、文档和测试。
- 小数使用`.`；时间统一为秒；位置使用Safe Area归一化坐标0～1。
- 一对多关系使用子表，不在单元格塞JSON。
- 非关键轻量列表可用英文逗号分隔，导出时去空格并稳定排序。
- 空白行忽略，必填字段空白即错误。
- 不推荐把Excel公式结果作为运行时输入；需要时必须有明确读取与测试约定。

## 4. 必须校验

- 主键或组合键唯一，必填字段非空。
- 数值范围、枚举、布尔和类型转换。
- 所有外键存在；assetKey在清单和Unity Registry中存在。
- Skill的effectGroup非空，order连续。
- Level → Wave → Spawn完整，波次order连续，时间非负。
- Boss阶段顺序唯一，HP阈值严格递减且在0～1。
- 敌人引用的策略ID由代码注册。
- 教程requireAction引用有效笔势或技能。
- 文案键、音频键和VFX键存在。
- 任意错误都阻止整份配置进入战斗，不能半应用。

## 5. Runtime约定

- JSON根包含 `schemaVersion`、`contentVersion`、`contentHash` 和各表数组。
- `GameplayConfigService`启动时一次性反序列化并构建只读索引。
- 业务代码通过 `IConfigProvider.GetEnemy(id)` 等API访问，不直接遍历原始DTO。
- 启动日志打印配置来源、版本、hash、记录数和校验摘要。
