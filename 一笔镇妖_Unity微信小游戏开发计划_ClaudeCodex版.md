# 《一笔镇妖》Unity 微信小游戏开发计划
## Claude Code / Codex 可执行版

> 文档状态：开发合同草案
> 生成日期：2026-07-11
> 项目形态：Unity 2D 横屏手势动作游戏，目标平台为微信小游戏
> 计划总量：49 个原子任务，约 84.0 人日（不含正式美术重绘、商店审核等待和大规模内容量产）
> 当前允许执行任务：`T010`（T000已完成）

---

## 0. 如何直接交给 Claude Code 或 Codex

把本文件和开发包中的 `AGENTS.md`、`CLAUDE.md`、`project-index.yaml`、`docs/`、`tasks/`、`prompts/`、`config/` 一起复制到仓库根目录。

首次执行时，不要让代理直接“做完整游戏”。把下面这段作为第一条指令：

```text
完整读取并严格遵守 AGENTS.md。
然后读取 project-index.yaml、docs/GAME_DESIGN_MVP.md、docs/MVP_SCOPE.md、
docs/TECH_SPEC.md、docs/CONFIG_SCHEMA.md、docs/TASKS.md、docs/PROGRESS.md。
只执行第一个依赖均为 DONE 的 READY 任务。
开始前输出：任务ID、目标、不做项、预计改动白名单、验收路径、专项测试、Git基线和外部工具门。
完成后运行最小反馈环，保存 artifacts/evals/TASK-ID/verification.md，
更新 TASKS/PROGRESS/project-index，只提交当前任务，然后停止。
```

此后每轮使用 `prompts/01_NEXT_TASK.md`。配置修改使用 `prompts/03_CONFIG_CHANGE.md`，微信平台验证使用 `prompts/04_WECHAT_SPIKE.md`。

---

## 1. 项目目标与MVP范围

### 1.1 一句话定义

玩家在横屏手机上用手指划出刀光或符线，在妖怪攻击命中前完成斩击、弱点打断、弹幕切断、一笔连斩和终极封印。

### 1.2 MVP交付物

- 1 名主角：玄狸·墨砚。
- 2 种架势：斩妖刀、镇魂笔。
- 5 类普通怪物、1 类精英怪、1 个三阶段Boss。
- 3 个关卡，其中第3关为Boss关。
- 1 个完整教程、胜利/失败/暂停/重开/关卡解锁。
- 单关约3–5分钟。
- Unity Editor、标准Web构建、微信转换、微信开发者工具和至少一台真机的分级验证。
- 所有玩法数值、敌人、攻击、技能、效果、关卡、波次、出生、Boss阶段、奖励、教程和文案都来自配置表。
- 原型可直接使用当前PSD导出的静态素材；正式版本再按计划拆件与统一风格。

### 1.3 MVP明确不做

- 联机、服务器权威、实时PVP、公会。
- 抽卡、广告、内购和复杂经济。
- 自由移动、开放世界、程序化关卡。
- 大规模角色养成和装备系统。
- 机器学习手写识别。
- 把Excel留在运行时解析。
- 在Gameplay代码中直接调用微信SDK。
- 未经真机验证就宣称平台PASS。

---

## 2. 技术基线

- 引擎：当前工程固定 Unity `6000.5.1f1`；变更版本前必须先完成平台兼容性Spike并记录决策。
- 模板与渲染：Universal 2D / URP 2D Renderer。
- 输入：Unity Input System，鼠标与触摸统一为指针事件。
- UI：uGUI + TextMeshPro；中文字体必须有动态字形或覆盖字符集与Fallback。
- 方向：横屏，参考分辨率1920×1080，Safe Area适配。
- 测试：Unity Test Framework；纯逻辑EditMode，Unity接线和生命周期PlayMode。
- 构建：先标准Unity Web，再进行微信小游戏转换。
- Web约束：核心代码不依赖托管线程、任意文件系统路径或运行时xlsx解析。
- 数据：构建期Excel导出稳定JSON；运行时只读JSON。
- 资源：`AssetRegistrySO`只保存 `assetKey → Unity Object` 引用，不保存平衡数值。
- 平台：`IPlatformService` 抽象保存、生命周期、震动、分享和平台能力。

