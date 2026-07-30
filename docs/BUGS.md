# BUGS

## BUG-0001 · WebGL不支持URP Edge Adaptive Spatial Upsampling shader

- 状态：OPEN
- 严重度：S2
- 发现版本或commit：Unity `6000.5.1f1` / T100工作树
- 配置hash：不适用
- 环境：标准Unity WebGL，本地HTTP，Codex In-app Browser
- 复现步骤：以T100 Smoke配置构建WebGL；本地HTTP打开并等待MainMenu。
- 期望：URP灰盒启动且浏览器Console无渲染能力warning。
- 实际：画面正常启动，但Console报告Edge Adaptive Spatial Upsampling shader不支持，相关后处理pass不会执行。
- 可证伪假设：当前MainMenu灰盒不依赖后处理，所以未影响本次G1可玩路径；加入依赖该pass的后处理后可能出现视觉差异。
- 最小修复范围：在后续视觉/平台任务中确认Web质量档的upscaling过滤策略，避免为Web选择不支持的FSR/EASU路径。
- 回归测试：标准Web构建后Console warning扫描与视觉截图。
- 证据：`artifacts/evals/T100/browser-smoke.json`、`warnings-summary.log`。

## BUG-0002 · Web PlayerPrefs手动同步API提示未来弃用

- 状态：OPEN
- 严重度：S3
- 发现版本或commit：Unity `6000.5.1f1` / T100工作树
- 配置hash：不适用
- 环境：标准Unity WebGL，本地HTTP
- 复现步骤：T100探针调用`PlayerPrefs.Save()`，重载页面。
- 期望：版本化JSON持久化且无弃用warning。
- 实际：持久化成功，重载计数1→2；Console提示手动`JS_FileSystem_Sync()`未来会移除，并建议`autoSyncPersistentDataPath=true`。
- 可证伪假设：warning不影响当前Unity版本，但未来升级可能移除手动同步。
- 最小修复范围：T130 Web平台服务与Web模板启用自动persistentDataPath同步，移除对手动同步的核心依赖。
- 回归测试：保存、重载、第二次加载和Console warning扫描。
- 证据：`artifacts/evals/T100/browser-smoke.json`、`warnings-summary.log`。

## BUG-0003 · 长Web构建后Unity MCP实例桥接未自动恢复

- 状态：OPEN
- 严重度：S3
- 发现版本或commit：Unity `6000.5.1f1` / Unity MCP commit `11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8`
- 配置hash：不适用
- 环境：macOS Editor，T100首次WebGL构建后
- 复现步骤：完成约9分钟WebGL batch构建；退出并重新打开本工程Editor；查询`mcpforunity://instances`。
- 期望：Editor实例自动注册并可读取Console。
- 实际：Editor正常打开但instance_count保持0；最终验证只能使用batch XML、Editor.log过滤和浏览器Console。
- 可证伪假设：问题位于MCP自动启动/桥接生命周期，不影响Unity编译、测试或Web运行。
- 最小修复范围：后续任务开始时恢复MCP自动启动设置并核验单实例，必要时升级固定commit前先做兼容验证。
- 回归测试：Editor重启三次后instances均为1，active instance可读Console。
- 证据：`artifacts/evals/T100/verification.md`。

## BUG-0004 · WXSDK在Unity 6000.5仍产生弃用API编译warning

