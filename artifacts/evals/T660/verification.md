# T660 Verification

## 追溯

- 日期：2026-07-14；触控板评审修复：2026-07-15（Asia/Shanghai）
- 任务与范围：建立生产可玩入口与Battle组合根，使`Bootstrap -> MainMenu -> Battle`通过正式按钮/关卡选择进入教学关、普通关或Boss关，并接入真实指针输入、玩家、敌人/波次、HUD、教程、反馈、结算、重开和主菜单返回。
- 明确不做：不实现T640多比例/触控适配、T700/T710回归矩阵扩展、T130微信平台存储、标准Web重构建、微信转换、DevTools或真机；不修改Packages/ProjectSettings/SDK/Builds/现有Prefab/Registry/Input Actions或用户PSD。
- 分支/提交：`main`；初始任务提交`9d2f35e0` / `T660: add production playable battle entry`；首轮评审修复提交`b73f55d4`；2026-07-15视觉复评继续使用同一T660原子范围，最终提交号见Git历史。任务在用户确认修复后视觉前保持REVIEW。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`；实现、场景/字体作者命令、编译与测试均在`artifacts/tmp/T600-unity-project`隔离Editor执行。2026-07-15已连接主工程MCP实例`onedraw_2@272e911286835fad`，确认MainMenu/Battle运行、Console和实际Game画面；用户已确认触控板能够产生笔迹，自动化只用于冻结几何复查，不冒充用户对修复后视觉的最终确认。
- 配置Schema/内容版本/hash：schema `5` / content `0.6.3-sample` / `2c005061c9a4bf806afcc6d6c16e7504b2df8b4bbecfec6edcc262900cd1dfdc`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增`MainMenuCompositionRoot/BattleCompositionRoot/BattleLaunchContext/ProductionBattleWorld`及Unity Editor场景作者命令；Editor写入MainMenu/Battle独立单位变换生产根；HUD补架势切换意图；新增T660 EditMode/PlayMode；配置新增3条入口Texts并同步双工作簿/JSON/hash/IDs/冻结测试；字体子集与静态TMP fallback同步新增5个中文码点。2026-07-15首轮评审补充生产`StrokeTrailPool`接线，并把技能音效解析修正为`AudioCues.audioKey -> assetKey -> AssetRegistry`。视觉复评新增采样点预览事件、池内预览生命周期、单位变换渲染根和独立参考空间坐标/宽度转换；Unity Editor仅把Battle场景开发对象`BattleGraybox`设为inactive，场景diff为单一active标志。未修改Prefab、Registry、配置或Input Actions。
- 用户已有改动保护：任务开始无用户未提交产品改动；本会话已存在的`artifacts/evals/T700/**`审计证据全程保留、未编辑、未暂存。用户提供的仓库外PSD未读取、复制或修改，T660无需新增位图。
- `git diff --check`：收尾执行并通过，结果见最终审查命令。
- 暂存白名单审查：仅暂存`change-whitelist.md`允许的T660文件；`artifacts/evals/T700/**`与`outputs/**`排除。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity --results-root artifacts/evals/T660/config-preflight-final`通过三生成物只读漂移门、ConfigExporter build 0 warning/0 error和.NET `58/58`；脚本明确输出`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN`，Unity部分由下列全量测试覆盖。字体作者日志输出`T610_FONT_ASSETS_READY primary=96 fallback=203 total=299 atlases=512x512+1024x1024 static=true multiAtlas=false`。场景作者日志输出`T660_SCENE_AUTHORING_PASS scenes=MainMenu,Battle`。
- T660专项EditMode XML：`2 / 2 / 0`，`test-results/editmode-t660.xml`。
- T660专项PlayMode XML：`3 / 3 / 0`，`test-results/playmode-t660-final-2.xml`。
- 最终全量EditMode XML：`197 / 197 / 0`，`test-results/editmode-full-final.xml`。
- 最终全量PlayMode XML：`49 / 49 / 0`，`test-results/playmode-full-final-3.xml`。
- 2026-07-15红测：`playmode-review-red.xml`以“生产笔迹未渲染”失败；`playmode-review-audio-red.xml`以`ARREG009 [context=sfx_switch]`失败。两份红测均在产品修复前保存。
- 2026-07-15最终专项：EditMode `2 / 2 / 0`（`test-results/editmode-review.xml`）；PlayMode `4 / 4 / 0`（`test-results/playmode-review-green-final.xml`），覆盖真实InputSystem Mouse按下/拖动/释放、命中伤害、可见轨迹和架势音效配置映射。
- 2026-07-15最终全量：EditMode `197 / 197 / 0`（`test-results/editmode-review-full.xml`）；PlayMode `50 / 50 / 0`（`test-results/playmode-review-full.xml`）。配置只读漂移门和ConfigExporter .NET `58 / 58`通过，记录于`config-review/`调用输出。
- 2026-07-15视觉复评红测：`test-results/playmode-review-visual-red.xml`以“释放前生产轨迹为空”失败（`3 / 4 / 1`），证明旧实现只在完成后闪现。
- 2026-07-15白板红→绿：未同步场景修复的隔离工程中`test-results/playmode-review-whiteboard-red.xml`以“开发灰盒仍active”失败（`3 / 4 / 1`）；同步Unity Editor保存的Battle场景后`test-results/playmode-review-whiteboard-green.xml`通过（`4 / 4 / 0`）。
- 2026-07-15视觉复评最终专项：StrokeSampling EditMode `10 / 10 / 0`（`test-results/editmode-review-visual-sampling.xml`）；StrokeTrail PlayMode `5 / 5 / 0`（`test-results/playmode-review-visual-trail-final.xml`）；T660 PlayMode `4 / 4 / 0`（`test-results/playmode-review-visual-green-final.xml`）。
- 2026-07-15视觉复评最终全量：EditMode `198 / 198 / 0`（`test-results/editmode-review-visual-full-final.xml`）；首次场景全量`playmode-review-visual-full-final-2.xml`以T030旧假设“Battle灰盒必须active”失败（`49 / 50 / 1`），同步把冒烟合同改为“对象存在但Battle中inactive”后专项`playmode-review-scene-smoke-green.xml`为`1 / 1 / 0`，最终PlayMode `50 / 50 / 0`（`test-results/playmode-review-visual-full-final-3.xml`）。配置只读漂移门、ConfigExporter build 0 warning/0 error和.NET `58 / 58`通过，记录于`config-preflight-review-visual-final.txt`。
- Metal图形专项：`1 / 1 / 0`，`test-results/playmode-graphics.xml`；触发生产Start/Level Button监听器，随后真实InputSystem鼠标笔迹命中敌人并扣HP/刷新HUD。
- Console新增Error/Warning：2026-07-15主工程现场捕获产品异常`ARREG009 [source=AssetRegistry:asset_registry, context=sfx_switch]: Unknown asset key 'sfx_switch'`，堆栈指向`ProductionBattleWorld.PlayAudio`；修复后专项和全量日志不再出现该异常。视觉复评刷新编译后读取产品Error/Warning为0。主Editor仍显示无产品脚本堆栈的`currentFileSystemTime.ticks != 0 using check file Temp/FSTimeGet-*` Assert，归类为Editor内部已知问题而非游戏脚本异常，不吞掉也不伪报为产品PASS。最终批处理日志无编译错误、运行异常、缺字、双EventSystem或测试失败。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap初始化配置/Registry/Input后自动进正式MainMenu；3个配置关卡按钮按进度锁定。教学关存在HUD和Tutorial，完成标记返回玩家路径可出生敌人；一笔真实鼠标输入`completedStrokeCount=1`、`lastResolvedHitCount>0`、敌HP下降且评分非0。解锁后`lv_002_cave`进入非Boss生产会话并出生敌人，`lv_003_boss`进入Boss生产会话并出生Boss。玩家致死显示Defeat，Restart使`SessionGeneration=2`并创建不同会话，MainMenu按钮返回正式入口。
- Unity Editor人工窗口点击：用户已明确确认主Unity入口和触控板输入可用；视觉复评仍为**REVIEW**。无轨迹现场枚举到`BattleGraybox`为active的8×4 MeshRenderer，临时设为inactive后白板立即消失；Unity Editor正式保存后资源复读确认`active=false`。冻结诊断轨迹返回`transform.lossyScale=(1,1,1)`、`referencePixelWorldScale=0.009835`、配置18参考像素对应宽度`0.177025`、4点、`useWorldSpace=true`、`preview=true`；截图无白板，折线连续可见。自动化几何复查不替代用户对实际拖动观感的确认。复核步骤：打开Bootstrap，Play，点击“开始游戏”→“幽菌古道”，先点“跳过”或完成提示，然后在非UI区域按住触控板并拖动；确认轨迹随手指连续增长、宽度正常、中央没有白板，松开后仅短暂淡出；再命中敌人确认扣血/评分，并点“切换流派”确认Console无`ARREG009`。
- 标准Web：NOT RUN（T660明确不做；既有T100基线不能替代本次入口构建）。
- 微信转换：NOT RUN（按用户延期决定）。
- DevTools：BLOCKED（既有T120缺工具）。
- 真机：BLOCKED（既有T120缺DevTools和设备）。
- 截图/日志/产物：`production-entry-1920x1080.png`为1920×1080 Metal实际渲染，2,799,305字节，SHA-256 `5077c53c4af436934344a75c4faca60a850fe354396f9efe8f8d988064e1a0cf`；已目检标题“一笔镇妖”、选择关卡与三关按钮完整、无replacement glyph或裁切。`production-live-trail-review-2026-07-15.jpg`为主Editor现场复查截图，150,029字节，SHA-256 `289d97a907f6846371778f58e3e15d7e5ded0a5306ddd891923acd1294fd3f95`；禁用已确认的开发灰盒后，结果层后可见连续细折线且中央白板消失。数值记录见`main-editor-visual-review-2026-07-15.txt`。工作簿交付为`Design/Config/GameConfig.xlsx`（95,553字节，SHA-256 `9f2330c94bc86f67563feba816cd2b8acb3f05cbd2f2f5811431ee891a4233ee`）。

## 结论

- 已知问题：用户已确认触控板能够画线，修复后的实时轨迹观感待确认；Unity `Temp/FSTimeGet-*` Assert无产品堆栈但仍作为Editor已知问题保留。T130平台存储、T640多比例/安全区、标准Web、微信DevTools/真机仍在各自任务边界。生产世界当前复用既有策略/反馈能力，不在T660扩展新的投射物视觉或敌人修饰规则。
- 结论：**REVIEW**。实时笔迹、渲染空间和配置音效接线均已完成红→绿修复，主Editor画面、编译、配置、专项和全量回归PASS；等待用户确认中央白板消失且拖动轨迹清楚后再把T660标DONE并释放T700。