### 2.1 推荐目录

```text
repo/
├─ AGENTS.md
├─ CLAUDE.md
├─ project-index.yaml
├─ Design/Config/GameConfig.xlsx
├─ Tools/ConfigExporter/
├─ docs/
├─ artifacts/evals/
└─ game/
   ├─ Assets/_Game/
   │  ├─ Art/{Characters,Enemies,Backgrounds,UI,VFX,Audio,Fonts}
   │  ├─ Config/{Registry,Generated}
   │  ├─ Prefabs/{Actors,Projectiles,UI,VFX}
   │  ├─ Scenes/{Bootstrap,MainMenu,Battle}
   │  ├─ Scripts/{Core,Config,Input,Combat,Actors,Skills,Levels,Presentation,Platform}
   │  └─ Tests/{EditMode,PlayMode}
   ├─ Packages/
   └─ ProjectSettings/
```

### 2.2 程序集边界

```text
OneStrokeDemon.Core
OneStrokeDemon.Config        -> Core
OneStrokeDemon.Input         -> Core
OneStrokeDemon.Combat        -> Core, Config, Input
OneStrokeDemon.Actors        -> Core, Config, Combat
OneStrokeDemon.Skills        -> Core, Config, Combat, Actors
OneStrokeDemon.Levels        -> Core, Config, Actors, Skills
OneStrokeDemon.Presentation  -> Core, Config, Actors, Levels
OneStrokeDemon.Platform      -> Core
OneStrokeDemon.Editor        -> Config, UnityEditor
OneStrokeDemon.Tests.EditMode
OneStrokeDemon.Tests.PlayMode
```

禁止形成循环依赖。纯规则层不得引用MonoBehaviour、GameObject、微信SDK或场景对象。

---

## 3. 核心玩法实现规格

### 3.1 手势处理链

```text
PointerDown
→ 参考分辨率坐标归一化
→ 最小采样距离过滤
→ 轨迹长度上限精确裁剪
→ RDP简化
→ 最大点数重采样
→ 笔势特征提取
→ 手势分类
→ 统一轨迹渲染与几何命中
→ 按轨迹参数排序命中目标
→ 结算伤害、连斩、能量和评分
```

必须保证显示轨迹和命中使用同一组处理后的点。

### 3.2 MVP手势

| 手势 | 用途 |
|---|---|
| Any | 普通斩击 |
| Horizontal | 地面群体、横向破盾 |
| Vertical | 重甲破甲、符印 |
| Diagonal | 飞行目标、暴击奖励 |
| Arc | 范围幽魂攻击 |
| Circle | 束缚和终极封印 |
| Charged | 蓄力破盾/破甲 |

所有角度、长度、闭合距离、面积、曲率、停留时间、宽度和伤害阈值均来自配置表。

### 3.3 玩家流程门

终极画符、教程确认、关卡开始等玩家动作必须由事件推进，不能用“计时器到点”伪造玩家确认。计时器只能表达最短展示时间、允许窗口或明确的失败/取消策略。

### 3.4 敌人架构

不为每个怪物创建深继承类。使用：

```text
EnemyController
+ EnemyRuntimeState
+ EnemyDefinition
+ IMovementStrategy
+ IAttackStrategy
+ IDefenseStrategy
+ IWeakpointStrategy
+ BuffContainer
+ PoolableActor
```

怪物差异由配置ID与可组合策略实现。MVP敌人：

- `enemy_fire_fish`：基础目标、火符弹幕。
- `enemy_wheel_zombie`：地面推进、横斩教学。
- `enemy_stone_turtle`：正面护甲、方向/蓄力破甲。
- `enemy_skeleton_ghost`：漂浮、符架势克制。
- `enemy_talisman_bat`：高速俯冲、斜斩打断。
- `enemy_soul_puppet`：精英支援、护盾与符纸顺序。
- `boss_tomb_armor_king`：符钉、方向封印、冲撞处决。

