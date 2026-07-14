# T660 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）
- 任务与范围：建立生产可玩入口与Battle组合根，使`Bootstrap -> MainMenu -> Battle`通过正式按钮/关卡选择进入教学关、普通关或Boss关，并接入真实指针输入、玩家、敌人/波次、HUD、教程、反馈、结算、重开和主菜单返回。
- 明确不做：不实现T640多比例/触控适配、T700/T710回归矩阵扩展、T130微信平台存储、标准Web重构建、微信转换、DevTools或真机；不修改Packages/ProjectSettings/SDK/Builds/现有Prefab/Registry/Input Actions或用户PSD。
- 分支/提交：`main`；任务提交信息`T660: add production playable battle entry`（提交号见Git历史；当前状态因人工Editor点击门为REVIEW）。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`；实现、场景/字体作者命令、编译与测试均在`artifacts/tmp/T600-unity-project`隔离Editor执行。主工程Unity进程存在但MCP列出0个连接实例，Computer Use状态读取持续超时；未把人工点击写成PASS。
- 配置Schema/内容版本/hash：schema `5` / content `0.6.3-sample` / `2c005061c9a4bf806afcc6d6c16e7504b2df8b4bbecfec6edcc262900cd1dfdc`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增`MainMenuCompositionRoot/BattleCompositionRoot/BattleLaunchContext/ProductionBattleWorld`及Unity Editor场景作者命令；Editor写入MainMenu/Battle独立单位变换生产根；HUD补架势切换意图；新增T660 EditMode/PlayMode；配置新增3条入口Texts并同步双工作簿/JSON/hash/IDs/冻结测试；字体子集与静态TMP fallback同步新增5个中文码点；更新受影响文档和证据。
- 用户已有改动保护：任务开始无用户未提交产品改动；本会话已存在的`artifacts/evals/T700/**`审计证据全程保留、未编辑、未暂存。用户提供的仓库外PSD未读取、复制或修改，T660无需新增位图。
- `git diff --check`：收尾执行并通过，结果见最终审查命令。
- 暂存白名单审查：仅暂存`change-whitelist.md`允许的T660文件；`artifacts/evals/T700/**`与`outputs/**`排除。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity --results-root artifacts/evals/T660/config-preflight-final`通过三生成物只读漂移门、ConfigExporter build 0 warning/0 error和.NET `58/58`；脚本明确输出`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN`，Unity部分由下列全量测试覆盖。字体作者日志输出`T610_FONT_ASSETS_READY primary=96 fallback=203 total=299 atlases=512x512+1024x1024 static=true multiAtlas=false`。场景作者日志输出`T660_SCENE_AUTHORING_PASS scenes=MainMenu,Battle`。
- T660专项EditMode XML：`2 / 2 / 0`，`test-results/editmode-t660.xml`。
- T660专项PlayMode XML：`3 / 3 / 0`，`test-results/playmode-t660-final-2.xml`。
- 最终全量EditMode XML：`197 / 197 / 0`，`test-results/editmode-full-final.xml`。
- 最终全量PlayMode XML：`49 / 49 / 0`，`test-results/playmode-full-final-3.xml`。
- Metal图形专项：`1 / 1 / 0`，`test-results/playmode-graphics.xml`；触发生产Start/Level Button监听器，随后真实InputSystem鼠标笔迹命中敌人并扣HP/刷新HUD。
- Console新增Error/Warning：Unity启动时先连到旧License通道并报告协议不兼容/Token不可用，随后自动连接`LicenseClient-...-6000.5.1`、解析Unity Personal授权并以code 0完成测试；该启动噪声不来自产品代码。除此之外，最终全量日志无编译Error/Warning、运行异常、缺字、双EventSystem或测试失败；旧T610弃用测试API已改为Unity 6无排序重载。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap初始化配置/Registry/Input后自动进正式MainMenu；3个配置关卡按钮按进度锁定。教学关存在HUD和Tutorial，完成标记返回玩家路径可出生敌人；一笔真实鼠标输入`completedStrokeCount=1`、`lastResolvedHitCount>0`、敌HP下降且评分非0。解锁后`lv_002_cave`进入非Boss生产会话并出生敌人，`lv_003_boss`进入Boss生产会话并出生Boss。玩家致死显示Defeat，Restart使`SessionGeneration=2`并创建不同会话，MainMenu按钮返回正式入口。
- Unity Editor人工窗口点击：**BLOCKED**；主工程Editor未连接MCP且Computer Use不可用。未用自动化按钮或截图冒充人工PASS。用户复核步骤：打开Bootstrap，Play，点击“开始游戏”→“幽菌古道”，划线命中首个敌人，再确认HUD/教程/结算/重开。
- 标准Web：NOT RUN（T660明确不做；既有T100基线不能替代本次入口构建）。
- 微信转换：NOT RUN（按用户延期决定）。
- DevTools：BLOCKED（既有T120缺工具）。
- 真机：BLOCKED（既有T120缺DevTools和设备）。
- 截图/日志/产物：`production-entry-1920x1080.png`为1920×1080 Metal实际渲染，2,799,305字节，SHA-256 `5077c53c4af436934344a75c4faca60a850fe354396f9efe8f8d988064e1a0cf`；已目检标题“一笔镇妖”、选择关卡与三关按钮完整、无replacement glyph或裁切。工作簿交付为`Design/Config/GameConfig.xlsx`（95,553字节，SHA-256 `9f2330c94bc86f67563feba816cd2b8acb3f05cbd2f2f5811431ee891a4233ee`）。

## 结论

- 已知问题：人工Unity窗口点击门待用户确认；T130平台存储、T640多比例/安全区、标准Web、微信DevTools/真机仍在各自任务边界。生产世界当前复用既有策略/反馈能力，不在T660扩展新的投射物视觉或敌人修饰规则。
- 结论：**REVIEW**。实现、配置、字体、场景、自动化玩家路径、Metal截图和全量回归均PASS；仅人工Editor点击未满足，因此T660不标DONE、T700不启动。
