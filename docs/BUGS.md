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
