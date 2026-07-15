# TEST_PLAN：测试与证据

## 验证金字塔

| 层 | 范围 | 示例 |
|---|---|---|
| L0 结构 | 文件、asmdef、schema、静态扫描 | 字段漂移、硬编码数值 |
| L1 EditMode | 纯算法和公式 | RDP、闭合、方向、伤害、配置校验 |
| L2 PlayMode | Unity接线和生命周期 | Collider、输入、Prefab、对象池、场景状态 |
| L3 玩家路径 | 真实场景操作 | 教学、终极画符、Boss、重开 |
| L4 稳定性 | 压力、暂停、重复进入 | 10分钟战斗、重开3次、前后台 |
| L5 平台 | Web、转换、DevTools、真机 | 四级平台门 |

## 必须有的EditMode测试

- `StrokeSamplerBoundaryTests`
- `StrokeGeometryTests`
- `GestureClassifierTests`
- `StrokeHitResolverTests`
- `DamageFormulaTests`
- `SkillEffectPipelineTests`
- `EnemyStateMachineTests`
- `PoolResetTests`
- `EnemyArchetypeConfigTests`
- `BossPhaseTests`
- `SpawnTimelineTests`
- `BattleFlowTests`
- `TutorialFlowTests`
- `ConfigValidationTests`
- `SaveMigrationTests`
- `LocalizationGlyphTests`
- `FeedbackEventTests`

## 必须有的PlayMode测试

- `PointerCancelPlayModeTests`
- `MultiTargetHitPlayModeTests`
- `ProjectileReflectPlayModeTests`
- `StanceSwitchPlayModeTests`
- `WaveRunnerPlayModeTests`
- `NoAdvanceBeforePlayerActionTests`
- `TutorialLevelE2EPlayModeTests`
- `NormalLevelE2EPlayModeTests`
- `BossLevelE2EPlayModeTests`
- `RestartThreeTimesPlayModeTests`
- `EnemyGalleryPlayModeTests`
- `BossBattlePlayModeTests`
- `HudBindingPlayModeTests`
- `LocalizationGlyphPlayModeTests`
- `CombatFeedbackPlayModeTests`

T510专项必须覆盖：配置倒计时到Playing的delta切分；统一时间缩放精确到期；Countdown暂停保留进度；FocusLost/ApplicationPaused叠加后完整恢复；Ultimate输入窗包含边界、严格超时只取消、旧gestureEventId不能重放；大delta不能跨PlayerConfirmed；同帧死亡/到时/完成只产生一次互斥结算。PlayMode从Bootstrap真实配置路径验证生命周期与有效终极事件，不能只构造表外设置。

T520专项必须覆盖：正式配置映射6步/6波/15怪与180秒上限；错误触发、未来完成事件和计时器单独推进均不改变步骤；正确动作可在最短展示前锁存并于边界完成；`StrokeHitCount>=3`严格拒绝2并接受3；Active步骤阻止波次结算而Waiting步骤不阻塞。PlayMode必须从Bootstrap真实配置走完普通斩、弱点、同笔三目标、切弹、实际架势切换及配置Circle终极，断言6次开始、6次完成、1次教程完成、15次出生、能量实际扣除和最终Victory。

T530专项必须覆盖：正式配置映射`lv_002_cave`的8波、23条出生行、45个敌人、六种非Boss原型和第5/7/8波精英修饰请求；四个双波战术段人口递进，`maxAlive`足以承载配置组合，不同架势危险目标至少错开1秒。EditMode必须用内存配置变体证明出生时间、数量与容量只改表即可改变重载结果；PlayMode必须从Bootstrap真实配置经T500/T510流程实际出生并击败45怪，覆盖投射物、冲撞、近战和支援动作，断言210秒内Victory且敌人池活动租约为0。

T540专项必须覆盖：正式配置映射`lv_003_boss`的240秒上限、2波、6条出生行、11个混合前置敌人和1个Boss，以及三阶段提示、攻击、进入效果和最终处决意图。EditMode必须用内存配置变体证明关卡时限、波次、出生、阶段阈值和提示只改表即可改变重载结果；PlayMode必须从Bootstrap真实配置经T500/T510/T460/T410完成前置门、三阶段和处决Victory，并独立验证玩家死亡Defeat后阶段运行时停止、全新协调器重试可Victory、终态事件不跨局且活动池租约为0。

T550专项必须覆盖：最终分数按配置拆分T360战斗分、弹反、无伤与剩余整秒，Victory使用Levels阈值得星并按Rewards顺序执行解锁/非付费积分，Defeat不发胜利奖励；相同settlementId重复结算不得再次写盘、加币、解锁或增加通关次数，写盘异常不得发布候选快照。SaveMigrationTests必须覆盖缺失、确定性往返、畸形JSON、未来版本、显式v0→v1迁移及未知配置ID回退。PlayMode必须从Bootstrap真实配置连续Restart三次再用胜利结果进入配置后继关，断言旧会话、GameObject和活动池租约全部释放。