### 3.5 对象池

敌人、弹幕、伤害数字、命中特效、斩击残影和音效实例均池化。回池和再次启用必须重置：

- HP、状态、Buff、冷却、速度、护甲和弱点窗口。
- 事件订阅、动画参数、Sprite材质状态。
- 当前攻击、协程、延迟回调。
- 所属波次、击杀计数、掉落和统计标记。

---

## 4. 配置驱动方案

### 4.1 唯一真相源

```text
Design/Config/GameConfig.xlsx
→ 独立.NET导出器
→ 结构/类型/唯一性/范围/枚举/外键/跨表校验
→ 稳定排序的 gameplay_config.json
→ Unity DTO反序列化
→ Runtime语义校验与只读索引
→ GameplayConfigService
→ 运行时系统
```

- Excel是策划源。
- JSON是可审查、可构建的版本化快照。
- Inspector和ScriptableObject不是第二数值库。
- 任何坏配置必须整包拒绝，禁止半应用。
- 启动日志必须输出Schema版本、内容版本、来源、校验摘要和哈希。
- 配置导出必须是确定性的：同样输入产生字节级稳定输出。

### 4.2 工作表清单

| 工作表 | 配置内容 |
|---|---|
| Global | 全局预算、时长、上限、阈值 |
| Players | 玩家生命、能量、默认架势 |
| Stances | 刀/符倍率、轨迹宽度和切换效果 |
| StrokeRules | 手势采样、简化、判定阈值 |
| DamageFormulas | 基础伤害、暴击、一笔倍率 |
| DefenseRules | 护甲、方向要求、破甲 |
| WeakpointRules | 弱点窗口与倍率 |
| MovePatterns | 敌人线性、漂浮、俯冲和Boss移动策略参数 |
| Enemies | 敌人主数据、策略和资源键 |
| EnemyAttacks | 前摇、伤害、冷却、弹幕和打断 |
| Projectiles | 弹道、切断、反弹、生命期 |
| Buffs | 减速、眩晕、燃烧、定身等 |
| Skills | 技能触发、CD、能量和效果组 |
| SkillEffects | 有序效果链 |
| Levels | 关卡入口、场景、奖励、星级 |
| Waves | 波次顺序、开始/结束条件 |
| SpawnPoints | 归一化出生位置、抖动、朝向和通用范围 |
| EnemyModifiers | 普通/精英出生倍率与附加状态 |
| Spawns | 出生时间、数量、间隔、位置 |
| BossPhases | 阶段阈值、策略与进入动作 |
| Rewards | 分数、解锁和预留奖励 |
| Tutorials | 事件驱动教学步骤 |
| Texts | 中英文文案 |
| AudioCues | 音频资源键与并发 |
| VfxCues | 特效资源键、生命期、池预热 |
| AssetManifest | 配置期预期资源键与类型 |
| Enums | 策划枚举字典 |
| FieldDictionary | 字段类型、必填、范围、外键和说明 |

### 4.3 配置变更完成定义

新增或修改任何字段时，必须同步：

1. Excel模板/正式工作簿。
2. `FieldDictionary`。
3. 独立导出器。
4. JSON Schema。
5. 版本化JSON快照。
6. Unity DTO。
7. 结构与语义校验器。
8. 运行时索引。
9. 配置文档。
10. EditMode/PlayMode测试。

缺一项不得标记DONE。

---

## 5. 微信小游戏平台策略

平台任务前置，避免玩法完成后才发现SDK不兼容。

### 5.1 分级结论

| 层级 | 必须保存的证据 | 允许结论 |
|---|---|---|
| Unity Editor | Play Mode、输入、音频、Console | Editor PASS |
| 标准Unity Web | 构建日志、产物、浏览器冒烟 | Web PASS |
| 微信转换 | 固定SDK版本/commit、转换日志、产物 | Transform PASS |
| 微信开发者工具 | 启动、触控、音频、暂停恢复、截图 | DevTools PASS |
| 真机 | 至少一台手机的输入、音频、内存、前后台 | Device PASS |

