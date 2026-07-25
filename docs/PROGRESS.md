# PROGRESS

- 日期：2026-07-25
- 当前成熟度：P6表现资源扩充完成；进入P7质量发布
- 当前任务：T700
- 状态：READY
- Unity精确版本：6000.5.1f1（已由ProjectVersion.txt与本机安装核验）
- 微信SDK来源或版本：官方 `minigame-tuanjie-transform-sdk` v0.1.33 / commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` / embedded最小补丁
- Active Scene：Assets/_Game/Scenes/Bootstrap.unity
- 配置版本：schema 5 / content 0.6.5-sample / hash `9cc48fcb5f3b45cff68dd0bfc09cf533d808b26cc956553bc5b060cfa5113abb`

## 进行中

- 当前没有`IN_PROGRESS`任务；T695已完成，依赖均为DONE的首个后续任务T700保持`READY`。

## 已完成

- T695：将用户提供的1024×768爆炸图集按源JSON自然顺序切成十一帧256×256 Sprite，以12 FPS生成非循环`EnemyDeath` Clip、单状态Controller和池化`vfx_enemy_death` Prefab；生产死亡链只在关卡接受既有死亡事实后发布0伤害表现事件，先快照目标位置再回收敌人。`VfxPoolItem`复用时重绑Animator并采样首帧，T630通用作者工具跳过专用动画Prefab且把图集纳入VFX Atlas。工作簿新增VfxCue、FeedbackCue、AssetManifest并升级content `0.6.5-sample`；双工作簿SHA-256均为`64c78fda...fbc8c3`，受管hash为`9cc48fcb...13abb`、748条记录、380个ID常量，Registry为77键。素材预检1/1、T695 EditMode 1/1、PlayMode 2/2、AssetImport 5/5、ConfigPipeline EditMode 19/19与PlayMode 3/3、ConfigExporter 58/58、全量EditMode 206/206和PlayMode 55/55通过；1920×1080 Metal逐帧Gallery确认白烟爆炸由饱满烟团过渡到烟圈消散。一次外部同步EditorSettings后的PlayMode初始化在Unity Test Framework `PlayModeRunTask`内空引用，未执行产品测试并作为无效证据丢弃；恢复Bootstrap与测试状态后相同命令及全量回归通过。用户已有`Design/Config/~$GameConfig.xlsx`删除状态未纳入提交。
- T694：保持`char_moyan_idle`稳定键和全部玩法参数不变，将用户提供的3×3九帧待机与4×3十二帧攻击图集按源JSON自然顺序切片；以12 FPS生成循环待机、非循环攻击、共享AnimatorController和`PlayerMoyan` Prefab。生产Battle入口现在按AssetManifest实例化Sprite或Prefab，只有普通有效笔势触发纯表现`Attack`，命中与伤害仍由原战斗链裁决。工作簿AssetManifest由Sprite改为Prefab并升级content `0.6.4-sample`，双工作簿SHA-256均为`a5fc5a21...a286`，受管hash为`e348bab0...52c`。素材预检2/2、T694 EditMode 1/1、PlayMode 1/1、Metal图形专项1/1、全量EditMode 205/205、PlayMode 53/53、ConfigExporter 58/58及完整配置门全部通过；真实相机1920×1080截图确认新主角在战斗场景正确受光并显示攻击帧。用户已有`Design/Config/~$GameConfig.xlsx`删除状态未纳入提交。
- T693：根因是Unity Player Settings使用Auto Rotation但同时允许Portrait、Portrait Upside Down、Landscape Left和Landscape Right，Android因此可直接竖屏启动。现保留Auto Rotation与左右横屏，仅关闭两个竖屏方向；新增方向合同EditMode测试1/1通过，Android Build Profile确认未覆盖全局Player Settings。Unity 6000.5.1f1验证APK构建成功（46,258,919字节，SHA-256 `94d33cdb...52ec0`），包内最终AndroidManifest的UnityPlayerGameActivity声明`screenOrientation=11`，本机Android 36 SDK将值11定义为`userLandscape`。首次IL2CPP链接无诊断返回1，使用Bee同一clang响应文件原样重跑7秒成功，复用缓存后构建0 error/2 warning通过；用户已有Standalone batching、QualitySettings、UnityConnectSettings及未跟踪`Assets/Resources.meta`改动均未纳入T693提交。
- T692：定位到火鱼Prefab继续使用正确的`Actors` Sorting Layer和URP 2D `Sprite-Lit-Default`材质，但Bootstrap、MainMenu、Battle的Global Light 2D只覆盖`Default`，因此Lit角色动画正常却显示全黑。现已通过Unity Editor API让三个场景的全局光覆盖Background、Default、Actors、Projectiles、VFX全部5层，并保留可重复执行的修复工具。T692 EditMode 4/4、PlayMode 1/1、全量EditMode 203/203、干净域重载后的全量PlayMode 52/52通过；相机直接渲染的火鱼目视图确认恢复原始彩色。连续专项与全量PlayMode首轮曾触发Unity Input System编辑器状态串扰，9条既有测试统一因`Map index out of range in ProcessControlStateChange`失败，干净域重载单独重跑后52/52通过。最终Unity重编译无产品Error，Console仅有本机VS/Unity UDP端口占用warning；用户已有ProjectSettings改动与未跟踪`Assets/Resources.meta`继续排除。
- T691：保留全局`*.meta`兜底，仅以`!/[Aa]ssets/**/*.meta`放开Unity资产数据库必需的meta；Assets外meta继续忽略。审计只发现`Android.asset.meta`与空本地`Resources.meta`两项历史遗漏：前者对应已跟踪Build Profile并纳入提交，后者没有受管目录内容，保留为未跟踪本地文件且未删除/未提交。用户已有ProjectSettings改动继续排除。
- T690：保持`enemy_fire_fish`稳定键和全部玩法参数不变，将用户提供的3×3鱼妖图集切为9个256×256 Sprite，以12 FPS生成循环`FireFishIdle`、AnimatorController和`EnemyFireFish` Prefab；Registry改绑Prefab，旧静态`fire_fish.png`由Unity AssetDatabase删除，敌人图集同步重建。权威工作簿及镜像经表格工具同步，受管JSON/hash/ConfigIds及样例JSON经导出器重生成，当前hash为`0cf75f9d...75d81`；ConfigIds跨平台统一LF。T690 EditMode 1/1、PlayMode 1/1、全量EditMode 199/199、PlayMode 51/51、ConfigExporter 58/58和最终Unity重编译均通过；未修改或暂存用户已有ProjectSettings改动。
- T681：对`Assets/_Game/Scripts`全量147个C#、当前35,744行执行最终审计；147/147文件包含中文说明，478/478个类型和1,314/1,314个方法通过相邻中文职责注释检查。审计发现并补齐`T660SceneAuthoring.EnsureProductionRoot<T>`一个泛型方法遗漏，仅新增1行注释。完整配置门中ConfigExporter 58/58、ConfigPipeline EditMode 19/19与PlayMode 3/3通过，最终全量EditMode 198/198、PlayMode 50/50通过；生成物漂移0、日志无新增产品Error/Warning，用户`AGENTS.md`未修改/未暂存。P6代码可读性T670–T681全部完成。
- T680：修改ConfigExporter的ConfigIds模板，使生成物确定性包含中文文件职责、5项元数据、28个来源分组和377个稳定ID说明；`ConfigIds.g.cs`由导出器新增412行注释，未手改。生成器新增23行中文注释，E2E测试新增中文输出断言与8行注释；受管JSON/hash字节及schema/content/hash均不变。ConfigExporter 58/58、完整ConfigPipeline EditMode 19/19与PlayMode 3/3、全量EditMode 198/198及PlayMode 50/50通过，用户`AGENTS.md`未修改/未暂存。
- T679：为Editor目录及其子目录9个手写C#脚本、1,556行基线代码补充158行中文类型、方法和主要逻辑注释，覆盖美术/Registry/字体/场景资产生成、标准Web与微信构建入口；删除0行且无语义变化。配置漂移门与ConfigExporter 58/58、Editor相关专项EditMode 29/29和PlayMode 8/8、全量EditMode 198/198及PlayMode 50/50通过；TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T678：为Bootstrap目录6个手写C#脚本、2,193行基线代码补充239行中文类型、方法和主要逻辑注释，覆盖生产入口、主菜单/战斗组合根、跨场景启动上下文、战斗世界与会话释放；删除0行且无语义变化。配置漂移门与ConfigExporter 58/58、ConfigPipeline/T660专项EditMode 21/21和PlayMode 7/7、全量EditMode 198/198及PlayMode 50/50通过；TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T677：为Presentation目录17个手写C#脚本、4,989行基线代码补充526行中文类型、方法和主要逻辑注释，覆盖轨迹池、HUD绑定/视图、反馈运行时、伤害数字/VFX池与教程遮罩；删除0行且无语义变化。配置漂移门与ConfigExporter 58/58、表现专项EditMode 16/16和PlayMode 9/9、全量EditMode 198/198及PlayMode 50/50通过；TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T676：为Levels目录14个手写C#脚本、5,288行基线代码补充579行中文类型、方法和主要逻辑注释，覆盖出生/波次、战斗状态、教程、Boss关、结果评分、存档迁移与重开导航；删除0行且无语义变化。配置漂移门与ConfigExporter 58/58、T500–T550专项EditMode 40/40和PlayMode 9/9、全量EditMode 198/198及PlayMode 50/50通过。T540首次运行在测试完成后的Burst退出阶段发生Bus error，作为无效证据保留；同命令重试3/3通过。TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T675：为Skills目录9个手写C#脚本、1,966行基线代码补充217行中文类型、方法和主要逻辑注释，覆盖技能条件、目标选择、效果注册/执行、玩家与敌人效果目标及Boss阶段控制；删除0行且无语义变化。配置漂移门与ConfigExporter 58/58、T410专项EditMode 4/4和PlayMode 1/1、全量EditMode 198/198及PlayMode 50/50通过；TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T674：为Actors目录19个手写C#脚本、7,305行基线代码补充747行中文类型、方法和主要逻辑注释，覆盖玩家战斗/架势、敌人状态机、移动/攻击策略、弱点/Buff、原型运行时与Boss阶段；删除0行且无运行语义变化。配置漂移门与ConfigExporter 58/58、专项EditMode 27/27和PlayMode 6/6、全量EditMode 198/198及PlayMode 50/50通过；TMP测试漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T673：为Combat目录25个手写C#脚本、2,520行基线代码补充中文类型、方法、属性职责和主要逻辑注释，重点说明连击单调时间、伤害方向/弱点/暴击、配置规则映射、命中排序去重、弹体所有权/反射/切割及对象池生命周期。脚本仅新增201行注释、删除0行。配置漂移门与ConfigExporter 58/58、专项EditMode 26/26和PlayMode 10/10、全量EditMode 198/198及PlayMode 50/50通过；测试产生的TMP序列化漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T672：为Input目录19个手写C#脚本、2,397行基线代码补充中文类型、方法、属性职责和主要逻辑注释，重点说明单活动指针所有权、Safe Area与UI起笔门、设备/焦点取消、实时预览、固定缓冲采样、精确长度裁剪、RDP/弧长重采样及笔势优先级。脚本仅新增285行注释、删除0行。配置漂移门与ConfigExporter 58/58、Input四分类专项EditMode 41/41和PlayMode 10/10、全量EditMode 198/198及PlayMode 50/50通过；首次逗号组合分类得到0测试，被明确标为无效调用且未计入结论。测试产生的TMP序列化漂移已恢复，用户`AGENTS.md`未修改/未暂存。
- T671：为Config目录22个手写Runtime C#脚本、2,113行基线代码补充中文类型、方法、属性职责和主要逻辑注释；重点说明严格JSON解析、规范化哈希、版本兼容、不可变索引、一次性原子发布、AssetManifest双向覆盖与对象池配置转换。脚本差异仅401行注释新增、删除0行，未修改`Generated/ConfigIds.g.cs`或运行语义。配置生成漂移门与ConfigExporter 58/58、ConfigPipeline EditMode 19/19和PlayMode 3/3、全量EditMode 198/198及PlayMode 50/50通过。Unity测试引发的TMP材质序列化漂移已恢复到Git基线；日志仅重报未修改异常类声明已有的两条CS0114警告，用户`AGENTS.md`改动全程未修改/未暂存。
- T670：为Core/Platform共6个手写C#脚本、897行基线代码补充中文类型、方法、属性和主要逻辑注释；重点说明对象池共享容量、世代租约、最旧复用、异常回滚和Web冒烟探针。脚本差异仅251行注释新增、删除0行，未改运行语义。配置只读漂移门、ConfigExporter 58/58、T440专项EditMode 5/5、全量EditMode 198/198和PlayMode 50/50通过。Unity测试引发的TMP材质序列化漂移已恢复到Git基线，用户`AGENTS.md`改动全程未修改/未暂存。
- T660：2026-07-17用户确认中央白板与轨迹实时显示修复后的视觉复测通过，状态由REVIEW转为DONE。

- T660：新增配置驱动生产主菜单、关卡锁定/选择和跨场景启动意图；Battle生产会话组合现有Player、Enemy、Wave、PointerInput、Damage/Combo/Score/Skill、HUD、Tutorial/Boss、Feedback、Result与Navigation。主菜单和Battle由Unity Editor保存到独立单位变换场景根，避免继承灰盒`8×4`缩放；会话保留父子所有权，修复场景卸载时反馈池对象先销毁后释放的异常。
- T660：HUD新增架势切换按钮意图并沿既有`StanceService/SkillService`执行；Editor/Web进度适配通过`IProgressSaveStore`使用T550版本化JSON与PlayerPrefs，不进入Gameplay规则或直接调用微信SDK。Restart创建新会话，主菜单返回正式入口，普通关/Boss选择继续服从配置进度解锁。
- T660：工作簿新增`text_game_title/text_ui_start_game/text_ui_select_level`并升级为schema 5/content `0.6.3-sample`；双工作簿95,553字节且SHA-256同为`9f2330c9...233ee`。受管JSON 194,319字节/745条/content hash `2c005061...1dfdc`，28组377个ID常量；30个Sheet重渲染目检、严格漂移门和ConfigExporter .NET 58/58通过。
- T660：新增“开始游戏/选择关卡”5个汉字后，固定上游Noto Sans SC子集重建为126,168字节、SHA-256 `9de334f2...8c0e4`；当前字符清单299码点，静态Latin 96 + 中文fallback 203仍保持512×512/1024×1024单Atlas，图形日志无缺字警告。
- T660：2026-07-15首轮评审将T340既有`StrokeTrailPool`装配进生产会话，并把技能音效修正为`AudioCues.audioKey -> assetKey -> AssetRegistry`，消除`ARREG009 sfx_switch`。视觉复评进一步让`StrokeInputCollector`在开始和每个有效采样点发布预览事件，生产轨迹在按住拖动时连续增长；LineRenderer本体移到单位变换根，参考像素点与配置宽度经独立参考空间转换。中央白板另经现场禁用对照锁定为场景`BattleGraybox`，Unity Editor保存的场景diff只把该对象`m_IsActive`从1改为0。红测分别以“松开前无轨迹”和“开发灰盒仍active”失败，最终StrokeSampling 10/10、StrokeTrail 5/5、T660 PlayMode 4/4、全量EditMode 198/198、PlayMode 50/50，配置只读漂移门及.NET 58/58通过。主工程刷新编译后产品Error/Warning为0；Console仍可见无产品堆栈的`Temp/FSTimeGet-*` Editor Assert，作为Unity内部已知噪声单独记录。未运行标准Web、微信转换、DevTools或真机。

- T650：新增事件驱动`TutorialDirector`、遮罩View/运行时工厂与高亮目标注册表；已有T520步骤事件是唯一推进真相，View只渲染配置提示、目标框、程序化横/竖/斜/弧/圆/蓄力示意并转发跳过/回看意图。回看不改变序列，显式跳过不伪造`StepCompleted`，且必须等全部出生完成、活跃敌人归零后才放行最终战斗门。
- T650：进度存档升级到v2，以经配置目录验证的`completedTutorialIds`保存一次性标记；写成功后才发布当前快照，内建v1→v2迁移补空列表。首次跳过后重开会自动跳过教程展示，但两局都需击败15怪才Victory，存储端口只写入一次。
- T650：双工作簿与交付副本字节一致，SHA-256均为`12781247...0ffa2`；新增“继续战斗/回放”两条`Texts`并升级为schema 5/content `0.6.2-sample`。受管JSON为193,859字节/742条/content hash `7e2a0880...b91e`，28组374个ID常量；30个Sheet前后重渲染、公式错误0，严格配置漂移门与ConfigExporter .NET 58/58通过。新文案只使用T610既有字符，未修改字体二进制或TMP资产。
- T650：专项EditMode 3/3、PlayMode 1/1，ConfigPipeline EditMode 19/19，最终全量EditMode 195/195、PlayMode 46/46。Metal 1920×1080截图显示中文提示、青色手势/目标框与跳过按钮，SHA-256为`86587e44...599f`；活动教程文案无overflow/truncate。测试启动时License Client IPC首次握手报错但随后取得授权并完成全部测试；产品编译、运行时和测试结果无新Error/Warning，图形专项仅重报T610已知的4处CS0618测试API warning。
- T650：未修改Scene、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；未提前实现T640/T700及后续任务，标准Web、微信转换、DevTools和真机按用户延期决定未运行。

- T630：用户于2026-07-14提供仓库外的2868×1320、130,855,476字节概念PSD，SHA-256为`e6a2552a...1fb34`；原稿未进入`Assets/`。按图层清单导出两张红色洞穴背景、黄衣灵猫、五种PSD敌人、UI与VFX，并按用户许可用ImageGen单独补齐魂偶和镇墓玄甲王；所有输入与输出登记为`APPROVED_PROTOTYPE`，无一声称具备发布授权，轮车僵妖上游生成条款仍待发布前核验。
- T630：Runtime新增28张RGBA PNG（26个非背景含透明像素）、5个SpriteAtlas v2、2个单帧Actor Prefab和38个可池化VFX Prefab；Sorting Layer顺序固定为Background/Default/Actors/Projectiles/VFX。Canonical Registry的18个Sprite和40个Prefab键均改绑实际资源，17个AudioClip继续保留T240静音占位，未修改玩法配置、Scene、Packages、微信SDK或Builds。
- T630：专项EditMode 5/5、配置漂移0与.NET 58/58、ConfigPipeline Unity EditMode 19/19和PlayMode 3/3、最终全量EditMode 192/192及PlayMode 45/45全部通过，最终日志无Error/Warning。确定性1920×1080最终RGBA画廊SHA-256为`2e7cb088...948e3`；一次`-nographics`崩溃和Metal批处理纹理/颜色损坏均明确标为INVALID且不作证据，标准Web/微信/DevTools/真机按用户延期要求为NOT RUN。

- T620：新增配置驱动的5类战斗反馈档案，以及只读`CombatFeedbackService`和Unity输出适配；命中、弱点、破甲、弹反与玩家受击可编排受击停顿/慢动作、闪白、震屏、池化VFX/伤害数字、预载音效及可关闭震动，反馈不修改伤害或战斗结算真相。
- T620：双工作簿已同步为schema 5/content `0.6.1-sample`，新增`FeedbackCues`并完成关键Sheet渲染、重新导入及公式错误0复核；受管JSON为193,551字节/740条/content hash `152b9faa...86f8`，28组372个ID常量。严格生成/漂移门及ConfigExporter .NET 58/58通过。
- T620：反馈Runtime预载全部配置AudioClip/VFX Prefab，按T440池合同复用VFX与伤害数字；参考像素字号、上浮和VFX尺寸按Global参考高度与实际字体/渲染Bounds换算，反馈对象继承宿主Layer。完成、Restart和Dispose均恢复目标颜色、相机基位、池租约、TMP与VFX状态。
- T620：专项EditMode 4/4、PlayMode 1/1，ConfigPipeline Unity EditMode 19/19、PlayMode 3/3，最终全量EditMode 187/187、PlayMode 45/45。Metal 1920×1080画廊清楚展示普通命中白字、弱点黄字、破甲橙字、弹反无伤害数字和玩家受击红字，截图SHA-256为`8c687c19...97d`；测试日志无新增编译错误、运行异常或失败，重编译只报告T610既有且本任务未改动的4处CS0618测试API弃用warning。
- T620：未修改Scene、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；T240黑色占位VFX/音频的正式品质留给T630，微信震动适配仍留给T130，未提前实现T640/T650。

- T610（当前字库随T660入口文案同步重建）：采用Google Fonts官方仓库固定提交`2894aab3...d384`的OFL Noto Sans SC作为一次性构建输入，实例化weight 500并按保留字体名规则重命名为`One Stroke Demon UI`。未提交17,772,300字节上游原字体，当前交付126,168字节子集、OFL全文和来源/hash记录；子集SHA-256为`9de334f2...8c0e4`。
- T610（当前基线）：字符清单确定性覆盖全部`texts[].zhCN`、可打印ASCII、NBSP和常用中文UI标点，共299个唯一码点；TMP拆为96字符512×512 Latin静态主Atlas和203字符1024×1024中文静态fallback，均关闭多Atlas。全局TMP Settings及BattleHUD显式资源路径指向主字体，主字体与全局fallback均指向中文资产；TMP Essential Resources经Unity Editor导入后删去未使用的LiberationSans大Atlas/源字体和非移动shader，仅保留约20KB移动SDF、设置、样式与中文换行规则。
- T610：调整Noto行高下LevelName、结算标题/分数/星级容器高度。专项EditMode 3/3、PlayMode 1/1，最终全量EditMode 183/183、PlayMode 44/44；1920×1080 Metal渲染截图显示中文生命/能量/连斩/评分/架势/终极和动态`-12345 暴击`，字符信息逐字断言无replacement glyph，活动HUD与结算文案均无overflow/truncate。配置只读漂移门与.NET 56/56通过，最终专项/全量日志无编译错误、缺字警告、异常或测试失败。
- T610：未修改工作簿、schema、FieldDictionary、DTO、受管JSON/hash/ConfigIds、场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；未提前实现T620反馈、T630正式美术、T640完整适配或T650教程遮罩，也未恢复T120/T130平台工作。
- T600：新增只读`BattleHudState/BattleHudViewModel`、`BattleHudPresenter`与`BattleHudStateBinding`。Presenter单向订阅玩家、连斩、评分、流程和结算状态，View只渲染模型并转发暂停、终极、重开、下一关和主菜单意图；按钮按Playing/Paused/终态、能量、冷却和后继关可用性门控，不直接修改战斗Model。
- T600：新增运行时`BattleHUD` Canvas/uGUI/TMP工厂，参考分辨率只读Global的1920×1080，全部关键面板挂在动态Safe Area根下；生命/能量、连斩、实时评分、架势、终极状态、暂停层及Victory/Defeat结算、星级和奖励均可由同一Presenter更新。当前Battle场景仍只有灰盒环境且没有完整关卡组合根，T600提供可组合运行时入口并在Bootstrap真实配置PlayMode中装配验证，不手工改Scene YAML或提前实现T640完整多设备布局。
- T600：工作簿新增20条中英文HUD通用文案并升级content `0.6.0-sample`，双工作簿与交付副本SHA-256均为`4429caa0...7f13`；受管JSON为185,197字节/715条/content hash `54885fb2...a1d`，运行时331个主索引、56个组索引，27组367个ID常量。29个Sheet全部重渲染并视觉复核、公式错误0，严格导出/漂移门与.NET 56/56通过；Runtime兼容线同步为schema 4/content 0.6.x。
- T600：专项EditMode 6/6、`HudBindingPlayModeTests` 1/1，最终全量EditMode 180/180、PlayMode 43/43。玩家路径断言HP/能量100/100、连斩4、评分521、Demon Blade、终极/暂停按钮转发、自定义Safe Area锚点、Victory 4480分/2星/100积分奖励及Restart/NextLevel；最终日志无编译错误、异常或新增Warning。未修改Scene、Prefab、Registry、Input Actions、Packages、ProjectSettings或微信SDK，也未实现T610字体、T620反馈、T630正式美术、T640完整适配或T650教程遮罩。
- T550：新增无`MonoBehaviour`依赖的`ResultScoring/ResultService`。最终分数以T360战斗分为基底，并只从Global读取每次弹反150分、胜利无伤1000分和每个剩余整秒20分；星级读取当前Levels三阈值。胜利奖励按Rewards的Clear/ScoreAtLeast/StarAtLeast与UnlockLevel/UnlockFeature/ScoreToken协议顺序执行，Defeat不发胜利奖励。
- T550：新增`ProgressSave` v1、确定性JSON编解码、`IProgressSaveMigration`与注入式`IProgressSaveStore`。初始解锁从完整Levels nextLevel图自动求根；缺失存档正常初始化，畸形/未知目录ID回退，未来版本或缺迁移链安全拒绝。结算ID全局持久化去重，重复回调不重复写盘、加币、解锁或增加通关次数；存储写入成功后才原子发布新快照。
- T550：新增`BattleResultNavigation`会话所有权边界，Restart释放旧会话后以同关新建，NextLevel只接受当前胜利且奖励已解锁的配置后继关。Bootstrap真实配置PlayMode连续重开3次再进入`lv_002_cave`，共5个会话全部释放，旧GameObject销毁、活动池租约为0，generation为5。
- T550：工作簿升级为content `0.5.5-sample`，双工作簿SHA-256均为`fb4033d5...80f2`；受管JSON为182,404字节/695条/content hash `aa391c48...5bb1`，运行时311个主索引、56个组索引，27组347个ID常量。29个Sheet全部重渲染并视觉复核、公式错误0，严格导出/漂移门与.NET 56/56通过；ConfigPipeline EditMode 19/19、PlayMode 3/3，T550专项EditMode 12/12、PlayMode 1/1，最终全量EditMode 174/174、PlayMode 42/42。未改Schema、FieldDictionary、DTO、导出规则、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK，也未实现PlayerPrefs/微信存储适配、云存档、付费货币或T600 UI。
- T540：正式工作簿把`lv_003_boss`编排为240秒、2波、6条出生行和12个敌人实例；前置混合波包含5种普通原型共11怪及1次精英修饰，随后只生成镇墓玄甲王。星级阈值8000/12000/17000，三阶段攻击、进入效果与提示均沿既有配置外键取得，第三阶段提示明确斜斩打断冲撞后处决。
- T540：新增无关卡/Boss ID分支的`BossLevelCoordinator`与`IBossLevelWorld`，组合T500时间轴、T510胜负流程、T460阶段控制器和T410效果链；只在配置Boss实际出生后绑定阶段运行时，拒绝存活Boss的错误击败通知，并在Victory、Defeat或Dispose时释放阶段策略和全部订阅。失败后以全新协调器和世界重试，避免跨局状态泄漏。
- T540：双工作簿保持字节一致，SHA-256均为`71fc222d...f36c5`；受管JSON为181,598字节/692条/content hash `9fbd5fa9...17a0`，27组344个ID常量与样例同源。29个Sheet均由工作簿工具重渲染并视觉复核，公式错误0；严格导出、三生成物漂移门、ConfigExporter构建0 warning/0 error与.NET 56/56通过。
- T540：专项EditMode 3/3、PlayMode 2/2，最终全量EditMode 162/162、PlayMode 41/41。Unity 6000.5.1f1隔离批处理路径验证前置混合波、三阶段攻击/进入效果/提示、处决Victory、玩家死亡Defeat、全新实例重试Victory和池活动泄漏0，静态编译及最终日志无编译错误、崩溃或异常。未修改Schema、FieldDictionary、DTO、导出规则、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK；未提前实现T550及后续任务，也未恢复T120/T130平台工作。
- T530：正式工作簿把`lv_002_cave`扩为8波、23条出生行和45个敌人实例，完整覆盖5种普通怪与1种精英怪；第5/7/8波配置精英修饰器，四个双波战术段依次引入基础混合、耐久前排、精英支援和终局全谱组合。关卡上限210秒，三星阈值为6500/9500/13000，所有节奏、容量、敌人、修饰器和评分仍只来自表。
- T530：`maxAlive`按5→8递进并覆盖配置峰值；所有要求不同架势的近战/投射物危险目标至少错开1秒，避免0.75秒架势切换冷却造成不可解重叠。运行时玩家路径实际出生并击败45怪，执行投射物2类、冲撞1类、近战2类和支援1类动作，三次精英修饰请求均到达世界端口，最终Victory且敌人池活动泄漏为0。
- T530：双工作簿保持字节一致，SHA-256均为`c97eda68...01bdc`；受管JSON为180,658字节/689条/content hash `50ff7874...11fe`，27组341个ID常量与样例同源。29个Sheet均由工作簿工具重渲染并视觉复核，公式错误0；严格导出、三生成物漂移门、ConfigExporter构建0 warning/0 error与.NET 56/56通过。
- T530：专项EditMode 4/4、PlayMode 1/1，最终全量EditMode 159/159、PlayMode 39/39；节奏/容量改表后重新加载即可改变运行结果。最终Unity 6000.5.1f1刷新编译与Console Error为0，测试临时EditorSettings差异已恢复。未修改Schema、FieldDictionary、DTO、导出规则、产品运行时代码、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK；未提前实现T540及后续任务，也未恢复T120/T130平台工作。
- T520：正式工作簿把`lv_001_tutorial`扩为6波、6条出生行和15个敌人，6个连续Tutorial步骤覆盖普通斩、弱点、同笔三目标、切弹、符架势和Circle终极；关卡上限180秒。全部触发、完成条件、阈值、最短展示、文案、手势和阻塞开关来自表，content升级为`0.5.2-sample`，未改schema/FieldDictionary/DTO。
- T520：新增无`MonoBehaviour`依赖的`TutorialDefinitionFactory/TutorialSequence`与`TutorialLevelCoordinator`。步骤只接受当前显式事件协议，`StrokeHitCount>=3`按包含边界解析；计时器不能完成动作，正确动作早到会锁存到配置最短展示边界。`blockProgress`只在Active步骤门控波次结算，Waiting不阻塞且不冻结敌人/出生/输入；最终PlayerConfirmed只由整个教程在T510/T410有效终极结果后确认。
- T520：双工作簿保持字节一致，SHA-256均为`e4d6d382...ff02`；受管JSON为174,474字节/668条/content hash `f666feb2...e92`，27组320个ID常量与样例同源。29个Sheet均由工作簿工具重渲染并视觉复核，README/Global版本一致，公式错误0；严格导出、三生成物漂移门、ConfigExporter构建0 warning/0 error与.NET 56/56通过。
- T520：专项EditMode 5/5、PlayMode 1/1，受影响T500-T520回归EditMode 21/21、PlayMode 5/5，ConfigPipeline EditMode 19/19、PlayMode 3/3，最终全量EditMode 155/155、PlayMode 38/38。Bootstrap真实配置路径以六次玩家动作完成6步、出生15怪、实际切换符架势、配置终极击败末波4目标并Victory；任意长计时、错误/未来事件和命中数2均不能越门。最终Unity 6000.5.1f1刷新编译与Console Error/Warning为0，测试临时EditorSettings差异已用Unity API恢复。
- T520：未修改Schema、FieldDictionary、配置DTO/导出规则、场景、Prefab、Registry、Input Actions、Packages、ProjectSettings或微信SDK；未提前实现T530/T540/T550/T600/T650正式教程UI，也未恢复T120/T130平台工作。
- T510：新增无`MonoBehaviour`依赖的`BattleFlowStateMachine/BattleTimeSource/BattleFlowCoordinator`，配置映射只沿Global倒计时/失焦开关与Players终极外键读取Skills输入窗；Countdown、Playing、UltimateDrawing、Paused、Victory、Defeat边界明确，产品C#不含内容ID或玩法数值。
- T510：统一时间源分离未暂停流程、未缩放战斗和受配置Effect缩放的战斗时间；Countdown精确切分跨界delta，Playing/UltimateDrawing向T500传同一战斗delta，Paused/终态冻结。暂停倒计时保留进度；FocusLost/ApplicationPaused叠加全部解除后才恢复，终极笔迹取消后只恢复Playing。
- T510：终极只接受本局单调非零且未消费的gestureEventId与T410配置终极Activated结果，旧事件不能跨绘制重放；2.5秒配置边界有效，严格超过只取消且100能量不变。PlayerConfirmed只在Playing转发；死亡/时限与完成同帧时Defeat优先，胜负互斥且Settled仅一次；配置0.25倍/0.8秒效果产生0.2秒战斗delta。
- T510：配置只读生成/漂移门与ConfigExporter .NET 56/56通过；专项EditMode 8/8、PlayMode 2/2，最终全量EditMode 150/150、PlayMode 37/37。Bootstrap真实路径验证暂停30秒零推进、重叠生命周期恢复、终极超时0成功及有效Circle成功1次；测试临时EditorSettings差异已用Unity API恢复。未改xlsx、Schema、DTO、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK，未提前实现T520/T540/T550/T600。
- T500：新增无`MonoBehaviour`依赖的`LevelCatalog/SpawnScheduler/WaveRunner/LevelRunner`，只沿Levels/Waves/Spawns/SpawnPoints/EnemyModifiers构造3关、9波、13条出生行和35个敌人时间点；关卡/波次连续order、出生点作用域、枚举、归一化区域和Boss结束所有权均在运行时再次校验，产品C#不列内容ID。
- T500：时间线按到期时刻、spawnId和行内序号稳定展开，Single/Line/Scatter/Stagger在表内归一化矩形取点；maxAlive及世界拒绝都保留当前请求重试，只有世界回执正且唯一实体ID后提交。请求完整携带lane/facing及精英HP/伤害/速度/评分/染色/额外Buff修饰，Level层不直接依赖敌人池或Inspector数值。
- T500：AllEnemiesDefeated要求计划提交完且活动为0，BossDefeated要求配置Boss实体死亡，TimeElapsed使用本波唯一endDelay作为持续时间；PlayerConfirmed只消费当前门，提前/暂停确认不锁存。暂停冻结关卡时钟、波次、出生和确认；durationLimit只公开事实并留给T510裁决。
- T500：配置只读生成/漂移门与ConfigExporter .NET 56/56通过；专项EditMode 8/8、PlayMode 2/2，最终全量EditMode 142/142、PlayMode 35/35。Bootstrap真实配置路径完成教学关3波10怪和Boss关6个前置敌人+1个Boss，全部出生坐标在[0,1]；最终刷新编译与Console Error/Warning为0。未改xlsx、Schema、DTO、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK，未提前实现T510/T520/T530/T540。
- T460：新增无`MonoBehaviour`依赖的`BossPhaseCatalog/BossPhaseStateMachine`，从`BossPhases`及移动、攻击、防御、弱点、进入效果和文案外键构造阶段。运行时拒绝非Boss归属、断号、阈值空洞/重叠、缺失进入效果或文案；等于边界切换，HP回升/重复观察不重放，大伤害跨多段仍按order逐段发布。
- T460：`BossPhaseController`监听Boss HP事件，切换时取消旧攻击/眩晕并回到Move，保留当前HP/上限、按新阶段重置护甲和弱点、释放旧策略订阅后从新移动/攻击表重建运行时；每段`onEnterEffectGroupId`经T410统一效果链执行后只发布一次阶段事件，不读取动画长度。
- T460：镇墓玄甲王三阶段由配置完整覆盖：HP边界`1→0.67→0.34→0`，攻击集phase1/2/3，护甲`120/60/0`，弱点由无切为封印弱点。新增二、三阶段Boss移动模板后，基础速度40按表内倍率得到20/32/48；内存配置变体证明阈值、速度、防御和弱点改动无需修改产品C#。
- T460：双工作簿保持字节一致，SHA-256均为`6c931323...16b1`；受管JSON为172,699字节/662条/content hash `95c42832...be4b`，27组315个ID常量与样例同源。ConfigExporter构建0 warning/0 error、.NET 56/56和只读三生成物漂移门通过。
- T460：专项EditMode 4/4、PlayMode 1/1，最终全量EditMode 134/134、PlayMode 33/33；真实Boss路径执行三段进入效果及落石/封印波/冲撞各一次，阶段事件各一次，最终Unity刷新编译与Console Error/Warning为0。未修改场景、Prefab、Registry、Packages、ProjectSettings或微信SDK，未提前实现T500/T510/T540。
- T450：新增只读`IConfigProvider.GetEnemies()`和`EnemyArchetypeCatalog`，从现有七条`Enemies`中自动排除Boss并按ID稳定聚合定义、移动、攻击、防御、弱点、中英文文案和资源类型；当前结果为5普通+1精英，产品C#中没有六怪ID列表或每怪业务子类。
- T450：用层级、移动、架势易伤、防御笔势/架势、弱点窗、攻击动作/打断和弹体交互生成不含ID与数值的教学特征，六怪摘要全部唯一。所有攻击均有正前摇且打断窗跨过执行边界；符火鱼妖可切可弹火符、轮车僵妖横斩打断、石甲龟妖蓄力破甲、骷髅幽魂符术/弧线克制、飞行符蝠斜斩俯冲、摄魂道傀护盾支援分别可断言。
- T450：`EnemyArchetypePool/Actor`按AssetManifest的5个Sprite键和1个Prefab键路由T240 Registry资源，为六怪建立T440配置池并预热共29个实例。Actor只在精确租约内Spawn和拥有T430策略运行时，回收先释放Telegraph/事件订阅再完整重置`EnemyController`；仍诚实使用T240类型占位，未伪报正式动画、美术或身体碰撞形状。
- T450：配置变体证明修改符火鱼妖HP 30→47、速度110→137、攻击/弹体伤害8→13不需改产品C#。专项EditMode 3/3、PlayMode 1/1，最终全量EditMode 130/130、PlayMode 32/32；ConfigExporter .NET 56/56、只读生成/漂移门、Unity 6000.5.1f1刷新编译均通过。真实Bootstrap路径同时生成六怪，执行2投射物/1冲撞/2近战/1支援后全部回收，活动泄漏为0。
- T450：未修改xlsx、FieldDictionary、Schema、导出器、受管JSON/hash/ConfigIds、场景、Prefab、AssetRegistry、Packages、ProjectSettings或微信SDK；未提前实现T460/T500/T510/T630。
- T440：新增无Unity依赖的`ObjectPoolService`、`IPoolable`、租约/重开generation、共享family容量、泄漏报告和确定性耗尽策略。`Reject`在容量满时保持池状态不变，`ReuseOldest`按family激活序号完整回收最旧对象；旧租约、重复释放和未知对象不会释放当前租约，`activeSelf`不作为容量真相。
- T440：`ObjectPoolConfiguration`把敌人/投射物/VFX/伤害数字容量、预热和耗尽策略全部映射自配置。配置保持schema 4并升级content 0.5.x，新增每类耗尽策略和投射物每类型预热；双工作簿SHA-256均为`cc5e9b11...c42`，受管JSON 172,045字节/660条/hash `d524ffcd...1ee8`，27组313个ID常量与样例保持同源。
- T440：`EnemyController`与`ProjectileController`接入显式池租约并恢复池父节点；敌人回收额外清除外部战斗事件订阅/序号、HP/护甲/Buff/计数/攻击/弱点/时钟/目标，投射物清除规则/归属/参考空间/位置方向/时间/命中与Collider。新增配置驱动`VfxPoolItem`和无表外寿命常量的`DamageNumberPoolItem`，均完整清空运行态与Transform。
- T440：PlayMode真实加载Bootstrap配置后预热44个对象，以`enemy_skeleton_ghost`、`proj_ghost_fire`、`vfx_ultimate_prepare`和伤害数字连续执行3轮生成、Buff/计数污染、击杀、弹体/VFX/数字污染、清场与重开；每轮复用同一四个实例、旧敌人事件监听计数不再变化、活动泄漏归零、generation最终为4。
- T440：专项EditMode 5/5、PlayMode 1/1，最终全量EditMode 127/127、PlayMode 31/31；ConfigExporter .NET 56/56、配置只读生成/漂移门、Unity 6000.5.1f1刷新编译均通过，最终Console Error/Warning为0。Unity测试造成的`ProjectSettings/EditorSettings.asset`临时变化已恢复；未修改场景、Prefab、Packages或微信SDK，未提前实现T450/T460/T500/T620。
- T430：新增无`MonoBehaviour`依赖的`MovementStrategyRegistry`，显式覆盖Linear/Sine/Dive/Hover/Boss；路径、参考分辨率、速度、循环、振幅与频率只从配置映射，未知类型失败。移动采样使用调用方累计时钟，Root/Slow由外部按配置效果控制时钟推进，不引入NavMesh/A*或隐藏阈值。
- T430：新增显式`AttackStrategyRegistry`，覆盖Cooldown/Distance/Support/HpThreshold，按配置order/weight稳定选取，并从效果/投射物合同生成近战、投射物、冲撞或支援动作；`DefenseRuleService`公开配置笔势、架势、倍率、反伤与破甲意图，不复制T360公式。
- T430：`EnemyStrategyRuntime`在T420 `BeginAttack`时先打开Telegraph，并在`Windup→Attack`状态边界恰好执行一次动作；恢复、眩晕、死亡和回收关闭/清空预警，动画事件不承担唯一伤害真相。支援路径通过T410真实效果链给骷髅盟友施加配置护盾，10点来伤变5，配置3秒到期后恢复为10。
- T430：配置闭环升级为schema 4/content 0.4.x，新增`DamageReduction`、`buff_shield_50`、`fx_puppet_shield`和中英文文案。双工作簿SHA-256均为`d3b281c5...b52e8`，受管JSON 170,196字节/653条/hash `61ed49c0...351f2`，27组308个ID常量与样例保持同源。
- T430：专项EditMode 5/5、PlayMode 1/1，最终全量EditMode 122/122、PlayMode 30/30；ConfigExporter .NET 56/56、配置只读导出/漂移门、最终Unity 6000.5.1f1刷新编译均通过，Console Error/Warning为0。未修改场景、Prefab、Packages、ProjectSettings或微信SDK，未提前实现T440对象池、T450内容装配、T460 Boss阶段或T510流程。
- T420：新增纯C# `EnemyStateMachine`，明确`Spawn→Move→Windup→Attack→Recovery→Move`与`Stun/Dead/None`分支；攻击前摇、有效段、完整周期和打断笔势/窗口只从`EnemyAttacks`读取，跨帧可追赶多个边界，死亡、打断和回收均幂等，复用后时钟与战斗状态完整重置。
- T420：`EnemyDefinitionFactory`统一映射`Enemies/DefenseRules/WeakpointRules`，`EnemyDamageModel`按护甲优先、溢出进HP结算并只在护甲首次归零发布配置`breakEffectGroupId`意图；`Damageable`接入T350 `IHittable`，`EnemyController`统一消费T360伤害、T370反弹投射物伤害和T410伤害/治疗/Buff/破甲/击退/处决/计数端口，未创建每怪子类或Inspector数值库。
- T420：`WeakpointController`只在配置的攻击相对时间窗内启用触发Collider；T360弱点结果还需同时满足当前攻击配置笔势与打断窗才进入显式恢复的`Stun`。真实Mouse斜划经T300–T360正式链路命中Boss弱点，造成1点护甲伤害、护甲120→119，并打断phase1配置攻击进入Stun。
- T420：专项EditMode 7/7、PlayMode 1/1，最终全量EditMode 117/117、PlayMode 29/29；配置只读导出/漂移门通过、ConfigExporter .NET 56/56，最终Unity刷新编译与Console Error/Warning为0。
- T420：未修改xlsx、FieldDictionary、Schema、导出器、受管JSON/hash/ConfigIds、配置DTO、Combat实现、场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK；未提前实现T430策略注册表、T440通用池、T450敌人装配、T460 Boss阶段或T510战斗流程。
- T410：`SkillService`统一执行`Skills`触发类型、有效笔势/输入窗、T400架势/能量和每技能CD门，再按`SkillEffects.order`稳定调用显式`IEffectExecutor`；`ExecuteEffectGroup`也消费T400发布的`onSwitchEffectGroupId`意图并复用同一路径。无效、超时、冷却、架势错误、能量不足或死亡均不部分扣能。`comboCount>=3`由受限比较表达式执行，未知语法在扣能前失败。
- T410：显式注册Damage/Heal/Buff/破甲/击退/重复笔迹/慢动作/低血处决/下笔倍率/计数/VFX/清弹12类效果，不用反射或每技能MonoBehaviour；`TargetType`覆盖主目标、世界、半径、上笔、手势区域、全敌、非Boss和Boss，并保持调用方目标顺序。配置行克隆出的`skill_test_heal`无需产品C#分支即可生效，治疗封顶且不复活。
- T410：配置闭环升级为schema 3/content 0.3.x；Enums/Schema/导出器注册表补齐`Heal`和`ClearProjectiles`，终极链冻结为减速→清弹→全敌50伤害→普通敌25%低血处决→Boss 2秒易伤。双工作簿SHA-256均为`eb7cd040...311e3`，受管JSON 169,528字节/650条/hash `ef7eec3a...b3c2`，JSON/hash/IDs与样例同源。
- T410：无效终极笔势保持100能量且零效果；有效Circle事件在2.5秒输入窗边界完成后清2枚弹并按5步执行。专项EditMode 4/4、PlayMode 1/1，ConfigExporter .NET 56/56，ConfigPipeline Unity 19/19 + 3/3，最终全量EditMode 110/110、PlayMode 28/28；最终脚本刷新编译与清理预期测试日志后Console Error/Warning为0。
- T410：未修改场景、Prefab、Input Actions、Packages、Combat实现或微信SDK；T420敌人适配、T440实际弹池、T510完整`UltimateDrawing`流程、T600 HUD和T620表现仍按任务边界后续接入。
- T400：`PlayerCombatSettingsFactory`沿`Players`映射HP/能量上限、默认架势、终极技能和受击无敌，并沿`ultimateSkillId`读取`Skills.energyCost`；`PlayerCombatModel`初始满HP/空能量，伤害夹到0，配置无敌窗在精确边界恢复，同帧重复致死不会重复标记。
- T400：T360 `DamageResult.energyAward`进入当前能量时按配置上限无溢出饱和；技能扣能直接读取`Skills.energyCost/requiredStanceId`，架势不符、能量不足或死亡均不部分扣除。终极100能量、符术20能量等值未复制到Inspector或产品代码。
- T400：`StanceService`把目标`Stances`行完整映射为不可变快照，成功切入目标后按该目标的`switchCooldownSec`计算冷却，等于边界可再次切换；同架势/冷却内/死亡请求均不发布事件。成功切换携带配置`onSwitchEffectGroupId`意图，实际EffectGroup执行留给T410。
- T400：`PlayerCombatController`发布单调序号`HpChanged/EnergyChanged/StanceChanged/Died`事件，致死固定先HP变化后死亡且生命周期只一次。当前架势直接驱动T340轨迹宽度、T360伤害公式/倍率和T370投射物`requiredStanceId`门，并公开配置切弹/幽魂倍率，不维护刀/符第二映射。
- T400：专项EditMode 8/8、专项PlayMode 2/2，最终全量EditMode 106/106、PlayMode 27/27；配置三生成物无漂移、.NET 55/55，最终脚本刷新编译Console Error/Warning为0。Bootstrap→MainMenu真实运行时路径验证刀18→符28参考像素、伤害公式切换、`proj_seal_bolt`从架势不符变为反弹、能量扣除和同帧一次死亡。
- T400：未修改xlsx、Schema、FieldDictionary、导出器、受管JSON/hash/ConfigIds、场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK；未实现T410技能CD/效果链、T420敌人状态机、T510战斗流程或T600 HUD。
- T370：`ProjectileRuleSetFactory`从现有`Projectiles`表完整映射移动策略ID、参考像素速度、寿命、伤害、切断/反弹开关、所需架势、命中半径和资源键；未修改xlsx、Schema、导出器或三生成物，也未在Prefab/Inspector/C#复制玩法数值。
- T370：`ProjectileCutResolver`冻结架势门→反弹→切断→不可切断优先级；`reflectable=true`时优先反弹，只有`cuttable=true`时回收，两者都false时保持弹体。反弹把当前归属切为玩家并反转显式方向，同时保留原敌方实体与反弹次数；同阵营笔迹和碰撞均不会错误消费弹体。
- T370：`ProjectileController`在参考像素空间按配置速度/寿命与外部delta做确定性Transform位移，不使用Rigidbody力；`ProjectileHitTarget`直接接入T350 `IHittable/HitRecord`。切断、敌方命中、寿命到期和显式释放均先保留快照，再清空规则、归属、参考空间、位置、方向、时间、命中ID、Collider和Transform并停用，同对象复用由新配置/来源完整覆盖。
- T370：`ProjectileDamageSource`同时携带配置`projectileId/damage`、当前归属、原始归属与反弹次数。真实Mouse横划命中`proj_ghost_fire`后由敌方7001切换到玩家101，方向左→右，0.5秒按260参考像素/秒移动130像素，再命中原敌方并以配置8点伤害归因给玩家；复用为`proj_rockfall`时旧归属/反弹/时间/半径均未残留。
- T370：专项EditMode 8/8、专项PlayMode 2/2，最终全量EditMode 98/98、PlayMode 25/25；配置三生成物无漂移、.NET 55/55，最终脚本刷新编译Console Error/Warning为0。首次PlayMode只因Unity把禁用Collider的零半径钳制为0.0001而产生错误断言，修正为验证Collider禁用与新配置覆盖后稳定通过。
- T370：未修改场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK；未实现T400玩家HP/当前能量/架势状态、T420敌人HP/状态机、T430攻击策略或T440通用对象池。
- T360：新增`Stances.damageFormulaId -> DamageFormulas.formulaId`必填外键和`DamageFormulas.scorePerDamage`伤害评分系数；配置契约升级为schema 2/content 0.2.x。双工作簿SHA-256均为`c1c04c57...edb8f`，29表渲染与公式扫描通过；受管JSON为168,862字节、647条记录、hash `19dc788f...2f733`，FieldDictionary为250条。
- T360：`DamageContext`、`DamageRuleSetFactory`和`DamageCalculator`均为无MonoBehaviour依赖纯C#；调用方只传架势/防御/弱点配置ID，公式沿架势外键解析。伤害组合架势、方向、弱点、连斩和注入随机暴击；评分同时组合命中奖励与`原始伤害 × scorePerDamage`；能量只发布本次收益，三项在公式末尾统一`MidpointRounding.AwayFromZero`。
- T360：方向成立同时检查配置笔势与可选架势，失败组合公式与防御表两级倍率并发布反伤；弱点倍率、能量/评分加值和打断标记仅在弱点命中生效。`ComboService`从`Global.combo_timeout_sec`读取1.8秒窗口，按T350稳定目标顺序逐个计数，等于边界继续、超过重启；`ScoreService`原子累计伤害、评分、已赚取能量和命中维度，不提前拥有T400玩家状态。
- T360：专项EditMode 12/12、真实Mouse到多目标结算PlayMode 1/1，最终全量EditMode 90/90、PlayMode 23/23；玩家路径首个弱点为48伤害/398分/11能量，第二目标以1.1连斩倍率得到13/123/3，累计61/521/14。配置三生成物无漂移、.NET 55/55，ConfigPipeline分类由全量回归覆盖19/19 EditMode和3/3 PlayMode；最终刷新编译Console Error/Warning为0。
- T360：未修改场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK；未扣敌人HP、控制弱点窗口、实现玩家能量上限/消耗，也未提前实现T370投射物、T400玩家状态或T420敌人状态机。
- T350：`StrokeHitResolver`直接遍历T320 `StrokeGeometryData.Points`的同一只读引用，逐段以所选`StrokeRules.hitRadiusRefPx`执行扫圆形成胶囊命中；解析结果不创建轨迹Collider、不修改目标，也不提前执行T360伤害。
- T350：`HitRecord`不可变地携带strokeId、`IHittable`目标与稳定targetId、弱点标记、归一化路径参数/参考像素路径距离、完整笔势结果和结束时间；结果按首次路径接触排序，同距离以targetId稳定裁决。
- T350：固定容量来自配置`max_active_enemies=18`与`max_active_projectiles=40`，得到58个唯一目标和含单体主体/弱点及饱和哨兵的117槽查询缓存；同一targetId跨段、主体和弱点只保留一条记录，保留最早接触并聚合弱点为真。
- T350：Unity 6000.5.1f1使用`Physics2D.CircleCast`数组重载和长期复用缓冲；预热后连续128次真实物理查询与解析的当前线程托管分配增量为0，未使用LINQ、闭包、线程或每段对象。
- T350：专项EditMode 6/6、PlayMode 2/2，最终全量EditMode 78/78、PlayMode 22/22；真实Mouse经Bootstrap配置、T300/T310/T320/T330/T340后，以视觉同一Points引用按路径命中两个Collider2D目标，主体/弱点同体去重；脚本导入编译隔离检查Console Error/Warning为0。
- T350：配置三生成物无漂移、.NET 54/54；未修改xlsx、FieldDictionary、Schema、导出器、JSON/hash/ConfigIds、DTO、场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK，也未提前实现T360伤害或T370投射物。
- T340：`StrokeTrailPath.FromGeometry`只保存T320 `StrokeGeometryData.Points`的同一只读引用，不复制或重新简化点集；`StrokeTrailView`只负责LineRenderer显示和淡出，不包含分类、碰撞或命中决策。
- T340：`StrokeTrailSettingsFactory`从调用方指定的`StanceConfig`和`VfxCueConfig`映射宽度、寿命、预热数与排序，从全部`StrokeRules.maxPointCount`取运行上限；当前真实配置得到刀18、符28参考像素、寿命0.3秒、预热12和最大80点，未建立Inspector第二数值库。
- T340：`StrokeTrailPool`初始化时一次性创建固定数组和12个视图，最多保持技术规范规定的3条活动残留；第四笔稳定回收激活序列最旧者，回收会清除strokeId、架势、源点引用、位置数、宽度、颜色、排序和Transform状态，同时保留唯一共享材质。
- T340：预热后连续128次`Show`与完整回收的当前线程托管分配增量为0；专项PlayMode 5/5、最终全量EditMode 72/72、PlayMode 20/20，真实Mouse经Bootstrap配置、T300/T310/T320后显示与几何完全相同的点集合。
- T340：配置三生成物无漂移、.NET 54/54，脚本Refresh编译后Console Error/Warning为0；通过Unity Editor API补齐配置已引用但工程原先缺失的`VFX` Sorting Layer，除此之外未修改xlsx、FieldDictionary、Schema、导出器、JSON/hash/ConfigIds、场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK，也未提前实现T350命中。
- T330：`GestureClassifier`是无MonoBehaviour、`Time`、线程、反射或全局状态依赖的纯规则层；从同一份`StrokeGeometryData`识别Any、Horizontal、Vertical、Diagonal、Arc、Circle和Charged，输入相同会得到完全相同的规则ID、类型、置信度和摘要。
- T330：`GestureRuleSetFactory`通过`IConfigProvider.GetStrokeRules()`一次性映射只读全表；最小长度、方向容差、闭合距离、最小面积、最小归一化曲率和蓄力停留全部来自`StrokeRules`，未知类型、空规则和重复规则ID显式失败，Input程序集不反向依赖Config。
- T330：方向角按首尾位移归一化到无向`[0,180)`，Horizontal/Vertical/两条Diagonal共享配置容差语义；Circle依次验证配置闭合/面积/曲率，Arc验证配置曲率，Charged验证配置停留，Any只作为配置最小长度兜底。
- T330：多规则同时命中时固定按Circle→Charged→Arc→方向类→Any的玩法语义选择，同优先级按置信度再按Ordinal规则ID裁决；置信度冻结为0～1的阈值余量摘要，结果同时携带长度、平均速度、角度、曲率、闭合比/距离、面积和首段停留。
- T330：为避免把“慢慢画完整笔”误判成蓄力，`StrokeSampler`在不改变T310过滤、裁剪、点数和分配合同的前提下记录起笔到首个有效采样点的时长；阈值内微抖不会提前结束停留，元数据经不可变`StrokeData`/`StrokeGeometryData`传递。
- T330：横/竖/正反向斜线、四分之一弧、大圆、首段停留蓄力、Any兜底、过短无匹配、23度近水平线、小面积闭环、直线非弧、未知类型和确定性回放均有回归；专项EditMode 14/14、PlayMode 1/1，最终全量EditMode 72/72、PlayMode 15/15。
- T330：真实Mouse→统一输入→配置采样→配置几何→配置规则分类得到`stroke_horizontal`，Bootstrap配置/Registry/1920×1080输入日志正常；配置三生成物无漂移、.NET 54/54，脚本Refresh编译后Console Error/Warning为0。
- T330：未修改xlsx、FieldDictionary、Schema、导出器、JSON/hash/ConfigIds、Input Actions、Packages、ProjectSettings、场景、Prefab或微信SDK，也未提前实现T340轨迹或T350命中。
- T320：`StrokeGeometry`是无MonoBehaviour、`Time`、线程或全局状态依赖的纯规则层；`Process`先按配置`rdpEpsilonRefPx`执行保留首尾的RDP，再仅在超出配置`maxPointCount`时按累计弧长等距重采样并精确保留首尾。
- T320：`StrokeGeometryData`持有供后续识别、视觉和命中共享的单一不可变处理点集，并只从该点集计算路径长度、参考像素包围盒、有向/绝对面积、首尾闭合距离、闭合比、有向转角、总绝对转角和`总转角/π`归一化曲率；原始`strokeId`、时间和终止原因可追溯。
- T320：RDP容差语义冻结为点到线段距离小于或等于epsilon即删除；面积使用首尾隐式闭合的鞋带公式；曲率忽略零长度段并按路径相邻非零段转角计算，因此尺度不影响归一化值，左右转方向与S形总弯曲可分别表达。
- T320：空集合、连续重复点、单点和零长度输入均有确定结果；非有限坐标、负容差和非法目标点数显式失败。直线、折线拐角、矩形、四分之一弧和圆均有稳定回放断言，处理不会修改输入或旧结果。
- T320：`StrokeGeometrySettingsFactory`从调用方选中的任意`StrokeRuleConfig`映射RDP epsilon与最大处理点数，Input程序集不依赖Config且不硬编码规则ID或阈值；圆规则真实映射为epsilon 5参考像素、最大80点。
- T320：最终StrokeGeometry专项EditMode 12/12、PlayMode 1/1，全量EditMode 58/58、PlayMode 14/14；真实Mouse→统一输入→采样→几何处理只发布1份共享结果，Bootstrap→MainMenu日志正常且新增Console Error/Warning为0。
- T320：配置只读检查继续保持三生成物无漂移和.NET 54/54；未修改xlsx、Schema、导出器、JSON/hash/ConfigIds、T310采样合同、Input Actions、Packages、ProjectSettings、场景或Prefab，也未提前实现T330识别或T340轨迹。
- T310：`StrokeSampler`是无MonoBehaviour、`Time`或全局状态依赖的纯规则对象；构造时按配置点数上限一次性分配固定缓冲，连续合法收点不创建List/GameObject，专项测试测得100次热路径收点托管分配增量为0。
- T310：采样以最后一个已接受点执行`minPointDistanceRefPx`过滤；接受段跨越`maxStrokeLengthRefPx`时沿该段插值到剩余路径长度，最终`StrokeData.TotalLengthReferencePixels`精确等于配置上限；收满`maxPointCount`时在最后一个合法点稳定终止并只发布一次。
- T310：完成时才复制为只读`StrokeData`快照，包含单调非零`strokeId`、处理后点、总长、起止时间和明确终止原因；采样器复用不会改写旧结果，生命周期取消只发布`StrokeCanceledEvent`且不生成可命中笔迹。
- T310：`StrokeInputCollector`直接消费T300的`IPointerInput`参考像素事件，保持活动pointerId/source所有权；`StrokeSamplingSettingsFactory`在Combat边界把任意选中`StrokeRuleConfig`的最小点距、最大长度和最大点数映射到Input，未让Input程序集反向依赖Config，也未选定或硬编码特定玩法规则。
- T310：最终StrokeSampling专项EditMode 9/9、PlayMode 1/1，全量EditMode 46/46、PlayMode 13/13；真实Mouse→Input System适配器→采集器得到单一不可变笔迹，Bootstrap→MainMenu配置/Registry/输入日志正常且新增Console Error/Warning为0。
- T310：配置只读检查继续保持三生成物无漂移和.NET 54/54；未修改xlsx、Schema、导出器、JSON/hash/ConfigIds、Input Actions、Packages、ProjectSettings、场景或Prefab，也未提前实现T320的RDP、重采样或几何量。
- T300：`IPointerInput`以同一`PointerInputEvent`合同发布Mouse/Touch的Began/Moved/Ended/Canceled，事件同时携带屏幕坐标、参考像素坐标、时间、来源、pointerId和明确取消原因；处理器只允许一个活动指针，第二根手指和同时鼠标不会接管或延长当前笔。
- T300：`ReferencePixelConverter`使用Bootstrap从配置`reference_width/reference_height`读取的1920×1080作为参考空间，并在每次事件读取动态`Screen.safeArea`；Safe Area外起笔被拒绝，合法笔迹移出后坐标夹紧到参考边界，代码不读取`Screen.dpi`。
- T300：`EventSystemPointerUiBlocker`在起笔时执行真实uGUI Raycast；UI上起笔不会发布Began，按住后移出UI也不会变成笔迹，而从非UI区域合法起笔后跨过UI仍保持连续。
- T300：`InputSystemPointerAdapter`锁定首个物理TouchControl或Mouse，失焦、应用暂停、适配器禁用、系统取消和设备断开均最多发布一次Canceled；Bootstrap成功加载配置与Registry后创建跨场景Runtime，输入初始化失败会阻断MainMenu并输出`POINTER_INPUT_FAILED`。
- T300：最终PointerInput专项EditMode 5/5、PlayMode 7/7，全量EditMode 37/37、PlayMode 12/12；真实Bootstrap→MainMenu输出`POINTER_INPUT_READY ... reference=1920x1080 safeArea=dynamic uiBeginBlock=true maxActivePointers=1`，Console Error/Warning为0。
- T300：配置只读门继续保持三生成物无漂移和.NET 54/54；未修改xlsx、Schema、JSON/hash/ConfigIds、Input Actions、ProjectSettings、场景或Prefab，也未实现T310采样规则。
- T250：`Tools/CI/verify-config.sh`默认只读地完成导出器构建、临时生成、三生成物逐字节漂移检查、.NET测试及Unity `ConfigPipeline` EditMode/PlayMode；`--update`是唯一显式刷新入口，`--skip-unity`只报告`PARTIAL`，不会伪报全绿。
- T250：同一个已解析并完整校验的`PreparedExport`确定性生成168,071字节JSON、65字节hash旁车和23,125字节`ConfigIds.g.cs`；JSON保持T230字节不变，三者SHA-256分别为`91d2c312...1a066a`、`5d591e73...b71c7a`、`b5c3b1ba...6b988`。
- T250：生成ID位于`OneStrokeDemon.Config`程序集作用域，覆盖27个ID集合与306个稳定字符串常量，只含配置ID和生成元数据，不复制HP/CD/伤害等玩法数值；缺失、字节漂移、非法ID或标识符碰撞统一以`CFG013`失败。
- T250：受控修改hash旁车后只读命令以退出码3和统一diff检出，随后由`--update`恢复；ConfigPipelineE2E与CLI测试使.NET全套达到54/54，Unity分类EditMode 19/19、PlayMode 3/3，全量EditMode 32/32、PlayMode 5/5。
- T250：真实Bootstrap→MainMenu再次记录配置28表645条、Registry 76键，Console Error/Warning为0；隔离工程副本中的完整默认一键入口最终输出`CONFIG_PIPELINE_PASS dotnet=PASS drift=PASS editmode=PASS playmode=PASS`。
- T240：Canonical `AssetRegistrySO`精确覆盖AssetManifest的76个键：Prefab 40、Sprite 18、AudioClip 17、Scene 1；条目只序列化`assetKey`和Unity对象引用，场景仅使用独立`AssetSceneReference`保存明确场景引用，不保存HP/CD/伤害等平衡值。
- T240：`AssetRegistryService`一次性校验空键、空对象、重复、缺失、额外和错型后才原子发布Ordinal只读索引；提供Prefab/Sprite/AudioClip/Scene类型化查询，未知ID和错型查询返回稳定`ARREG`错误码。
- T240：Editor作者工具可创建或修复Canonical Registry，并在重跑时保留每个键已有的合法类型引用；当前正式资源尚未接入，因此40个Prefab、18个Sprite和17个AudioClip键分别复用一个受管占位资源，`scene_battle`明确引用Build Settings中的Battle场景。
- T240：Editor菜单与`IPreprocessBuildWithReport`构建门同时核对76键覆盖、持久化资源、Prefab资产和启用场景；Runtime和Editor绑定均不使用AssetManifest的`addressOrPath`，替换Unity对象无需修改配置ID。
- T240：Bootstrap在配置校验成功后加载Registry，只有两者都成功才进入MainMenu；专项EditMode 5/5、全量EditMode 30/30、全量PlayMode 5/5、ConfigExporter .NET 46/46与正式严格校验通过，真实Bootstrap→MainMenu记录76键分类摘要且Console Error/Warning为0。
- T230：提交工具生成的初始Runtime快照，schema `1` / content `0.1.1-sample` / hash `16b64a6f...b4b1c`，28表645条、168,071字节、文件SHA-256 `91d2c312...1a066a`；与正式工作簿重新导出结果字节一致。
- T230：28表DTO与冻结Schema的248字段精确对齐；`GameplayConfigService`严格拒绝未知/缺失/重复/null属性、不兼容版本、根/Global版本分叉、hash篡改、空键与重复主键/组合键，失败不发布部分状态。
- T230：一次解析后构建只读主键字典和分组列表，业务只通过 `IConfigProvider` 显式查询；Bootstrap在成功日志后才进入MainMenu，不在热路径反序列化或使用反射驱动战斗。
- T230：Unity Runtime直接固定 `com.unity.nuget.newtonsoft-json 3.2.2` / Newtonsoft.Json `13.0.2`；Unity Companion License与包内第三方MIT边界已记录，依赖不再偶然来自开发期MCP传递引用。
- T230：专项EditMode 12/12、专项PlayMode 2/2、全量EditMode 25/25、全量PlayMode 4/4及ConfigExporter .NET 46/46通过；真实Bootstrap→MainMenu记录source/hash/28表/645条，Console新增Error/Warning为0。
- T220：`ConfigValidator`已接入T210同一内存模型，并在序列化/原子写入前执行；任一错误均拒绝整包，不半应用或静默修正，诊断包含稳定`CFG`错误码、Sheet、Excel数据行和字段。
- T220：覆盖必填、类型/范围、稳定ID、主键/组合键、枚举、代码策略登记、普通/分组/唯一通配符/conditional外键、连续order、Global联合、星级阈值、Level→Wave→Spawn/出生点作用域和Boss从1到0无缝覆盖。
- T220：提供Schema时同步核对FieldDictionary/Enums的类型、可空性、min/max和枚举集合，防止工作簿与Schema静默分叉；正式源、镜像、Schema和样例JSON均未修改。
- T220：37类坏配置只修改正式工作簿读取后的内存副本；专项38/38、全套.NET测试46/46通过，锁定还原、Release编译0 warning/0 error和格式检查均通过。
- T220：正式CLI生产校验28表645条记录通过，保持schema `1` / content `0.1.1-sample` / hash `16b64a6f...b4b1c`；两次导出均168,071字节，文件SHA-256均为`91d2c312...1a066a`。
- T210：`Tools/ConfigExporter` 已实现 `validate/export` CLI；固定读取29个Sheet并导出28张数据表，基础类型使用InvariantCulture，字符串Trim，固定根/字段顺序和最短小数格式，不依赖Excel数据行号或系统区域设置。
- T210：`DocumentFormat.OpenXml 3.5.1`、`Microsoft.NET.Test.Sdk 17.14.1`、xUnit `2.9.3`及runner `3.1.5`均使用精确版本约束并提交锁文件；许可证为MIT/Apache-2.0，所有依赖限定在Tools，不进入Unity Runtime。
- T210：真实CLI读取645条记录，保持schema `1` / content `0.1.1-sample` / hash `16b64a6f...b4b1c`；双导出均为168,071字节、文件SHA-256均为`91d2c312...1a066a`，字节完全一致。
- T210：专项.NET测试8/8通过，覆盖冻结hash与样例语义、反转源行、表头漂移、fr-FR区域设置、CLI错误码、临时自检失败保护旧输出；锁定还原和编译0 warning/0 error。
- T210：原子写先在目标同目录写`.tmp`并落盘，自检顶层顺序、版本、记录数与hash后替换；本任务没有修改xlsx/Schema/样例或Unity资产，也没有生成受管Runtime JSON。
- T200：正式源固定为 `Design/Config/GameConfig.xlsx`，`config/一笔镇妖_游戏配置表模板.xlsx` 只做字节一致镜像；最终两文件SHA-256均为 `aa215a1fd5c798da97e5d21a7ab9b71b3ee1dd3f2326c424e17f023ef80ca52a`。
- T200：按GAME_DESIGN修正关卡玩法ID为 `lv_001_tutorial`、`lv_002_cave`、`lv_003_boss`，Boss玩法ID为 `boss_tomb_king`；所有Level/Wave/Spawn/BossPhase/Reward依赖引用同步，文案键和资源键保持独立命名空间。
- T200：FieldDictionary保留248条非递归字段记录，修正Global互斥类型列及10个主键/条件字段的必填语义；冻结Schema属性存在与Excel单元格非空的差异、空值转换、普通/分组/通配符/conditional外键和数据所有权。
- T200：29表、14公式、248字段、Schema/样例、类型/范围/枚举、主键、外键、连续order、关卡-Boss语义和规范化contentHash的只读契约审计全部PASS；29表最终渲染经4张总览拼图复核，无结构或版式异常。
- 执行顺序调整：用户明确要求暂时绕过T120及微信开发者工具/打包问题，先完成游戏主要内容；T120保持`BLOCKED`、T130保持`BACKLOG`，不删除MVP平台验收要求。T700曾在P6内容任务完成后进入`READY`，但发现缺少玩家可点击的正式入口后已暂停为`BACKLOG`，当前先评审T660。
- T120 G2：Unity `6000.5.1f1` 基于固定 embedded WXSDK 成功生成 `Builds/WeChat/T120`；84个文件、总计101,901,218字节，其中`minigame` 12,008,520字节，JSON结构、关键文件、SHA-256清单与敏感占位扫描均通过。
- T120构建参数：空AppID、横屏、256MB、触摸启用、Development、关闭渲染线程与性能分析、清理构建，并使用SDK受支持的多线程Brotli；可复现入口为`Tools/CI/build-wechat.sh`。
- T120 G2结论为`PASS WITH KNOWN ISSUES`：默认单线程Brotli在当前macOS Unity安装布局下引用错误路径（BUG-0005，已用配置规避）；转换含93条未匹配替换规则及6条Emscripten warning（BUG-0006），需要G3实际运行判定影响。
- T120回归：全量EditMode 13/13、PlayMode 2/2通过；Bootstrap→MainMenu真实Unity玩家路径未回归。构建封装完成后`ProjectSettings`、SDK源码和URP资源无残留差异。
- T120 G3：本机无微信开发者工具，文件系统、bundle id、常见CLI路径和应用清单探测均未发现，状态`BLOCKED_MISSING_DEVTOOLS`。
- T120 G4：Unity内置ADB无已连接Android设备，本机亦无可用iOS设备工具或已连接手机证据；且G4依赖G3预览/二维码能力，状态`BLOCKED_MISSING_DEVICE_AND_G3`。
- T110：重新确认官方分发源为 `wechat-miniprogram/minigame-tuanjie-transform-sdk`；固定v0.1.33 commit `ed4ad28f...`、MIT许可证、上游tree和SHA-256，旧禁用仓库未作为安装源。
- T110原始导入：Unity 6000.5.1f1因上游`Object.GetInstanceID()`触发CS0619，且产生`WxEditor`缺失级联错误；原始条目已归档。
- T110兼容补丁：Package Manager embedded完整SDK，只在`UNITY_6000_5_OR_NEWER`切换到`GetEntityId/EntityId.ToULong`；补丁后Console Error/Exception 0，专项2/2、全量EditMode 10/10、PlayMode 2/2通过。
- T110只证明官方来源、依赖可复现和导入编译；G2转换、G3 DevTools、G4真机仍为NOT RUN。SDK的6条非阻断warning登记为BUG-0004。
- T100：Unity 6000.5.1f1标准WebGL构建PASS，耗时9分9秒，总输出12,433,772字节；Brotli产物、SHA-256和HTTP headers已归档。
- T100浏览器：MainMenu canvas实际运行；Input System点击、AudioSource播放启动、中文UTF-8 interop均PASS；PlayerPrefs重载计数1→2；Console Error 0。
- T100回归：专项EditMode 2/2、最终全量EditMode 8/8、PlayMode 2/2通过；初次7/8失败及修复已记录。
- T100只证明G1；G2微信转换、G3 DevTools、G4真机仍为NOT RUN。Web warning与MCP桥接问题登记为BUG-0001至BUG-0003。
- T040：EditMode/PlayMode批处理命令可独立生成NUnit XML，并由结果检查器把失败、零测试和损坏XML转换为非零退出码。
- T040：标准WebGL构建入口已编译并通过参数合同测试；实际Web构建明确留给T100，未生成Builds/WebGL。
- T040：verification/白名单模板、Git基线记录、防覆盖证据初始化、日志卫生和一任务一提交流程已文档化。
- T040专项2/2、批处理全量EditMode 6/6、批处理全量PlayMode 2/2通过；真实Bootstrap→MainMenu路径与最终Console Error 0均已复核。
- T030：目标目录、十个Runtime asmdef、Editor asmdef和现有EditMode/PlayMode测试程序集边界已建立，依赖图无环。
- T030：Bootstrap、MainMenu、Battle由Unity MCP创建并保存；Build Settings固定为三场景，Bootstrap可自动进入MainMenu并通过场景流接口进入Battle。
- T030专项与回归：AssemblyDependencyTests 1/1、SceneFlowSmokePlayModeTests 1/1、全量EditMode 4/4、全量PlayMode 2/2均通过；场景校验和最终Console Error为0。
- T000：玩法、MVP范围、技术边界、配置唯一真相源和完成定义已统一。
- T010：建立根 `.gitignore`，现有Unity工程与开发合同已纳入唯一Git根。
- T020：Graphics与Low/High质量档统一使用URP 2D；Unity MCP依赖固定到commit。
- T020 EditMode 3/3、PlayMode 1/1通过；Mouse与Touchscreen均可驱动同一个Pointer Action。
- TMP与Unity Test Framework程序集已加载，测试程序集可发现并独立运行。
- Unity 6000.5.1f1成功加载SampleScene并进入Play Mode 2秒；探针记录Console Error/Exception/Assert为0。
- T010曾确认SampleScene包含Main Camera与Global Light 2D；T030保留该资产，并由新三场景替换其Build Settings入口。
- 接受现有Unity `6000.5.1f1` 与“仓库根目录即Unity工程根”的基线决策。
- 已确认当前仓库内只有一个Git根；Android、WebGL、macOS和Windows构建模块已安装。
- 玩法、MVP、技术、配置、平台、测试和原子任务计划已建立。
- 已生成Excel配置模板、示例JSON和schema。
- 已纳入工程复盘中的唯一真相源、平台前置Spike、配置闭环、证据分层和一任务一提交方法。

## 当前风险

1. T120缺少微信开发者工具及至少一台可运行微信的连接手机，G3/G4无法执行；按用户决定暂时延期，但G2成功仍不能外推为小游戏实际可运行，发布前必须恢复。
2. G2存在93条未匹配替换规则与6条Emscripten warning（BUG-0006）；只有导入开发者工具并运行后才能判定是否影响启动或交互。
3. 官方SDK对Unity 6的公开说明仍是测试版本；本工程需要embedded单点补丁才能在6000.5.1f1编译，且仍有6条上游warning（BUG-0004）。
4. SDK默认单线程Brotli路径与当前macOS Unity安装布局不兼容（BUG-0005）；当前通过SDK公开的多线程Brotli设置完成转换。
5. 标准Web存在URP EASU不支持与PlayerPrefs手动同步弃用warning，见BUG-0001/BUG-0002；T610已在Editor Metal路径验证TMP中文，但尚未重新构建Web验证字体压缩/加载，G1仍未覆盖后台音频恢复或真机触摸。
6. 长Web构建后Unity MCP实例桥接未自动恢复，见BUG-0003；Unity batch测试与Web运行未受影响。
7. PSD主角和怪物大多为单张Sprite；T610两个静态TMP资产约2.78MB，实际Web压缩包、运行内存和真机触摸延迟仍需T730及平台门验证。
8. T630已替换18个Sprite和40个Prefab视觉占位，但17个AudioClip仍使用T240静音占位；单帧角色尚无正式动画或逐对象身体碰撞，ImageGen补图与PSD原画的细节密度存在差异，全部美术仅获原型授权，不能外推为发布品质。
9. 三个MVP关卡已接入生产主菜单和Battle组合根，用户已确认Unity Editor入口、Mac触控板笔迹与修复后视觉。`IProgressSaveStore`与震动仍待T130平台适配，T640多比例/安全区又依赖T120，因此当前证据不能外推为平台持久化、多设备布局或真机体验。
## 下一步

执行T700：补齐纯规则EditMode回归矩阵。该任务属于P7，需在下一原子任务中独立实施与提交。
