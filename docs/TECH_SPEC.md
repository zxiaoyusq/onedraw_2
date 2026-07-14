# TECH_SPEC：Unity技术规格

## 1. 引擎基线

- 当前工程固定Unity `6000.5.1f1`，机器真相为 `ProjectSettings/ProjectVersion.txt`，索引真相同步写入 `project-index.yaml`。
- Unity工程直接位于仓库根目录，不使用额外的 `game/` 包装目录。
- 不得静默升级或降级；任何版本迁移必须先完成微信平台兼容性Spike并写入 `DECISIONS.md`。
- 当前工程由Unity 2D模板初始化；目标渲染基线为URP 2D Renderer，具体管线绑定在T020验证和固定。
- 输入Unity Input System；UI使用uGUI + TextMeshPro。
- 测试使用Unity Test Framework；Runtime、Editor、EditMode、PlayMode程序集分离。
- 横屏；参考分辨率1920×1080。手势阈值使用“参考像素”，不依赖 `Screen.dpi`。

## 2. Web与微信约束

- 运行时不解析Excel。
- 不使用 `Task.Run`、自建线程或依赖托管多线程的核心库；耗时工作在构建期处理，运行时用协程或分帧。
- 不依赖任意文件系统路径；保存、震动、生命周期和平台API通过 `IPlatformService`。
- 平台SDK是可替换适配层，Gameplay程序集不能直接引用微信SDK程序集。
- MVP无后端；后续网络只经可替换Web请求抽象。
- 不使用 `BinaryFormatter`；存档为版本化JSON。

## 3. 目标目录

```text
repo/
├─ AGENTS.md
├─ CLAUDE.md
├─ project-index.yaml
├─ Design/Config/GameConfig.xlsx
├─ Tools/ConfigExporter/
├─ docs/
├─ artifacts/evals/
├─ Assets/_Game/
│  ├─ Art/{Characters,Enemies,Backgrounds,UI,VFX,Audio}
│  ├─ Config/{Registry,Generated}
│  ├─ Prefabs/{Actors,Projectiles,UI,VFX}
│  ├─ Scenes/{Bootstrap,MainMenu,Battle}
│  ├─ Scripts/
│  │  ├─ Core
│  │  ├─ Config
│  │  ├─ Input
│  │  ├─ Combat
│  │  ├─ Actors
│  │  ├─ Skills
│  │  ├─ Levels
│  │  ├─ Presentation
│  │  └─ Platform
│  └─ Tests/{EditMode,PlayMode}
├─ Packages/
└─ ProjectSettings/
```

## 4. 程序集边界

```text
OneStrokeDemon.Core
OneStrokeDemon.Config        -> Core
OneStrokeDemon.Input         -> Core
OneStrokeDemon.Combat        -> Core, Config, Input
OneStrokeDemon.Actors        -> Core, Config, Combat
OneStrokeDemon.Skills        -> Core, Config, Combat, Actors
OneStrokeDemon.Levels        -> Core, Config, Actors, Skills
OneStrokeDemon.Presentation  -> Core, Config, Combat, Actors, Levels
OneStrokeDemon.Platform      -> Core
OneStrokeDemon.Bootstrap     -> all runtime modules
Tests.EditMode / Tests.PlayMode
```

禁止循环依赖。平台实现由Bootstrap注入接口。

## 5. 关键组件

### 配置

- `GameplayConfigDocument`：JSON DTO数组。
- `GameplayConfigService`：启动解析、完整校验和只读索引。
- `AssetRegistrySO`：只映射 `assetKey → UnityEngine.Object`，不保存HP、CD等数值。
- `ConfigIds.g.cs`：可选生成ID常量，避免魔法字符串。

### 手势

- `StrokeSampler`：采样、长度裁剪、点数上限。
- `StrokeGeometry`：RDP、重采样、长度、面积、闭合、曲率。
- `GestureClassifier`：配置驱动笔势匹配。
- `StrokeHitResolver`：分段NonAlloc命中、排序、同笔去重。
- `StrokeTrailView`：纯表现，不决定结果。

### 战斗与角色

- `DamageContext` / `DamageCalculator`：纯规则。
- `PlayerCombatModel`：HP、能量、架势。
- `EnemyController`：通用状态机；策略注册表组合移动、攻击和防御。
- `SkillService`：Skill → EffectGroup → 有序Effect执行器。
- `ObjectPoolService`：敌人、投射物、VFX和数字复用并完整重置。