上一级不能代替下一级。缺少DevTools或真机时只能写 `KNOWN ISSUE` 或 `BLOCKED`。

### 5.2 SDK规则

- 在T110重新核验当前官方支持渠道，不沿用未经验证的旧仓库或旧commit。
- 固定SDK版本或commit，并记录来源、兼容矩阵、补丁和移除条件。
- 第三方源码补丁必须最小化、带Unity版本条件，并保留原始错误证据。
- Gameplay不引用SDK程序集，平台能力只通过 `IPlatformService`。
- 平台关闭/前后台切换必须暂停游戏并安全保存。
- 真机必须验证横屏、安全区、触控采样、音频解锁、内存峰值和重启。

---

## 6. 开发阶段和里程碑

| 阶段 | 成熟度门 | 主要产出 | 估算人日 |
|---|---|---|---:|
| P0 合同与Harness | 可开发 | 统一文档、Git、Unity、目录、测试与证据链 | 4.5 |
| P1 微信平台Spike | 平台可验证 | 先跑标准Web，再验证微信转换、DevTools和真机 | 6.0 |
| P2 配置系统 | 可配置 | Excel→校验→JSON→Unity只读索引→资源注册表 | 9.0 |
| P3 手势战斗核心 | 核心可玩 | 输入、轨迹、几何、命中、方向、切弹、连斩 | 12.5 |
| P4 玩家敌人技能 | 战斗系统完整 | 玩家、敌人、攻击、Buff、技能、Boss、对象池 | 16.0 |
| P5 关卡完整单局 | 可试玩 | 关卡、波次、教程、UI、评分、胜败、存档 | 12.0 |
| P6 表现与资源 | 视觉可评审 | PSD资源接入、动画拆分、VFX、音频、中文字体 | 10.0 |
| P7 质量发布 | 候选版本 | 性能、兼容、平台回归、隐私、构建与发布证据 | 14.0 |

单人配合Claude Code/Codex的现实排期建议为10–14周：前2周完成Harness、平台Spike和配置主链；第3–7周完成核心战斗与内容闭环；第8–10周接入资源和打磨；其余时间用于真机、性能、缺陷与审核缓冲。该估算假设现有PSD素材可用于原型，正式动画和重绘工作另计。

---

## 7. 原子任务总表

> 详细任务合同（目标、输出、不做项、验收、测试、证据目录）在开发包 `docs/TASKS.md`。状态只允许 `BACKLOG / READY / IN_PROGRESS / REVIEW / DONE / BLOCKED`。

