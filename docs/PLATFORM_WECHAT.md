# PLATFORM_WECHAT：微信小游戏工程计划

## 1. 版本原则

- 当前工程固定使用Unity `6000.5.1f1`；T110必须以该精确版本验证微信转换方案，未经决策不得迁移版本。
- 当前官方来源固定为 `wechat-miniprogram/minigame-tuanjie-transform-sdk`，发布线 `v0.1.33`，commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`；许可证及校验见 `docs/UPSTREAM.md`。
- manifest 保留完整 commit，实际使用 Package Manager embedded 快照。Unity 6000.5 的唯一逻辑补丁位于 `Runtime/WXRuntimeExtDef.cs`，带 `UNITY_6000_5_OR_NEWER` 条件和移除条件。
- 不引用浮动分支。平台包本地补丁必须embedded、最小化、带版本条件、`UPSTREAM.md`和移除条件；本项目不得新增未记录的 SDK 差异。
- 旧的 `wechat-miniprogram/minigame-unity-webgl-transform` GitHub仓库仍被禁用，不作为安装源。

### 当前 SDK 导入基线（T110）

- 未修改官方 commit：Unity 6000.5.1f1 编译 `FAIL`，根因为 `Object.GetInstanceID()` 的 CS0619。
- embedded 单点补丁后：全工程编译 PASS；EditMode 10/10、PlayMode 2/2；Console Error/Exception 0。
- SDK仍产生6条非阻断编译 warning，见 BUG-0004。
- 以上只证明依赖与导入兼容；不证明转换、DevTools或真机。

## 2. 四级平台门

| Gate | 证明什么 | 不能证明什么 |
|---|---|---|
| G1 Unity Web Build | Unity工程能编译为Web | 微信转换或运行 |
| G2 微信转换 | 转换工具能生成小游戏项目 | DevTools或真机可运行 |
| G3 开发者工具 | 模拟环境能启动、操作、读日志 | 真实手机性能和生命周期 |
| G4 真机 | 目标设备实际可玩 | 所有设备均无问题 |

每一级单独记录PASS、REVIEW、BLOCKED或KNOWN ISSUE。

### 当前门状态（2026-07-13）

| Gate | 状态 | 证据 |
|---|---|---|
| G1 Unity Web Build | PASS WITH KNOWN ISSUES | `docs/WEB_BUILD_BASELINE.md`、`artifacts/evals/T100/` |
| G2 微信转换 | PASS WITH KNOWN ISSUES | `artifacts/evals/T120/g2-conversion-unity.log`、`g2-output-manifest.sha256`；见BUG-0005/BUG-0006 |
| G3 开发者工具 | BLOCKED | 本机未安装微信开发者工具；见`artifacts/evals/T120/g3-devtools-probe.log` |
| G4 真机 | BLOCKED | 未发现已连接手机，且依赖G3预览能力；见`artifacts/evals/T120/g4-device-probe.log` |

### T120 G2 可复现转换

```bash
Tools/CI/build-wechat.sh --development --log artifacts/tmp/T120-wechat-unity.log
```

- 输入固定为Unity `6000.5.1f1`、WXSDK `v0.1.33` / `ed4ad28f...`和Build Settings中启用的场景；Bootstrap必须为首场景。
- Spike参数固定为空AppID、横屏、256MB、触摸启用、Development、关闭渲染线程、关闭性能分析、清理构建；使用SDK公开的多线程Brotli设置，规避BUG-0005。
- 输出只允许位于`Builds/WeChat/**`，T120默认路径为`Builds/WeChat/T120`。本次生成84个文件、101,901,218字节；`minigame`为12,008,520字节。
- 构建封装会在退出时恢复`ProjectSettings`、SDK配置和SDK生成的受管Assets路径；本次验证后这些路径无受管残留差异。目标输出目录会被本次结果替换。
- `PASS WITH KNOWN ISSUES`只证明固定SDK生成了结构完整的小游戏工程。93条未匹配替换规则与6条Emscripten warning已登记为BUG-0006，必须通过G3实际启动判断影响。

### 解除当前阻塞

1. 安装并登录官方微信开发者工具，导入`Builds/WeChat/T120/minigame`，保存启动结果、Console日志与截图。若空AppID被拒绝，只在本机选择无AppID测试模式或填写测试AppID，不提交私有配置。
2. 连接至少一台可运行微信的手机，通过预览/二维码执行单指触摸、首次交互音频、前后台切换和版本化存储读写，并保存设备/工具版本及日志。
3. G3和G4均取得真实证据后才能把T120从`BLOCKED`推进；转换成功不能代替这两级门。

### 执行顺序延期

- 2026-07-13用户明确要求暂时绕过T120以及微信开发者工具、真机和打包问题，优先完成游戏主要内容。T120的G3/G4状态仍为`BLOCKED`，T130保持`BACKLOG`，当前主内容任务切换为T200。
- 延期只改变执行顺序，不改变MVP完成标准，也不把任何未执行平台门写为PASS。平台链最迟在依赖T120的T640或发布验收T750前恢复。

## 3. 最小平台Spike内容

- URP 2D Sprite和TMP中文。
- 单指按下、移动、抬起、取消，并显示采样点数。
- 首次用户交互后播放音频，验证前后台恢复。
- 写入和读取一个版本化JSON存档。
- onHide/onShow或对应Unity回调触发暂停和取消当前笔迹。
- Unity、转换层和平台API日志使用不同前缀。
- 记录首屏、峰值内存、帧率、构建大小；以当次工具输出为准。

## 4. 平台抽象

```csharp
public interface IPlatformService
{
    string PlatformName { get; }
    void Vibrate(VibrationPattern pattern);
    bool TryLoad(string key, out string value);
    bool TrySave(string key, string value);
    event Action Paused;
    event Action Resumed;
    void LogEvent(string name, IReadOnlyDictionary<string, string> fields);
}
```

MVP不接广告、支付、登录、分享和排行榜。后续以新任务扩展接口，不能让Gameplay直接调用SDK。

## 5. 发布候选真机矩阵

- 至少一台低端Android、一台主流Android、一台iPhone。
- 首次加载、第二次加载、弱网和断网启动行为。
- 单指快速连续划动、屏幕边缘、刘海、圆角和左右手布局。
- 来电、锁屏、切后台、返回、音频恢复和当前笔迹取消。
- 三关完整流程、失败重试三次、保存后重启。
- 十分钟压力战斗，无持续内存增长和严重掉帧。