### 流程

- 游戏：Boot → MainMenu → Loading → Battle → Result。
- 战斗：Countdown → Playing ↔ Paused → UltimateDrawing → Playing → Victory/Defeat。
- 必须由玩家完成的门使用事件，不允许超时自动确认。

## 6. 手势实现细节

- 输入在Safe Area内转换为1920×1080参考像素，算法测试不依赖设备分辨率。
- T300由Bootstrap从配置`Global.reference_width/reference_height`初始化`PointerInputRuntime`，不得在Input程序集或Inspector保存另一套参考分辨率；`Screen.safeArea`在事件转换时动态读取，不缓存设备绝对边距。
- `IPointerInput`统一Mouse与Touch的Began/Moved/Ended/Canceled事件；MVP只锁定第一个活动物理指针，其他指针在其结束或取消前不会接管。Safe Area外起笔无效，合法笔迹移出Safe Area后夹紧到参考边界以保留终止事件。
- uGUI命中只在起笔时阻断：UI上起笔不会形成笔迹，合法起笔后的移动不因跨过UI而截断。失焦、应用暂停、禁用、系统取消或活动设备断开必须产生至多一次带原因的Canceled事件。
- 采样先按最小距离过滤，再按最大长度裁剪，最后RDP与最大点数重采样。
- `GestureClassifier`输出类型、置信度、长度、速度、角度、曲率和闭合比。
- 命中用每段胶囊NonAlloc查询或等价低分配实现；不为轨迹生成大量Collider。
- 命中记录包含 `strokeId`、目标、弱点标记、路径参数、笔势和时间。
- 同一笔默认同目标只命中一次；重复命中必须由技能配置明确允许。
- 视觉和碰撞使用同一简化点集合。

## 7. 敌人与技能的数据驱动边界

- 新敌人若只组合现有移动、攻击、防御、弱点和数值，必须只改配置和资源，不写子类。
- 新行为策略才允许新增代码，并同步策略枚举、校验和测试。
- Skill只描述触发、目标、CD、能量、EffectGroup。
- Effect执行器使用显式注册表，不使用运行时反射。
- Boss阶段由条件、阈值、策略、倍率和进入技能组成，百分比不硬编码。

## 8. 性能内部目标

这些是项目目标，不是微信官方限制：

- 目标60fps；低端机短时可降到30fps，但不得持续卡顿。
- 单笔最多96个简化点，最多3条残留轨迹。
- 活跃敌人目标≤40，活跃投射物≤60，全部池化。
- 热路径避免LINQ、闭包、字符串拼接、每帧分配和材质实例化。
- 角色、敌人、UI、背景、VFX分别使用SpriteAtlas。
- 纹理、音频、字体和包体预算在平台Spike后记录，不凭旧资料硬编码。

## 9. C#规范

- 命名空间 `OneStrokeDemon.*`。
- 组合优于继承。
- 纯规则不得依赖MonoBehaviour、Time或全局Random。
- 随机通过可注入 `IRandomSource`，测试固定种子。
- Runtime配置只读；临时状态单独存储。
- 错误必须包含配置ID和上下文；不吞异常，不半份回退。
- `.unity/.prefab/.asset` 由Unity Editor或MCP保存，任务未授权时禁止手改YAML。

## 10. 中文TMP字体与fallback

- UI字体采用OFL来源的重命名子集；上游完整CJK字体只作本地构建输入，不进入仓库或交付，许可证、固定提交和源/子集hash必须随包保存。
- 字符清单由全部`Texts.zhCN`、可打印ASCII、动态数字所需符号和常用中文UI标点求并集；配置文案新增字符后，`LocalizationGlyphTests`必须先失败，再重建子集与TMP资产。
- 运行时不动态扩Atlas。默认Latin主字体和中文fallback均为Static、单Atlas，最大尺寸分别为512×512和1024×1024；HUD显式加载项目字体，不允许静默退回系统字体。
- TMP `.asset`、Settings与shader资源只能由Unity Editor作者工具生成/导入。固定uGUI包的Essential Resources导入后只保留移动SDF shader、Settings、样式及中文换行规则，删除未使用的默认大Atlas和源字体。
- PlayMode必须逐字符验证实际`TMP_CharacterInfo`没有替换字形，同时检查活动HUD/结算文本无overflow或truncate，并保存带中文HUD和动态伤害数字的1920×1080实际渲染截图。