| ID | 阶段 | 状态 | 依赖 | 人日 | 目标 |
|---|---|---|---|---:|---|
| T000 | P0 合同与Harness | DONE | — | 0.5 | 统一玩法、MVP范围、技术基线、配置唯一真相源和完成定义。 |
| T010 | P0 合同与Harness | READY | T000 | 1.0 | 验收并纳管仓库根目录现有Unity 6000.5.1f1 2D工程基线。 |
| T020 | P0 合同与Harness | BACKLOG | T010 | 1.0 | 固定URP 2D、Input System、TMP、Test Framework和质量档。 |
| T030 | P0 合同与Harness | BACKLOG | T020 | 1.0 | 建立目录、asmdef和Bootstrap/MainMenu/Battle三场景骨架。 |
| T040 | P0 合同与Harness | BACKLOG | T030 | 1.0 | 建立构建、测试、证据和一任务一提交工作流。 |
| T100 | P1 微信平台Spike | BACKLOG | T040 | 1.0 | 先验证标准Unity Web构建，不接微信转换。 |
| T110 | P1 微信平台Spike | BACKLOG | T100 | 2.0 | 确认当前官方微信Unity转换方案并做Unity/SDK兼容矩阵。 |
| T120 | P1 微信平台Spike | BACKLOG | T110 | 2.0 | 完成微信转换、开发者工具和至少一台真机的分级冒烟。 |
| T130 | P1 微信平台Spike | BACKLOG | T110 | 1.0 | 建立Editor/Web/WeChat平台服务抽象。 |
| T200 | P2 配置系统 | BACKLOG | T040 | 1.0 | 确认Excel工作簿、字段字典、ID规则和数据所有权。 |
| T210 | P2 配置系统 | BACKLOG | T200 | 2.0 | 实现独立.NET配置导出器：xlsx到稳定JSON。 |
| T220 | P2 配置系统 | BACKLOG | T210 | 2.0 | 实现结构、范围、枚举、唯一性、外键和跨表语义校验。 |
| T230 | P2 配置系统 | BACKLOG | T220 | 2.0 | 实现Unity Runtime配置加载、版本检查和只读索引。 |
| T240 | P2 配置系统 | BACKLOG | T230 | 1.0 | 建立assetKey到Unity对象的AssetRegistry，且不保存平衡值。 |
| T250 | P2 配置系统 | BACKLOG | T240 | 1.0 | 把导出、校验、JSON diff和Unity配置测试接入一条命令。 |
| T300 | P3 手势战斗核心 | BACKLOG | T250, T030 | 1.0 | 实现统一指针输入、UI阻挡、Safe Area和参考像素坐标。 |
| T310 | P3 手势战斗核心 | BACKLOG | T300 | 1.0 | 实现笔迹采样、最小距离、最大点数和长度裁剪。 |
| T320 | P3 手势战斗核心 | BACKLOG | T310 | 2.0 | 实现RDP简化、重采样、长度、包围盒、面积、闭合和曲率。 |
| T330 | P3 手势战斗核心 | BACKLOG | T320 | 2.0 | 配置驱动识别横、竖、斜、弧、圆和蓄力笔势。 |
| T340 | P3 手势战斗核心 | BACKLOG | T310 | 1.5 | 实现低分配笔迹视觉、淡出和池化。 |
| T350 | P3 手势战斗核心 | BACKLOG | T320, T340 | 2.0 | 实现分段胶囊命中、顺序排序、同笔去重和弱点命中。 |
| T360 | P3 手势战斗核心 | BACKLOG | T350, T230 | 1.5 | 实现伤害公式、方向奖励、连斩、评分和能量。 |
| T370 | P3 手势战斗核心 | BACKLOG | T350 | 1.5 | 实现可切断、不可切断和可反弹的敌方投射物。 |
| T400 | P4 玩家敌人技能 | BACKLOG | T360 | 1.5 | 实现玩家HP、能量、刀/符架势、切换冷却和战斗事件。 |
| T410 | P4 玩家敌人技能 | BACKLOG | T400, T230 | 3.0 | 实现数据驱动Skill到EffectGroup到有序Effect执行链。 |
| T420 | P4 玩家敌人技能 | BACKLOG | T360 | 2.0 | 实现通用敌人状态机、Damageable和Weakpoint。 |
| T430 | P4 玩家敌人技能 | BACKLOG | T420, T370 | 3.0 | 实现可组合移动、攻击、防御和支援策略注册表。 |
| T440 | P4 玩家敌人技能 | BACKLOG | T420 | 1.5 | 建立敌人、投射物、VFX和伤害数字对象池及完整重置。 |
| T450 | P4 玩家敌人技能 | BACKLOG | T430, T440 | 2.0 | 只用配置组合5种普通怪和1种精英怪。 |
| T460 | P4 玩家敌人技能 | BACKLOG | T410, T430 | 3.0 | 实现配置驱动Boss阶段、阈值、技能序列和切换。 |
| T500 | P5 关卡完整单局 | BACKLOG | T450 | 2.0 | 实现Level/Wave/Spawn时间轴和条件结束。 |
| T510 | P5 关卡完整单局 | BACKLOG | T500, T400 | 1.5 | 实现Countdown/Playing/UltimateDrawing/Paused/Victory/Defeat状态机。 |
| T520 | P5 关卡完整单局 | BACKLOG | T510 | 2.0 | 完成幽菌古道教学关：普通斩、连斩、切弹、架势和终极。 |
| T530 | P5 关卡完整单局 | BACKLOG | T520 | 2.0 | 完成混合怪物普通关，验证战术组合和难度曲线。 |
| T540 | P5 关卡完整单局 | BACKLOG | T460, T530 | 2.5 | 完成Boss关和镇墓玄甲王三阶段战斗。 |
| T550 | P5 关卡完整单局 | BACKLOG | T540 | 2.0 | 实现结算、星级/评分、解锁、重开和最小进度保存。 |
| T600 | P6 表现与资源 | BACKLOG | T510 | 2.0 | 实现生命、能量、连斩、评分、架势、终极、暂停和结算UI。 |
| T610 | P6 表现与资源 | BACKLOG | T600 | 1.0 | 建立中文TMP字体、fallback和字符覆盖检查。 |
| T620 | P6 表现与资源 | BACKLOG | T360, T440 | 2.0 | 实现受击停顿、闪白、震屏、伤害数字、音效、震动和慢动作。 |
| T630 | P6 表现与资源 | BACKLOG | T450, T600 | 2.0 | 接入PSD解析出的背景、主角、怪物、UI和特效作为原型资源。 |
| T640 | P6 表现与资源 | BACKLOG | T600, T120 | 1.5 | 适配横屏比例、刘海/圆角、安全区和触控遮挡。 |
| T650 | P6 表现与资源 | BACKLOG | T520, T600 | 1.5 | 完成事件驱动教程遮罩、手势示意和跳过/回看。 |
| T700 | P7 质量发布 | BACKLOG | T540 | 2.0 | 补齐纯规则EditMode回归矩阵。 |
| T710 | P7 质量发布 | BACKLOG | T550, T650 | 3.0 | 补齐Unity集成、完整单局、暂停、重开和生命周期PlayMode测试。 |
| T720 | P7 质量发布 | BACKLOG | T710, T250 | 1.0 | 审计所有玩法数值、内容和文案是否来自配置表。 |
| T730 | P7 质量发布 | BACKLOG | T710, T630 | 3.0 | 在目标低端机收敛CPU、GC、内存、DrawCall、纹理和包体。 |
| T740 | P7 质量发布 | BACKLOG | T730 | 2.0 | 自动化配置验证、Unity测试、Web构建和证据归档。 |
| T750 | P7 质量发布 | BACKLOG | T740, T120 | 2.0 | 生成微信小游戏发布候选并完成四级平台验收。 |
| T760 | P7 质量发布 | BACKLOG | T750 | 1.0 | 完成发布资料、版本冻结、回滚方案和最终证据索引。 |

