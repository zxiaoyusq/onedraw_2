# Unity / 微信小游戏转换 SDK 兼容矩阵

核验日期：2026-07-13

## 结论口径

本矩阵把“上游声明”“本工程导入编译”“微信转换”“开发者工具”“真机”分开。T110只对来源、固定依赖与Unity导入编译下结论；T120已完成G2，但G3/G4因本机缺少工具和设备而阻塞。

## 上游矩阵

| Unity/引擎族 | 上游证据 | T110结论 |
|---|---|---|
| Unity 2019.4+ | UPM `package.json` 最低字段为 `2019.4.29f1`；该字段明显未随发布线维护 | 仅表示包管理器最低门槛，不等同运行兼容承诺 |
| Unity 2021 / 2022 | v0.1.29 变更记录明确写 EmscriptenGLX 兼容 Unity 2021/2022/团结 | 上游明确覆盖部分转换路径 |
| Unity 6（泛版本） | 2025-01-07 变更记录写“支持Unity6，仅作为测试版本不建议上线使用”；v0.1.33 源码含 `UNITY_6000`、`UNITY_6000_0_OR_NEWER` 分支 | 可做 Spike，不能据此宣称生产或真机兼容 |
| 团结引擎 1.6+ | v0.1.31～v0.1.33 持续修复 BuildProfile 与团结引擎导出 | 与本项目的国际版 Unity 6000.5.1f1 不是同一验证对象 |

## 本工程精确矩阵

| 层级 | Unity | SDK | 状态 | 证明范围 |
|---|---|---|---|---|
| 官方来源/许可证 | 6000.5.1f1 | v0.1.33 / `ed4ad28f...` | PASS | 官方仓库、固定 commit、MIT 可追溯 |
| UPM 解析 | 6000.5.1f1 | 同上 | PASS | manifest 固定上游 commit；lockfile 固定 embedded 快照 |
| 未修改上游全工程编译 | 6000.5.1f1 | 同上 | FAIL | `Object.GetInstanceID()` 触发 CS0619；原始错误已保存 |
| 最小补丁后全工程编译 | 6000.5.1f1 | 同上 | PASS | embedded 单点条件补丁后 Runtime/Editor 与现有程序集共同编译；Console Error 0 |
| EditMode | 6000.5.1f1 | 同上 | PASS 13/13 | T120构建策略专项3/3及全量固定依赖、包内容、程序集与现有规则回归 |
| PlayMode | 6000.5.1f1 | 同上 | PASS 2/2 | 现有 Bootstrap→MainMenu 玩家路径通过 |
| G2 微信转换 | 6000.5.1f1 | 同上 | PASS WITH KNOWN ISSUES | 生成84个文件、101,901,218字节；结构/hash/敏感占位检查通过；见BUG-0005/BUG-0006 |
| G3 开发者工具 | — | — | BLOCKED | 本机未安装微信开发者工具，不能验证启动、交互或Console |
| G4 真机 | — | — | BLOCKED | 未发现已连接手机，且缺少G3预览/二维码能力 |

## 风险与 T120 解除阻塞条件

- 上游 `package.json` 版本号滞后为 `0.1.1`，自动化必须同时核对完整 commit 与 `CHANGELOG v0.1.33`。
- Unity 6 的上游公开说明仍带“测试版本不建议上线”限定；即使 T110 编译通过，发布兼容性仍是 REVIEW。
- T120已保存转换原始日志、关键产物SHA-256与全量manifest；转换成功不能替代DevTools或真机结论。
- SDK 含6条非阻断编译 warning（弃用 API 及未使用字段），见 BUG-0004；本任务不扩大补丁范围。
- SDK默认单线程Brotli在当前macOS Unity安装布局下引用不存在的`.app/PlaybackEngines`路径；T120使用SDK支持的多线程Brotli成功规避，见BUG-0005，未新增SDK源码补丁。
- 转换产生93条未匹配WXReplaceRules规则及6条Emscripten warning，见BUG-0006；必须在G3实际启动后判断运行影响。
- 当前不迁移Unity；继续采用已记录的embedded单点补丁和固定快照。
- 解除阻塞需要安装并登录官方微信开发者工具，导入`Builds/WeChat/T120/minigame`完成G3，并连接至少一台可运行微信的手机完成G4触摸、音频、前后台和存储冒烟。

## T120 当前判定

`6000.5.1f1 + WXSDK v0.1.33/ed4ad28f`在带版本条件的T110 embedded单点补丁后，已达到“可导入、可编译、可转换且现有玩家路径不回归”的Spike基线。G2为`PASS WITH KNOWN ISSUES`；由于上游对Unity 6仍有测试版限定、BUG-0006尚待运行判定，且G3/G4没有真实执行条件，T120保持`BLOCKED`，不能写为微信平台PASS。
