# DECISIONS

## D-001 · Unity版本策略

- 状态：ACCEPTED
- 决定：采用现有工程已经固定的Unity `6000.5.1f1`，不得在未完成兼容性Spike和新决策前升级或降级。
- 理由：用户已用该版本初始化工程，`ProjectSettings/ProjectVersion.txt` 与本机Editor安装均可核验；T000不为追随原计划建议版本而迁移现有工程。
- 替代：若T110证明官方微信方案不兼容，比较更换补丁、切换已验证版本或最小embedded补丁；必须另写决策。

## D-002 · 配置唯一真相源

- 状态：ACCEPTED
- 决定：Excel为内容源，稳定JSON为构建快照，Runtime不读xlsx。
- 理由：可审查、可验证、适合Web和微信，避免Inspector双主库。

## D-003 · Unity对象引用

- 状态：ACCEPTED
- 决定：AssetRegistrySO只映射assetKey到Prefab、Sprite、Audio和VFX，不保存平衡数值。

## D-004 · 敌人架构

- 状态：ACCEPTED
- 决定：通用EnemyController、状态机和策略注册表，不为每个怪物建立空壳子类。

## D-005 · MVP平台能力

- 状态：ACCEPTED
- 决定：MVP只接存储、震动、生命周期和日志；广告、支付、登录、分享和排行榜不在范围内。

## D-006 · 横屏与参考坐标

- 状态：ACCEPTED
- 决定：横屏，1920×1080参考坐标；输入阈值按Safe Area缩放后的参考像素计算，不依赖Screen.dpi。

## D-007 · Unity工程目录

- 状态：ACCEPTED
- 决定：仓库根目录同时作为唯一Git根和Unity工程根，`Assets/`、`Packages/`、`ProjectSettings/` 不再放入 `game/` 子目录。
- 理由：当前目录已经是初始化完成的Unity 2D工程；避免移动资产产生额外GUID、路径和工具链风险。

## D-008 · T020 Unity包与渲染基线

- 状态：ACCEPTED
- 决定：Unity 6000.5.1f1使用URP 17.5.0、Input System 1.19.0、uGUI/TMP 2.5.0和Test Framework 1.7.0；Unity MCP固定commit `11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8`。
- 决定：Graphics默认管线与Low/High质量档统一引用 `Assets/Settings/UniversalRP.asset`，其默认Renderer为 `Renderer2D.asset`。
- 理由：消除Graphics空管线与Git浮动依赖，确保Editor、测试和后续构建使用同一可复现基线。
- 细节：完整直接依赖、质量和输入测试入口见 `docs/PACKAGE_BASELINE.md`。

## D-009 · T110 微信转换 SDK 固定与 Unity 6000.5 补丁