---

## 8. 测试与验收

### 8.1 验证金字塔

- L0 文本/结构：文件、命名、asmdef、JSON结构、导出器。
- L1 纯逻辑：采样、RDP、几何、分类、公式、状态转换。
- L2 Unity集成：Collider、Prefab、事件、对象池、场景引用。
- L3 玩家路径：教程、战斗、终极、胜败、暂停、重开。
- L4 稳定性：连续重开、前后台、压力、内存、长局。
- L5 平台：Web、转换、DevTools、真机分别保存证据。

### 8.2 必须覆盖的核心边界

- 单点、过短、过长、急转弯、多点触摸、越过UI、安全区边缘。
- 横/竖/斜边界角、闭合圆、弧线、蓄力、错方向。
- 同一笔同一目标去重、多目标命中排序、一笔倍率。
- 弹幕切断、反弹、弱点窗口、护甲破除。
- 对象池连续复用和三次完整重开无状态泄漏。
- 配置ID重复、外键缺失、数值越界、Boss阈值乱序、坏JSON整包拒绝。
- 玩家未确认前流程绝不自动推进。
- 中文字体不缺字、不裁切。
- 微信前后台切换、音频恢复、触摸坐标和Safe Area。

### 8.3 每个任务证据

`artifacts/evals/TASK-ID/verification.md` 至少记录：

- 日期、分支、commit、Unity精确版本和实例。
- 做了什么、明确没做什么。
- 预计文件白名单与实际diff。
- EditMode/PlayMode测试计数。
- 真实玩家路径和可断言数值。
- Console新增Error/Warning。
- 截图、日志和构建产物路径。
- 已知问题。
- `PASS / REVIEW / BLOCKED / KNOWN ISSUE`。

