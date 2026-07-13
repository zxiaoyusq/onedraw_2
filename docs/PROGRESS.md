# PROGRESS

- 日期：2026-07-13
- 当前成熟度：T330已完成配置驱动的七类笔势识别、确定性置信度与几何摘要；P3进入低分配笔迹视觉
- 当前任务：T340
- 状态：READY
- Unity精确版本：6000.5.1f1（已由ProjectVersion.txt与本机安装核验）
- 微信SDK来源或版本：官方 `minigame-tuanjie-transform-sdk` v0.1.33 / commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` / embedded最小补丁
- Active Scene：Assets/_Game/Scenes/Bootstrap.unity
- 配置版本：schema 1 / content 0.1.1-sample / hash `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`

## 已完成

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
- 执行顺序调整：用户明确要求暂时绕过T120及微信开发者工具/打包问题，先完成游戏主要内容；T120保持`BLOCKED`、T130保持`BACKLOG`，不删除MVP平台验收要求；T210/T220/T230/T240/T250/T300/T310/T320/T330已按该顺序完成，当前唯一`READY`任务为T340。
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
5. 标准Web存在URP EASU不支持与PlayerPrefs手动同步弃用warning，见BUG-0001/BUG-0002；G1未覆盖TMP中文、后台音频恢复或真机触摸。
6. 长Web构建后Unity MCP实例桥接未自动恢复，见BUG-0003；Unity batch测试与Web运行未受影响。
7. PSD主角和怪物大多为单张Sprite；中文字体包体、Web内存和真机触摸延迟仍需后续验证。
8. T240为尚未到达资源接入阶段的75个非场景键使用按类型共享的受管占位资源；类型与覆盖合同已成立，但正式视觉、音频和逐Prefab内容仍须在T630及相应玩法任务中替换并重新校验。
## 下一步

只执行T340：实现低分配笔迹视觉、淡出和池化，并让视觉消费T320的同一处理点集但不决定分类或命中；不提前实现T350命中，也不恢复T120/T130。平台任务最迟在T640/T750前恢复。