- 状态：ACCEPTED
- 决定：采用微信官方 `wechat-miniprogram/minigame-tuanjie-transform-sdk` 的 `v0.1.33` 发布线，固定 commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`，不使用浮动分支或已禁用的旧仓库。
- 决定：SDK由 Unity Package Manager embedded 到 `Packages/com.qq.weixin.minigame`。只允许 `WXRuntimeExtDef.cs` 在 `UNITY_6000_5_OR_NEWER` 使用 `GetEntityId` 的单点补丁；较早 Unity 保持上游实现。
- 理由：未修改上游在 Unity 6000.5.1f1 因 `GetInstanceID()` 的 CS0619 无法编译；替代 API 已通过 Unity 反射核验，补丁后全工程编译与回归通过。embedded 使补丁和完整上游快照可复现。
- 许可证：SDK根许可证为 MIT；随包保留 Brotli MIT-style 与 Binaryen 103.0.0 Apache-2.0 许可证。
- 移除条件：官方不可变版本修复该调用，并在 Unity 6000.5.1f1 通过 T110 同等编译与测试矩阵后，删除 embedded 包并恢复纯 Git 依赖。
- 限制：该决定不确认 G2转换、G3 DevTools、G4真机，也不授权迁移 Unity。

## D-010 · T120 可重复微信转换入口与Brotli策略

- 状态：ACCEPTED
- 决定：G2统一通过项目自有 `WechatBuildEntry` 与 `Tools/CI/build-wechat.sh` 调用固定SDK的 `WXConvertCore.DoExport`；输出限定在忽略目录 `Builds/WeChat/**`，Spike配置使用空AppID、横屏、256MB、关闭渲染线程和性能分析。
- 决定：macOS Unity `6000.5.1f1` 使用SDK公开的 `brotliMT=true` 路径。默认单线程路径因错误定位 `Unity.app/PlaybackEngines` 无法运行；这不是新增SDK源码差异。
- 决定：构建包装器在运行前备份、退出后恢复ProjectSettings、embedded SDK配置/元数据、URP构建期字段和SDK临时Assets，避免平台Spike污染可审查基线。
- 理由：第一次完整转换证明默认Brotli路径阻断G2；启用随SDK提供的压缩实现后，Builder、Converter、JSON、产物清单和 `.br` 均通过，并且仓库无平台设置残留。
- 限制：G2为 `PASS WITH KNOWN ISSUES`，不替代G3/G4；93条未匹配替换规则保持BUG-0006，只有实际DevTools和真机可以缩小风险。
- 移除条件：官方固定版本修复macOS路径并在同一Unity版本通过完整G2～G4后，可恢复默认压缩路径并删除兼容策略。

## D-011 · 平台阻塞期间优先推进主内容链

- 状态：ACCEPTED
- 决定：按用户明确指示，T120保持`BLOCKED`并保留现有G2证据，T130保持`BACKLOG`；暂不处理微信开发者工具、真机和打包问题，依赖T040且可独立执行的T200成为唯一`READY`任务。
- 理由：G3/G4需要本机之外的登录工具与设备条件，而P2配置系统到大部分玩法主链不依赖这些运行门；继续主内容可以产生有效进展，同时不伪造平台结论。
- 限制：这是执行顺序延期，不是范围裁剪。`MVP_SCOPE`中的微信四级验证仍必须完成，T120不得改为DONE，G3/G4不得改为PASS。
- 恢复条件：具备已登录微信开发者工具和至少一台可用手机后可恢复T120；无论设备何时到位，平台任务最迟在T640或T750开始前恢复并满足其依赖。

## D-012 · T200配置契约冻结

- 状态：ACCEPTED
- 决定：`Design/Config/GameConfig.xlsx` 为唯一内容源，`config/一笔镇妖_游戏配置表模板.xlsx` 只做字节一致的同步镜像；当前冻结 schema `1` / content `0.1.1-sample`。
- 决定：稳定ID匹配 `^[a-z][a-z0-9_]*$`；玩法权威ID采用 `lv_001_tutorial`、`lv_002_cave`、`lv_003_boss` 与 `boss_tomb_king`。文案键、奖励/教程分组键和资源键是独立命名空间，不跟随玩法ID自动改名。
- 决定：Schema `required` 表示JSON属性存在，FieldDictionary `required` 表示Excel单元格非空；可空字符串导出为空字符串，可空数值/布尔导出为null。Global四个值列组成由valueType判别且恰好一项非空的联合。
- 决定：普通外键使用 `Sheet.field`；分组外键校验目标组存在；`SpawnPoints.levelId="*"` 是唯一通配符；`Rewards.rewardId=conditional` 按UnlockLevel/UnlockFeature/ScoreToken分别校验关卡ID、`feature_`和`token_`命名空间。
- 决定：contentHash对排除自身后的完整配置对象计算SHA-256，采用递归Ordinal对象键序、固定数组排序、UTF-8无BOM和紧凑JSON；生成时间不进入内容。
- 理由：T210导出器和T220校验器需要无歧义的输入、空值、外键、排序及hash合同；先修正与GAME_DESIGN冲突的ID和字段字典必填错误，避免把初始样例缺陷固化到Runtime。
- 限制：T200的项目内审计脚本只用于冻结证据，不是T210导出器或T220生产校验器；本决定不授权Runtime读取xlsx或Inspector保存数值。

## D-013 · T210独立配置导出器与确定性写入

- 状态：ACCEPTED
- 决定：配置导出采用独立 `net8.0` 控制台工具 `Tools/ConfigExporter`；xlsx读取固定为 `DocumentFormat.OpenXml 3.5.1`，直接依赖使用精确版本约束并提交NuGet锁文件。Open XML及测试依赖仅存在于Tools，不进入Unity Runtime。
- 决定：输出顺序和`contentHash`严格执行D-012；所有表显式稳定排序，FieldDictionary按固定Sheet和对应表头序，重复排序键以整行字段作Ordinal/数值兜底，禁止依赖Excel行号、当前区域设置或字典枚举顺序。
- 决定：`export`先在目标同目录写入`.tmp`并强制落盘，重新读取验证顶层顺序、版本、记录数和hash后才原子替换旧文件；`validate`执行同一读取、建模、序列化与内存自检但不写输出。
- 理由：相同工作簿在不同机器和重复运行中必须得到字节一致快照，且导出器异常或自检失败不能破坏最后一份有效JSON。
- 许可证：运行时工具依赖均为MIT；测试依赖为MIT或Apache-2.0，完整版本和上游记录见 `Tools/ConfigExporter/THIRD_PARTY_NOTICES.md`。
- 限制：T210只证明可导出性、契约对齐与确定性，不执行T220的必填、范围、枚举、唯一性、外键或跨表生产校验，不创建T230/T250负责的Unity Runtime资产。

## D-014 · T220生产校验与整包拒绝

- 状态：ACCEPTED
- 决定：`ConfigValidator`在完整工作簿建模后、序列化和原子写入前执行；任一错误以稳定`CFG`错误码和Sheet/Excel行/字段定位并拒绝整包，不半应用、不静默修正。
- 决定：生产合同覆盖必填、类型/范围、稳定ID、主键/组合键、枚举、普通/分组/通配符/conditional外键、连续order、Global联合、Level→Wave→Spawn、星级和Boss全覆盖语义。提供Schema时，FieldDictionary/Enums的类型、可空性、min/max和枚举集合还必须与Schema精确一致。
- 决定：`MovePatternType`与`AttackTriggerType`属于代码拥有的算法合同，当前由导出器登记精确集合；T430实现Runtime策略注册表时必须复用或同步该合同，不能把策略选择变成第二套玩法数值库。
- 测试策略：坏配置样例采用可审查JSON变更清单，只修改正式工作簿读取后的内存副本；37类反例逐一断言错误码、Sheet、Excel数据行和字段，正式xlsx及其镜像、Schema、样例JSON保持只读。
- 限制：T220不生成Unity Runtime快照、DTO、加载服务或AssetRegistry；这些仍分别属于T230、T240和T250。

## D-015 · T230 Runtime一次加载与只读发布

- 状态：ACCEPTED
- 决定：Bootstrap把工具生成的 `gameplay_config.json` 作为 `TextAsset` 交给 `GameplayConfigService`；每个服务实例只解析一次，在局部候选快照上完成严格JSON、schema/content、根与Global版本一致性、contentHash、空键/重复键和组合键检查，全部通过后才发布静态Runtime引用。
- 决定：Runtime兼容线固定为schema `1`和content `0.1.x`。所有业务读取通过 `IConfigProvider` 的显式只读主键/分组索引完成；不公开可变表数组，不在热路径反序列化，不用反射驱动战斗。
- 决定：Unity Runtime直接固定 `com.unity.nuget.newtonsoft-json 3.2.2`（上游 Newtonsoft.Json `13.0.2`）。可空数值/布尔及严格缺失/null语义不能由 `JsonUtility` 无损表达，运行时依赖不得偶然来自开发期Unity MCP的传递依赖。
- 理由：坏包必须在进入MainMenu/Battle前整包失败，正常启动只付出一次解析成本，并为后续玩法提供稳定O(1)配置读取。
- 许可证：Unity包封装使用Unity Companion License；包内列出的第三方Json组件为MIT，版本和边界见 `docs/PACKAGE_BASELINE.md`。
- 限制：T230提交初始JSON快照但不生成hash旁车或ConfigIds，也不实现AssetRegistry；T240/T250分别负责这些后续边界。

## D-016 · T240 AssetRegistry对象引用、占位与构建门

- 状态：ACCEPTED
- 决定：Canonical `AssetRegistrySO`只序列化`assetKey`和`UnityEngine.Object`，Runtime一次校验后发布Ordinal只读索引；场景通过独立`AssetSceneReference`保存明确场景引用。Registry、Inspector和C#均不得复制HP、CD、伤害或其他平衡值。
- 决定：AssetManifest定义76个稳定ID及预期类型；运行时和Editor绑定不使用其中的`addressOrPath`，不以路径或GUID查找资源。替换Sprite、AudioClip或Prefab只修改Registry对象引用，不修改配置ID；场景路径仅存在于明确的Unity场景引用包装中。
- 决定：缺少正式资源的当前阶段按类型复用三个受管占位资产（Sprite、AudioClip、Prefab），`scene_battle`直接引用Build Settings中的Battle场景。作者工具重跑时保留每个键已有的合法类型引用，因此后续可逐项替换而不被占位生成覆盖。
- 决定：Editor菜单和`IPreprocessBuildWithReport`在构建前核对空键、空对象、重复、缺失、额外、类型、持久化Prefab及启用场景；任何失败转换为构建失败。Bootstrap仅在Runtime配置与Registry均通过后进入MainMenu。
- 理由：配置ID需要与Unity资源位置和替换解耦，同时在资源尚未齐备时建立可编译、可测试、可逐项替换的完整绑定合同。
- 限制：共享占位只证明引用覆盖与类型合同，不代表正式表现完成；正式资源接入和视觉验收仍属于各玩法任务及T630，T250只负责配置生成流水线。

## D-017 · T250 同源三生成物与只读漂移门

- 状态：ACCEPTED
- 决定：T250的`generate`必须复用T210/T220的`PreparedExport`，一次生产校验后从同一`ConfigDocument`/`SerializedConfig`生成`gameplay_config.json`、64位contentHash+LF旁车和`ConfigIds.g.cs`；不得新增第二套xlsx解析、排序、hash或数值模型。
- 决定：`ConfigIds.g.cs`放在`Assets/_Game/Scripts/Config/Generated/`，确保实际编入`OneStrokeDemon.Config.dll`；当前按27个定义/分组ID集合生成306个Ordinal常量，并嵌入schema/content/hash。它只消除魔法字符串，不替代JSON内容索引，也不保存平衡值或Unity对象引用。
- 决定：默认`verify`和`Tools/CI/verify-config.sh`只读重建预期字节，任何JSON/hash/C#缺失或单字节漂移以`CFG013`非零失败；只有显式`--update`允许重生成受管文件。脚本在漂移时给出unified diff，不静默修正。
- 决定：一键完整门包含ConfigExporter全套测试及`ConfigPipeline`分类的Unity EditMode/PlayMode；`--skip-unity`只输出PARTIAL。Unity Editor已打开的交互会话可由MCP执行同一分类，但证据必须同时保留脚本的.NET/漂移层和MCP job计数，不能把局部脚本输出伪报为完整一键PASS。
- 理由：代码、Runtime配置与提交快照必须对同一工作簿形成可审查闭环；把生成C#放入正确asmdef并使漂移默认失败，才能在进入玩法开发前阻止“只改Excel”“手改JSON”或遗漏旁车/常量。
- 限制：T250不实现通用全项目CI、Web/微信构建或玩法逻辑；这些分别属于T740、T750和T300以后任务。

## D-018 · T300 单活动指针、Safe Area参考空间与UI起笔门

- 状态：ACCEPTED
- 决定：`IPointerInput`只发布一个统一的Mouse/Touch事件流；事件包含屏幕/参考坐标、来源、pointerId、时间、阶段与取消原因。MVP锁定首个物理TouchControl或Mouse，其他触点在当前指针终止前全部忽略，不做多指手势。
- 决定：参考宽高只由Bootstrap读取已验证配置`Global.reference_width/reference_height`并注入Input Runtime；Safe Area来自每次转换时的`Screen.safeArea`。Safe Area外不能起笔，合法起笔移出后夹紧到参考边界，绝不读取`Screen.dpi`。
- 决定：UI门使用当前EventSystem的uGUI Raycast且只在Began前检查；UI上起笔永不转换为战斗笔迹，从非UI起笔后经过UI仍连续。失焦、应用暂停、适配器禁用、系统取消或活动设备断开都发布一次明确Canceled，后续重复生命周期通知幂等。
- 理由：后续采样、识别、命中和视觉必须消费同一个设备无关、分辨率无关且可回放的输入真相；在入口固定单指所有权、坐标空间和取消语义，可避免UI误攻击、刘海偏移与后台残留笔迹。
- 限制：T300不实现采样距离、点数/长度裁剪、识别、轨迹或命中；这些从T310开始。真机Safe Area形状、触摸延迟和平台暂停仍由T120/T640/T710独立验证。

## D-019 · T360架势公式外键、命中结算次序与评分伤害维度

- 状态：ACCEPTED
- 决定：配置契约升级为schema `2` / content `0.2.x`。`Stances.damageFormulaId`是到`DamageFormulas.formulaId`的必填普通外键，取代任何调用方或C#维护的架势到公式映射；`DamageFormulas.scorePerDamage`显式配置每点原始伤害进入评分的系数。该版本决定只替代D-012/D-015中的当前版本冻结值，其他唯一真相源、空值、外键、hash和一次发布合同继续有效。
- 决定：`DamageCalculator`是无MonoBehaviour依赖的纯规则。伤害依次组合架势、方向、弱点、连斩和暴击倍率；方向失败同时组合公式与防御表的失败倍率。评分组合配置命中奖励、弱点加值、方向/连斩奖励与`原始伤害 × scorePerDamage`；能量组合命中值、弱点加值与方向/连斩奖励。三种结果分别在公式末尾使用`MidpointRounding.AwayFromZero`取整。
- 决定：`ComboService`只接收外部单调时间戳，按T350稳定命中顺序逐目标计数；间隔等于`Global.combo_timeout_sec`仍延续。`ScoreService`累计伤害、评分、已赚取能量和命中维度，但不拥有玩家当前能量、上限或消耗。
- 理由：架势若由调用方传formulaId，T400必然形成第二映射；若评分只使用`scorePerHit`，暴击造成的伤害不会进入GAME_DESIGN规定的“伤害”评分维度。把两项都纳入配置契约可使公式独立断言且不硬编码平衡系数。
- 限制：T360不扣敌人HP、不控制弱点窗口、不拥有玩家状态，也不实现投射物弹反、无伤或剩余时间评分；这些分别属于T420/T400/T370/T550及其后续任务。

## D-020 · T370投射物交互优先级、归属追溯与确定性回收

- 状态：ACCEPTED
- 决定：T370复用现有`Projectiles.cuttable`、`reflectable`和`requiredStanceId`，不升级配置版本。处理顺序固定为架势门→反弹→切断→不可切断；当两个开关都为true时反弹优先，同一笔迹不会同时回收。
- 决定：每枚投射物同时保存当前归属与不可变原始归属。反弹只把当前归属切换为玩家并反转显式单位方向，原敌方实体、表内`projectileId/damage`和反弹次数进入`ProjectileDamageSource`；阵营过滤使用当前归属，因此反弹弹体可伤原敌方而不会伤玩家。
- 决定：`ProjectileController`在参考像素空间按表内速度和寿命做确定性Transform位移，不使用不可控物理力。所有终止路径先发布快照，再清空规则、归属、位置、方向、时间、命中ID、Collider和Transform并停用对象；同对象下一次生成必须以新规则和新来源覆盖全部运行态。
- 理由：只保存一个“owner”会在反弹后丢失原攻击来源，只看`cuttable`会让同时可切/可反弹的现有内容产生歧义；固定优先级和双来源快照可使伤害归因、目标阵营及回收复用独立断言。
- 限制：T370不实现T400玩家HP/架势状态、T420敌人HP/状态机、T430攻击策略或T440通用对象池；这些系统只消费本任务公开的归属、伤害和回收结果。

## D-021 · T400玩家状态、目标架势冷却与战斗事件边界

- 状态：ACCEPTED
- 决定：T400不升级配置版本，直接复用`Players`、`Stances`和`Skills`现有字段。`PlayerCombatModel`拥有HP、当前能量和唯一`StanceService`；初始HP取`Players.maxHp`、能量为空槽，受击无敌、能量上限、默认架势、终极技能及其能量消耗均沿配置读取，不在Inspector或C#复制平衡值。
- 决定：成功切换采用“切入目标行”的`Stances.switchCooldownSec`，并立刻发布该目标行`onSwitchEffectGroupId`作为效果意图；等于冷却边界允许切换，同架势和冷却拒绝不发布事件。T410负责解释和执行EffectGroup，T400不提前执行技能效果。
- 决定：当前架势快照统一提供给轨迹、伤害和投射物入口：轨迹读取`strokeWidthRefPx`，伤害沿`damageFormulaId`及倍率解析，投射物继续执行T370 `requiredStanceId`门并同时暴露`projectileCutMultiplier`。调用方不得维护刀/符分支表。
- 决定：控制器按状态变化发布稳定序号事件；致死固定先`HpChanged`后`Died`，模型只在HP首次归零时标记死亡，后续同帧事件不重复。死亡后拒绝能量获取/消耗和架势切换；有效技能扣能还必须先满足`Skills.requiredStanceId`。
- 理由：把当前架势或能量留在UI/场景脚本会形成多个状态真相；把即时效果直接写进切换控制器又会提前侵入T410。纯模型、配置快照和效果意图可让同一状态被战斗/表现消费，并独立验证一次性死亡与原子扣能。
- 限制：T400不实现技能CD/效果执行、治疗/复活策略、敌人状态、战斗流程或HUD；这些分别属于T410、T420、T510和T600。场景与Prefab接线也留给对应集成任务。

## D-022 · T410显式效果注册、目标选择与终极有效笔势门

- 状态：ACCEPTED
- 决定：配置契约升级为schema `3` / content `0.3.x`。`EffectType`新增`Heal`与`ClearProjectiles`，终极`fx_ultimate_seal`在减速后显式清弹；Enums、JSON Schema、导出器代码注册集合和Unity `EffectExecutorRegistry`必须精确对齐，未知类型不能反射发现或静默跳过。
- 决定：`SkillService`统一执行Skill触发类型、有效笔势/输入窗、冷却、T400架势/能量门，再按`SkillEffects.order`调用显式`IEffectExecutor`。`TargetType`由单一选择器解释主目标、世界作用域、半径/上笔/手势区域及全敌/普通敌/Boss；目标顺序沿调用方稳定列表。条件表达式只支持显式比较语法，缺变量为不成立，非法语法在扣能前失败。
- 决定：效果通过`ISkillEffectWorld`和`ISkillEffectTarget`适配当前/后续战斗对象；治疗封顶且不复活，清弹、减速、重复笔迹和下笔倍率走世界端口。新增由12类现有效果组成的技能只改表，不创建每技能MonoBehaviour；T420/T440接入敌人和投射物时实现端口，不反向复制技能数值。
- 决定：`Gesture`与`Ultimate`必须收到调用方的有效笔势事件且不超过配置输入窗；无效、超时、冷却、架势或能量拒绝都不部分扣能。当前终极执行顺序为减速→清弹→全敌伤害→普通敌低血处决→Boss易伤，等于2.5秒输入窗边界有效。
- 理由：仅在C#添加治疗/清弹会使配置枚举与运行时能力分叉；按技能写组件会让内容组合变成代码发布。冻结注册表、选择器和门控次序后，策划可用现有效果组合新技能，同时保持错误可定位、顺序可回放和终极不能被计时器误判成功。
- 限制：T410不实现T420敌人状态机/具体Damageable、T430攻击策略、T440实际对象池、T510完整`UltimateDrawing`战斗流程、T600 HUD或T620表现；PlayMode只验证真实GameObject玩家、有效笔势事件和世界/目标适配端口。

## D-023 · T420敌人攻击时序、护甲优先与显式打断恢复

- 状态：ACCEPTED
- 决定：T420不升级配置版本，直接复用`Enemies`、`EnemyAttacks`、`DefenseRules`、`WeakpointRules`和`Buffs`。状态机只接收配置快照与外部单调时间，`cooldownSec`定义完整攻击周期，内部依次为Windup、Attack和剩余Recovery；弱点窗与攻击打断窗均相对攻击开始且包含边界。
- 决定：T360弱点结果不能单独强制打断；当前敌人还必须处于Windup/Attack，并满足该攻击行的笔势与打断时间窗。弱点/攻击配置没有眩晕时长，所以此类打断保持无限期Stun直到调用方显式恢复；只有配置Stun Buff按配置持续时间自动恢复。
- 决定：来伤先消耗配置护甲，溢出进入HP；护甲从正数首次归零只发布一次配置`breakEffectGroupId`意图。死亡、重复打断和回收分别只产生一次状态/事件，回收必须清空全部运行态后才允许同一对象复用。
- 理由：把`cooldownSec`另当额外Recovery会把表内攻击频率拉长；只看弱点标记会绕过攻击自身的笔势教学；为缺失的眩晕时长写常量会形成第二数值库。冻结组合条件与显式恢复可以保持配置真相并把后续策略选择留给T430。
- 限制：T420只发布攻击/击退/破甲等运行时意图，不实现T430的移动、攻击、防御、支援策略与Telegraph，不实现T440通用对象池、T450内容装配或T460 Boss阶段覆盖。

## D-024 · T430显式策略注册、状态边界执行与配置护盾

- 状态：ACCEPTED
- 决定：配置合同升级为schema `4` / content `0.4.x`，新增`DamageReduction` Buff类型，并用`buff_shield_50 -> fx_puppet_shield`配置链表达摄魂道傀支援护盾。减伤只由Buff的`magnitude/durationSec/maxStacks/stackMode`决定，运行时不按敌人ID或技能ID写特例。
- 决定：移动、攻击触发分别使用显式注册表，完整覆盖当前`MovePatternType`与`AttackTriggerType`枚举，未知值失败。移动只消费配置路径、速度、振幅和频率及调用方移动时钟；攻击只消费调用方已判定的触发事实，并按配置order/weight稳定选取，从效果和投射物合同生成近战、投射物、冲撞或支援动作。
- 决定：攻击预警在T420 `BeginAttack`成功时打开，动作在纯状态机`Windup -> Attack`边界恰好执行一次；恢复、眩晕、死亡或回收都关闭并清空预警。动画事件只负责表现同步，不能成为伤害、弹体、冲撞或护盾实际执行的唯一来源。
- 决定：防御服务仅映射笔势、架势、命中/失败倍率、反伤与破甲意图，继续由T360/T420负责实际伤害与生命状态；策略运行时不复制伤害公式或配置数值。
- 理由：按敌人ID分支会使T450内容组合再次依赖代码发布；在策略层猜测距离/HP阈值或依赖动画事件会引入不可回放的隐式规则。显式注册、外部事实和状态边界执行可让相同策略跨敌人复用，并让未知合同在加载或测试阶段失败。
- 限制：T430不创建通用对象池、不装配六种敌人内容、不实现Boss阶段或完整关卡流程；这些分别属于T440、T450、T460与T500以后任务。

## D-025 · T440共享family容量、显式租约与确定性耗尽

- 状态：ACCEPTED
- 决定：配置保持schema `4`并升级content `0.5.x`。敌人/VFX沿各自行读取预热，投射物按新增Global统一预热，伤害数字按配置池大小预热；四类family容量和耗尽策略全部读取Global，策略值只允许`Reject`或`ReuseOldest`。
- 决定：Core `ObjectPoolService`只以显式租约管理活动对象。租约包含pool/family、重开generation和单调激活序号；同family下所有ID池共享活动容量。`Reject`不改变池状态，`ReuseOldest`先完整回收family内最旧活动对象再获取目标池对象，重开回收全部活动租约并推进generation。
- 决定：`activeSelf=false`不等同于容量已释放。对象因玩法结束可先清空自身状态，但持有者仍须用精确租约调用服务释放；旧租约、重复释放和未知对象不影响当前租约。泄漏报告以尚未归还的活动租约为真相。
- 决定：敌人、投射物、VFX和伤害数字都实现`IPoolable`完整重置；敌人额外清除外部战斗事件订阅和事件序号，四类对象均恢复池父节点/标准Transform并停用。VFX保留每池不可变配置但清空播放态，伤害数字不在T440引入表外生命期常量。
- 理由：只看GameObject开关无法区分自停用、旧租约和已归还容量；每ID独立上限又会绕开全局活动预算。共享family容量、显式generation租约和确定性最旧复用可以让清场/重开可断言，并让配置直接控制内存预热和峰值行为。
- 限制：T440只提供池合同、配置映射与可复用池项，不负责T450敌人内容装配、T500波次所有权、T620反馈编排、场景/Prefab接线或平台构建。

## D-026 · T450只读敌人目录、资源类型路由与策略租约同生命周期

- 状态：ACCEPTED
- 决定：T450不升级配置版本。`IConfigProvider.GetEnemies()`公开只读敌人快照，`EnemyArchetypeCatalog`从表内自动选取非Boss行，聚合已有移动/攻击/防御/弱点/文案/资源合同并按ID稳定排序。不在产品C#中列六怪ID，不为每怪派生`EnemyController`子类。
- 决定：每个非Boss攻击必须有正前摇，打断窗必须跨过执行边界。目录用不含ID/数值的策略特征摘要检测重复教学点；当前六怪分别以可切可弹火符、横斩打断冲车、蓄力破甲、符术/弧线克幽魂、斜斩俯冲和精英护盾支援形成独立语义。
- 决定：资源创建只根据`AssetManifest.assetType`路由Sprite或Prefab，并从T240 Registry取Unity引用。`EnemyArchetypeActor`与T440池租约同生命周期：获取后才Spawn和创建T430策略运行时，任何池回收先Dispose策略再完整重置敌人。
- 理由：若关卡或Prefab为六怪分别拼接组件，HP/速度/攻击改动仍会需要代码或Inspector同步；若策略订阅不跟随池租约，重开或最旧复用会保留旧攻击世界。只读目录与单一装配路径让内容变更直接来自表与Registry。
- 限制：T450仍使用T240类型占位资源，不声称正式动画、视觉或身体碰撞形状已完成；不实现T460 Boss阶段、T500关卡时间轴、T510战斗流程或T630正式资源。

## D-027 · T460以纯阶段状态机重建Boss战斗档案并顺序执行进入动作

- 状态：ACCEPTED
- 决定：配置保持schema `4`并升级content到`0.5.1-sample`。镇墓玄甲王的HP区间、移动模板、攻击集、防御、弱点、进入效果和双语文案全部由`BossPhases`及外键表提供；二、三阶段使用独立Boss移动模板，让速度变化也只改表。
- 决定：`BossPhaseStateMachine`只消费当前HP比例，按连续阈值顺序推进。等于边界立即换阶段；HP回升和重复观察不重放；单次非致死大伤害跨越多段时仍依次发出所有阶段进入事件，致死则直接进入死亡语义。目录加载同时拒绝非Boss归属、断号、空洞、重叠、空进入效果或空文案。
- 决定：`BossPhaseController`在阶段切换时取消旧攻击/眩晕，保留Boss当前HP与上限，按新防御重置护甲并替换弱点，释放旧策略订阅后从新移动/攻击配置重建T430运行时，再经T410效果链执行进入动作并发布一次阶段事件。
- 理由：把阈值或三套战斗数值写进Boss组件会形成第二配置库；把动画结束当换阶段真相会因表现资源长度变化产生时序漂移。纯规则顺序推进与状态边界重建可独立测试，也能保证非致死大伤害不吞掉进入动作。
- 限制：T460只提供Boss三阶段运行时，不实现T500关卡时间轴、T510战斗流程、T540完整Boss关卡胜败回路、正式演出资源或平台构建。

## D-028 · T500用显式世界回执提交出生并隔离玩家动作门

- 状态：ACCEPTED
- 决定：T500不升级配置版本。`LevelCatalog`沿现有Levels/Waves/Spawns/SpawnPoints/EnemyModifiers查询构造只读定义并执行运行时所有权、连续order、枚举、范围和Boss结束条件校验；产品代码不维护关卡、波次、敌人或出生点ID列表。
- 决定：出生时间线按表内时刻和稳定ID顺序展开，只有`ILevelSpawnWorld`成功接收并给出唯一实体ID后才提交。`maxAlive`或世界拒绝形成可重试背压，不丢请求；归一化出生点及完整修饰器随请求交给世界适配器，Level层不依赖具体敌人池，也不把修饰数值复制到Inspector。
- 决定：玩家确认只消费当前正等待的`PlayerConfirmed`开始/结束门，暂停和提前事件不锁存。非动作门可用大delta追赶，动作门不能被计时器跨越；暂停时关卡时钟、波次、出生和确认全部冻结。`TimeElapsed`结束把唯一可用的`endDelaySec`解释为本波持续时间，其余结束条件把该字段解释为条件成立后的延迟。
- 理由：先移动时间线游标再请求对象池会在容量耗尽时永久吞怪；把确认保存为全局布尔值会让早到事件自动跨过未来教程门；在Level层直接依赖敌人Prefab/池又会把关卡规则和实例化生命周期绑定。显式回执、当前门消费和世界端口让同一纯规则可在EditMode回放，也能由后续T510/T520接入实际战斗世界。
- 限制：T500公开`durationLimitSec`到时事实但不判Victory/Defeat，不实现Countdown/UltimateDrawing/Paused流程、具体关卡教学、完整Boss关、HUD、清场策略或正式资源；这些仍属于T510及后续任务。

## D-029 · T510统一时间源、叠加暂停与一次性胜负裁决

- 状态：ACCEPTED
- 决定：T510不升级配置版本。流程设置只沿`Global.battle_countdown_sec`、`Global.pause_on_focus_lost`和`Players.ultimateSkillId -> Skills`映射；统一时间源区分未暂停流程、未缩放战斗和受配置Effect缩放的战斗时间。Countdown不推进关卡，Playing/UltimateDrawing使用同一战斗delta，Paused与终态完全冻结；暂停恢复保留倒计时进度。
- 决定：终极按钮只把Playing切到UltimateDrawing。成功同时要求非零且本局单调递增的`gestureEventId`和T410对配置终极产生的`SkillActivationResult.Activated`，旧笔迹事件不能跨绘制重放；等于配置输入窗边界有效，严格超过只取消。无效、超时、暂停或主动取消都不能由计时器升级为成功，也不能绕过T410能量/架势/死亡原子门。
- 决定：FocusLost、ApplicationPaused和玩家暂停使用独立位原因。首次暂停统一请求取消当前笔迹，重叠原因全部解除后才恢复；若从UltimateDrawing暂停，本次绘制取消且恢复目标固定为Playing，避免后台旧输入继续生效。
- 决定：`BattleFlowCoordinator`把T500 LevelCompleted/durationLimit事实与玩家死亡在同一裁决点消费；同帧死亡或到时优先于关卡完成，得到Defeat。终态后拒绝所有后续裁决，Victory/Defeat互斥且Settled事件只发布一次。
- 理由：若各系统各读Unity Time，慢动作、暂停和关卡时限会漂移；若用单布尔暂停，失焦与系统暂停回调顺序会提前恢复；若胜负按回调先后直接结算，同帧事件会产生双结算或平台相关结果。统一切片、暂停原因集合和单点事实裁决使流程可回放并保持玩家动作门。
- 限制：T510只提供纯流程/时间/事件合同和T500协调器，不制作T520教学编排、T540完整Boss关、T550结果/存档、T600 HUD、清场演出或场景/Prefab接线。