---

## 9. PSD资源接入计划

当前PSD适合作为可编辑战斗概念稿和静态原型素材，不是完整动画资源包。

### 9.1 可直接用于灰盒/原型

- 红色幽冥洞穴背景。
- 黄衣灵猫主角静态图。
- 符火鱼妖、轮车僵妖、石甲龟妖、骷髅幽魂、飞行符蝠。
- 生命条、终极、切换、设置按钮。
- 斩击轨迹、烟尘、伤害数字和鱼妖拆分受击状态。

### 9.2 正式接入前要做

- 统一线条、明暗、材质和像素密度。
- 主角与怪物拆分头、身体、四肢、武器、尾巴、表情和受击部件。
- UI拆分框体、填充、图标、文字和多状态。
- 背景拆分远景、中景、地面、近景、发光物和氛围层。
- VFX改为4–8帧序列或程序化轨迹+Shader。
- 建立PPU、Pivot、图集、压缩、Max Size和命名规范。
- 检查所有外来/生成素材的商用授权和来源记录。

---

## 10. 主要风险与应对

| 风险 | 应对 |
|---|---|
| 微信SDK与Unity精确版本不兼容 | T110先做兼容矩阵；固定版本；最小补丁；保留UPSTREAM与移除条件 |
| 配置与Inspector形成双主库 | GameplayConfigService单一入口；SO只存资源引用；启动打印来源和哈希 |
| 手势看起来命中但实际没命中 | 显示与碰撞共用处理后的点；纯几何EditMode边界测试 |
| 手机触控采样差异 | 参考像素、长度裁剪、重采样、宽松阈值；DevTools和多真机校准 |
| Web平台线程/文件限制 | 构建期Excel处理；运行时协程/分帧；无任意路径和托管线程核心依赖 |
| 对象池旧状态泄漏 | Begin/Stop双端重置合同；连续生成、死亡、重开回归 |
| 中文缺字 | 固定字体源、动态SDF或字符集、Fallback、真机截图 |
| PSD资源无法直接动画 | 先静态原型，后按拆件清单生产，不让美术阻塞核心机制 |
| AI代理跨任务扩散 | 一次一个READY任务；明确不做项和白名单；一任务一提交 |
| 构建成功被误判为平台成功 | Web/转换/DevTools/真机四级证据独立记录 |

---

## 11. Definition of Done

项目只有在以下条件全部满足时，才能称为MVP候选版本：

- 玩法、范围、技术和配置文档无冲突。
- 49个任务按依赖完成或有经确认的范围决策。
- 配置Excel、导出器、JSON、DTO、校验和测试形成闭环。
- 无硬编码玩法数值；Inspector无第二数值库。
- 3关完整可玩，教程、胜败、暂停、重开和解锁正常。
- 专项与全量EditMode/PlayMode通过。
- 真实玩家路径、连续三次重开和对象池压力通过。
- Unity Editor、标准Web、微信转换、DevTools和至少一台真机均有独立证据。
- Console无新增阻断错误；性能达到设备预算。
- 中文字体、Safe Area、音频和前后台恢复通过。
- 构建版本、配置版本、SDK版本、commit和发布检查记录完整。
- 资源授权和隐私清单确认。

---

## 12. 代理执行硬规则

1. 任何时刻只做一个原子任务。
2. 先读取权威文档，再读取当前任务相关代码。
3. 开始前记录Git工作树并写改动白名单。
4. 不扫描或提交 `Library/Temp/Logs/UserSettings/Builds`。
5. 不手工编辑Unity YAML；场景/Prefab/导入设置用Editor或MCP。
6. 纯规则先写测试，MonoBehaviour只负责接线与生命周期。
7. 所有内容走配置，不在C#或Inspector复制一份。
8. 玩家确认使用事件门，不用计时器替代。
9. 测试通过不等于玩家路径通过；必须做真实Play Mode。
10. 转换完成不等于真机PASS。
11. 不能执行的人工步骤必须明确BLOCKED，禁止伪造。
12. 一个任务一个提交，更新证据后停止。