T600专项必须覆盖：Presenter只从配置和单一只读状态源生成生命、能量、连斩、实时评分、架势、终极、暂停与结算ViewModel；能量、冷却、流程终态和`CanGoNext`必须完整门控按钮，View点击不得绕过门直接改Model，Dispose后不得继续渲染或执行命令。状态绑定必须覆盖Player、Combo、Score、BattleFlow和ResultService事件以及单调终极冷却时钟。`HudBindingPlayModeTests`必须从Bootstrap真实配置创建实际Canvas，断言全部关键面板位于同一Safe Area根、自定义安全矩形锚点准确，并走通终极、暂停、主菜单、Victory 4480分/2星/奖励、Restart和NextLevel按钮路径。

T610专项必须覆盖：字符清单与全部`texts[].zhCN`、可打印ASCII、NBSP和常用中文UI标点精确一致；OFL子集的来源提交、重命名、SHA-256和体积固定；Latin主字体与中文fallback均为Static单Atlas且不超过512×512/1024×1024，TMP Settings及HUD资源路径指向同一fallback链。PlayMode必须从Bootstrap真实配置渲染中文HUD、结算和动态负伤害/暴击数字，逐字符拒绝replacement glyph并检查活动文本无overflow/truncate；图形设备路径保存1920×1080截图并目检，`-nographics`专项仍须独立通过。

T620专项必须覆盖：五类语义事件选择的`FeedbackCues`档案、配置变体无需改代码即可改变cue/强度、原始伤害与事件字段不被反馈层改写，以及震动总开关关闭后平台端口零请求但视觉命令保留。PlayMode必须从Bootstrap真实配置与Registry预缓存音频/VFX，验证白闪、时间缩放、相机震动、池化VFX和伤害数字的激活/完成/重开清理，并以图形设备保存五类反馈同屏截图进行人工感知验收。

T630专项必须覆盖：Runtime不存在PSD/PSB/JPG/JPEG，全部目标PNG可解码为RGBA且前景具有透明像素；Importer的Sprite Single、PPU、Pivot、Mesh、Clamp/Bilinear、Mip/ReadWrite、压缩和Max Size按资源类型一致；五个SpriteAtlas v2覆盖明确类别且不把字体等目录杂项打包；Sorting Layer顺序固定为Background、Default、Actors、Projectiles、VFX；Registry的18个Sprite与40个Prefab键均指向实际持久化资源，可渲染Prefab不保存玩法组件或数值。视觉证据使用确定性1920×1080最终RGBA资产画廊；离屏GPU捕获异常必须标为INVALID，不能替代Web/微信/真机路径。

T650专项必须覆盖：提示、高亮目标、手势与跳过/回看文案只来自`Tutorials/Texts`；计时、错误事件和回看都不能代替正确玩家动作；显式跳过不伪造`StepCompleted`，只持久化一次完成标记；v1存档迁移到v2后标记可确定性往返。PlayMode必须从Bootstrap真实配置创建HUD/遮罩，验证中文提示、手势、高亮、跳过和回看无overflow/truncate，并走通“首局跳过但仍击败15怪后Victory → 重开自动跳过但仍击败15怪后Victory”，存储只写一次。

T660专项必须覆盖：主菜单标题/开始/选择文案和Levels按钮只来自配置，锁定状态只来自ProgressSnapshot，非法或未解锁跨场景选择不得进入Battle。PlayMode必须从Bootstrap进入生产MainMenu，触发实际Button监听器选择教学关，并以InputSystem鼠标笔迹命中屏内真实敌人、扣减HP、刷新HUD且显示与处理后几何一致的可见轨迹；架势切换等技能音效必须沿`AudioCues.audioKey -> assetKey -> AssetRegistry`配置链解析。还要解锁并分别进入普通关与Boss关，验证教程/HUD存在、Defeat结算、Restart全新会话和MainMenu返回。专项须保存1920×1080图形设备菜单截图并目检；全量EditMode/PlayMode必须排除生产EventSystem、场景卸载和测试顺序回归。人工Unity窗口点击若缺可用UI控制链必须明确标为BLOCKED，不能用自动化截图冒充。

## 证据模板

每个任务写`artifacts/evals/TASK-ID/verification.md`。模板真相源为`templates/verification.md`和`templates/change-whitelist.md`，可用下列命令初始化且不会覆盖已有证据：

```bash
Tools/CI/new-task-evidence.sh TASK-ID
```

Unity测试必须用`Tools/CI/run-unity-tests.sh`分别执行EditMode和PlayMode，并各自保存NUnit XML。脚本会解析XML并把测试失败、零测试、缺失或损坏结果转换为非零退出码。完整命令、日志卫生、标准Web入口和提交步骤见`docs/WORKFLOW.md`。

verification至少覆盖：

```markdown
# TASK-ID Verification
- Git与Unity追溯、任务范围和明确不做
- 预计白名单、实际改动和用户已有改动保护
- EditMode/PlayMode总数、通过、失败和XML路径
- 玩家路径、可断言值、Console和平台分层结论
- 已知问题及PASS / REVIEW / BLOCKED / KNOWN ISSUE结论
```

证据包含可断言值，例如HP变化、命中数、阶段、配置hash和帧率，不只写“看起来正常”。
