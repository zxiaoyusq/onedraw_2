# T120 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T120；基于T110固定的embedded WXSDK，分别验证G2微信转换、G3开发者工具与至少一台G4真机。
- 明确不做：不把G2外推为G3/G4；不写入或提交AppID/AppSecret；不升级Unity/SDK；不修改SDK源码、Gameplay、场景、Prefab或配置；不开始T130。
- 分支/提交：`main`；任务基线`3cc548cfb287bb3dd2922824642ca1cb2a5445b4`（`T110: pin official WeChat SDK`）。用户明确决定延期平台门后，本任务以`T120: checkpoint blocked WeChat platform spike`形成单一可回滚检查点；任务状态仍为`BLOCKED`，提交不表示G3/G4完成。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`；除新建证据目录外工作树干净，无需覆盖的用户改动。
- Unity精确版本：`6000.5.1f1`；Unity Editor/MCP完成最终编译与专项测试，批处理完成G2及全量回归。
- 微信SDK：官方`minigame-tuanjie-transform-sdk` v0.1.33 / `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` / T110 embedded单点补丁。
- 配置Schema/内容版本/hash：schema `1` / content `0.1.0-sample` / `ed8ab5789586c1e6b5c82b9f3185052f408cd61b53126cd8ec81b917c738756a`；T120未改配置。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：项目Editor程序集增加`WxEditor`引用；新增项目自有微信构建入口、保护性命令封装、3个EditMode策略测试及Unity `.meta`；同步平台/上游/问题/决策/任务/进度/索引文档和T120证据。
- 用户已有改动保护：基线无用户改动；构建封装备份并恢复`ProjectSettings`、包内MiniGame配置/`.meta`、URP资产和SDK生成的受管Assets路径。目标输出目录按构建语义由本次结果替换。
- SDK/设置边界：embedded SDK源码、`ProjectSettings`及URP资产最终均无受管差异；`Builds/`、Unity缓存、私有配置与凭据未纳入版本控制。
- `git diff --check`：PASS；非原始日志的未跟踪文件也通过空白符检查。原始Unity日志保留工具输出中的尾随空格以维持原始性和已记录SHA-256，不做清洗。
- 白名单审查：PASS；`git status --short`每一项均属于预计白名单，SDK源码、`ProjectSettings`和URP路径无差异，敏感值模式扫描通过。只将本白名单作为T120阻塞检查点提交。

## 实现与自动验证

- 可复现命令：`Tools/CI/build-wechat.sh --development --log artifacts/tmp/T120-wechat-unity.log`。
- 脚本校验：`bash -n`与`--help`通过；恢复受保护设置失败时会保留备份并以非零退出，不能误报转换成功。
- 构建策略：输出限制在`Builds/WeChat/**`；Bootstrap首场景；空AppID；横屏；256MB；触摸启用；Development；渲染线程关闭；性能分析关闭；清理构建；多线程Brotli开启。
- 初始编译：测试直接暴露SDK预编译类型导致编译失败，原始错误见`initial-compile-errors.log`；改为项目自有`WechatBuildPolicy`后编译通过，未扩大SDK补丁。
- G2尝试：第一次被会话中断；第二次因GUI Editor锁主动拒绝；第三次暴露SDK默认单线程Brotli路径问题；第四次使用SDK公开的多线程Brotli配置成功。逐次证据见`g2-attempts-summary.log`及对应原始日志。
- G2结构校验：84个文件、总计101,901,218字节；WebGL中间产物89,892,698字节；`minigame` 12,008,520字节；2/2 JSON可解析；关键文件齐全；空AppID、横屏与敏感占位扫描通过。
- G2完整性：`g2-output-manifest.sha256`含84项并通过`shasum -c`；清单文件SHA-256为`ccb494d55c022c561685d176ff7be5edf9ec1435126aebe61feb0d2f9b93fc32`；成功原始日志SHA-256为`8837b2048454a0b0f26d97b19f854a302e1947d30ab6b91c224af85f5eb17507`。
- EditMode XML：13 / 13 / 0，`editmode-results.xml`；T120专项3/3包含在内。
- PlayMode XML：2 / 2 / 0，`playmode-results.xml`；Bootstrap→MainMenu路径通过。
- 最终编译/测试：无致命编译或测试错误。SDK仍有6类已知编译warning（BUG-0004）。成功转换日志另含93条`UnMatched WXReplaceRules rule`、6条Emscripten warning（BUG-0006）及3条Unity许可证握手/访问令牌错误文本；后者未阻止有许可证的Player构建、转换成功标记或进程返回0，仍保留原始日志供审查。

## 玩家与平台证据

- 真实Unity玩家路径：PlayMode实际加载Bootstrap并进入MainMenu，2/2通过；本任务没有伪造DevTools或手机玩家路径。
- G1标准Web：`PASS WITH KNOWN ISSUES`，继承T100证据，不在T120重复构建结论。
- G2微信转换：`PASS WITH KNOWN ISSUES`。`[Builder] Done`、`[Converter] All done!`、`WECHAT_CONVERSION_PASS`均出现在成功原始日志；证明范围仅为固定SDK生成结构完整的小游戏工程。
- G3开发者工具：`BLOCKED_MISSING_DEVTOOLS`。文件系统、Spotlight bundle id、常见CLI路径及Computer Use应用清单均未发现微信开发者工具；见`g3-devtools-probe.log`。
- G4真机：`BLOCKED_MISSING_DEVICE_AND_G3`。Unity内置ADB设备列表为空，本机无可用iOS设备工具，USB探测未发现目标手机；且缺少G3预览/二维码能力；见`g4-device-probe.log`。
- 截图：无。缺少对应应用和设备时未伪造截图。
- 主要日志/产物：`g2-conversion-unity.log`、`g2-output-manifest.sha256`、`g2-summary.log`、`g3-devtools-probe.log`、`g4-device-probe.log`、`editmode-results.xml`、`playmode-results.xml`、`warnings-summary.log`。

## 已知问题与解除阻塞

- BUG-0004：固定SDK在Unity 6000.5.1f1仍产生6类非阻断编译warning。
- BUG-0005：SDK默认单线程Brotli在当前macOS Unity布局引用不存在的`.app/PlaybackEngines`路径；使用SDK支持的多线程Brotli设置规避，未修改SDK源码。
- BUG-0006：转换出现93条未匹配替换规则与6条Emscripten warning；必须通过G3实际启动判断影响。
- 用户/外部动作：安装并登录官方微信开发者工具，导入`Builds/WeChat/T120/minigame`。空AppID若被拒绝，仅在本机选择无AppID模式或填写测试AppID，不提交私有配置。
- 用户/外部动作：连接至少一台可运行微信的手机，通过开发者工具预览执行单指触摸、首次交互音频、前后台切换和版本化存储读写。
- 执行顺序：用户已明确要求暂时绕过T120及微信工具/打包问题，优先推进主要游戏内容；T130保持`BACKLOG`，T200成为唯一`READY`任务。该决定见D-011。

## 结论

- G2结论：`PASS WITH KNOWN ISSUES`。
- T120结论：`BLOCKED`。G3/G4没有可用工具和设备，不能写PASS；按D-011形成可回滚阻塞检查点并转入T200，发布前仍必须恢复平台门。