- 状态：OPEN
- 严重度：S3
- 发现版本或commit：Unity `6000.5.1f1` / WXSDK `v0.1.33` commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`
- 配置hash：不适用
- 环境：macOS Editor，embedded SDK最小补丁后全量EditMode编译
- 复现步骤：导入固定SDK并运行全量EditMode。
- 期望：SDK在Unity 6000.5无编译warning。
- 实际：编译通过，但报告6个唯一warning：`GraphicsDeviceType.OpenGLES2`弃用，`PlayerSettings.Get/SetScriptingDefineSymbolsForGroup`共4处弃用，以及1个未使用字段；批处理日志中的第二组为同次重编译重复输出。
- 可证伪假设：这些 API 在6000.5仍可调用，当前不阻断导入和现有测试；未来 Unity 删除 API 时会升级为编译错误。
- 最小修复范围：等待官方SDK升级；若后续升级为阻断错误，再按 `docs/UPSTREAM.md` 的embedded补丁流程逐项处理。T110不扩大上游补丁。
- 回归测试：SDKImportCompile、全量EditMode warning扫描。
- 证据：`artifacts/evals/T110/warnings-summary.log`、`artifacts/tmp/T110-editmode-unity.log`。

## BUG-0005 · WXSDK单线程Brotli路径不兼容macOS Unity 6000.5安装布局

- 状态：OPEN
- 严重度：S2
- 发现版本或commit：Unity `6000.5.1f1` / WXSDK `v0.1.33` commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`
- 配置hash：不适用
- 环境：macOS batchmode，T120 G2转换
- 复现步骤：使用SDK默认 `brotliMT=false` 完成WebGL Player构建和框架转换。
- 期望：SDK找到Unity或随包Brotli并生成小游戏 `.br` 文件。
- 实际：SDK尝试执行不存在的 `Unity.app/PlaybackEngines/WebGLSupport/BuildTools/Brotli/macos/brotli`，随后因目标 `.br` 缺失抛出 `FileNotFoundException`；本机PlaybackEngines实际位于Unity Editor根目录，不在 `.app` 内。
- 可证伪假设：问题只影响SDK的单线程压缩路径；启用SDK公开配置 `brotliMT=true` 后，随包 `BrotliEnc` 成功生成相同职责的产物并完成G2。
- 最小修复范围：T120构建策略固定 `brotliMT=true`，不新增第二处SDK源码补丁；等待上游修复路径解析后重新验证默认路径。
- 回归测试：`WechatBuildEntryTests`断言多线程Brotli策略；完整 `build-wechat.sh` 断言 `.br` 和转换终态。
- 证据：`artifacts/evals/T120/g2-attempt-3-unity-brotli-path.log`、`g2-conversion-unity.log`、`g2-summary.log`。

## BUG-0006 · Unity 6000.5转换出现大量未匹配WXReplaceRules

- 状态：BLOCKED
- 严重度：S1
- 发现版本或commit：Unity `6000.5.1f1` / WXSDK `v0.1.33` commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`
- 配置hash：不适用
- 环境：macOS batchmode，T120 G2 Development转换
- 复现步骤：运行 `Tools/CI/build-wechat.sh --development`，检查框架适配日志。
- 期望：适用于当前Unity生成代码的替换规则全部命中，或对可选规则给出可机器区分的无害说明。
- 实际：SDK报告93条 `UnMatched WXReplaceRules rule`，另有Emscripten undefined `WX_SyncFunction_tnnt` 等warning；转换仍返回 `All done` 并生成完整结构。
- 可证伪假设：部分规则属于旧Unity或未启用功能，可能不影响当前灰盒；但在G3实际启动、输入、音频、存储和生命周期运行前不能认定无害。
- 最小修复范围：先在微信开发者工具复现并把首个运行失败映射到具体规则；若为真实不兼容，向固定上游版本反馈并只在独立任务中评估最小补丁或SDK升级。
- 回归测试：G3 DevTools Console、启动/交互冒烟，以及转换日志未匹配规则计数。
- 证据：`artifacts/evals/T120/g2-conversion-unity.log`、`warnings-summary.log`、`g3-devtools-probe.log`。

## BUG-0007 · Battle开发灰盒遮挡画面且轨迹只在松开后闪现

- 状态：FIXED
- 严重度：S1
- 发现版本或commit：Unity `6000.5.1f1` / `b73f55d4`
- 配置hash：`2c005061c9a4bf806afcc6d6c16e7504b2df8b4bbecfec6edcc262900cd1dfdc`
- 环境：macOS Editor，生产Bootstrap→MainMenu→Battle，Mac触控板/InputSystem Mouse路径
- 复现步骤：进入教学关，在非UI区域按住触控板拖动并观察画面中央和松开后的笔迹。
- 期望：轨迹随有效采样点连续增长，宽度来自当前架势配置，松开后短暂淡出。
- 实际：Battle场景的8×4开发对象`BattleGraybox`仍为active，其MeshRenderer形成中央白板；另一路径的LineRenderer继承参考根`(0.009835,0.009249,1)`且采样完成前不显示，松开后只出现一次粗白光。
- 可证伪假设：若通过Unity Editor把开发灰盒设为inactive，同时把轨迹渲染器置于单位变换根、单独转换参考坐标/宽度并在完成前发布有效采样点，白板应消失且能辨认拖动路径。
- 最小修复范围：Battle场景只变更`BattleGraybox`的active状态；代码只调整采样预览事件、轨迹View/Pool生命周期和生产组合根的渲染空间，不修改配置、Prefab或Input Actions。
- 回归测试：白板红测T660 PlayMode 3/4失败后绿测4/4；StrokeSampling 10/10、StrokeTrail 5/5、全量EditMode 198/198、PlayMode 50/50；主Editor冻结4点折线目检。
- 证据：`artifacts/evals/T660/review-diagnosis-2026-07-15.md`、`main-editor-visual-review-2026-07-15.txt`、`production-live-trail-review-2026-07-15.jpg`。

## BUG-0008 · Unity Editor资源时间检查产生FSTimeGet断言

- 状态：OPEN
- 严重度：S3
- 发现版本或commit：Unity `6000.5.1f1` / T660评审现场
- 配置hash：不适用
- 环境：macOS Editor，资源刷新与Play Mode切换
- 复现步骤：刷新工程脚本并进入/退出Play Mode，读取Editor Console。
- 期望：资源时间检查不产生红色Assert。
- 实际：Editor报告`currentFileSystemTime.ticks != 0 using check file Temp/FSTimeGet-*`；条目没有产品脚本文件、行号或堆栈，专项和全量测试仍通过。
- 可证伪假设：该条目来自Editor资源管线而非游戏脚本；如果后续出现产品堆栈、编译错误或运行异常，应重新归类而不能继续视为噪声。
- 最小修复范围：不在T660修改Unity内部文件系统逻辑；升级Editor或出现可定位产品堆栈时另开任务处理。
- 回归测试：每次主Editor刷新后分别扫描产品异常与无堆栈Editor Assert，不能只按红色计数判定产品失败。
- 证据：`artifacts/evals/T660/main-editor-visual-review-2026-07-15.txt`、`verification.md`。

## BUG-0009 · Windows自动换行转换使CI Shell脚本无法执行

- 状态：OPEN
- 严重度：S2
- 发现版本或commit：T699工作树 / Unity `6000.5.1f1`
- 配置hash：不适用
- 环境：Windows 11、Git `core.autocrlf=true`、PowerShell调用WSL Bash
- 复现步骤：在Windows检出当前仓库；运行`git ls-files --eol Tools/CI/run-unity-tests.sh Tools/CI/verify-config.sh`确认索引为LF而工作树为CRLF；执行`bash Tools/CI/run-unity-tests.sh --help`。
- 期望：仓库CI脚本在支持的Bash环境中可解析参数并显示帮助。
- 实际：Bash在第2行起报告`$'\r': command not found`、`pipefail\r`非法选项和语法错误；`.gitattributes`固定了C#/JSON/Markdown/YAML为LF，但没有覆盖`*.sh`。
- 可证伪假设：问题来自工作树换行转换而非脚本业务逻辑；在LF检出中相同索引内容应恢复执行。
- 最小修复范围：独立工程卫生任务在`.gitattributes`增加`*.sh text eol=lf`，重新规范化Shell脚本并在Windows Git Bash/WSL及既有macOS批处理环境验证，不在内容制作任务顺手改写脚本。
- 回归测试：Windows新检出后`git ls-files --eol`显示`w/lf`；`verify-config.sh --help`、`run-unity-tests.sh --help`和`test-harness-smoke.sh`通过。
- 证据：`artifacts/evals/T699/closeout.md`。

## 缺陷模板

### BUG-XXXX · 标题

- 状态：OPEN / FIXED / WONTFIX / BLOCKED
- 严重度：S0 / S1 / S2 / S3
- 发现版本或commit：
- 配置hash：
- 环境：Editor / Web / 微信DevTools / 真机型号
- 复现步骤：
- 期望：
- 实际：
- 可证伪假设：
- 最小修复范围：
- 回归测试：
- 证据：
