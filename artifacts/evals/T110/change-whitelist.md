# T110 Change Whitelist

- Git基线：`6e4fe22a40da7ed3883c68fa0e2587cbae8a9326`（`main`，任务开始时除本证据目录外工作树干净）。
- 需要保护的用户已有改动：无；若后续出现白名单外改动，先判定来源并保留，不覆盖。
- 任务目标：确认当前官方微信 Unity 转换 SDK 来源，固定版本/commit/许可证，导入 Unity 6000.5.1f1 并建立兼容矩阵与编译证据。
- 明确不做：不执行微信转换、开发者工具或真机冒烟；不使用浮动分支；不迁移 Unity；除原始编译失败时白名单允许的单点兼容补丁外，不修改 SDK 上游代码。

## 预计改动白名单

- `.gitattributes`：仅对完整嵌入的官方 SDK vendor 目录关闭 whitespace 报错；保留上游原始字节与校验，不放宽项目自有文件检查。
- `Packages/manifest.json`、`Packages/packages-lock.json`：以完整 commit 固定官方 UPM Git 依赖及 Unity 解析结果。
- `Packages/com.qq.weixin.minigame/**`：仅当 Unity 6000.5.1f1 原始编译失败时，由 Unity Package Manager embedded 固定上游完整快照，并对 `Runtime/WXRuntimeExtDef.cs` 应用带版本条件的单点兼容补丁；禁止其他上游改动。
- `Assets/_Game/Tests/EditMode/T110/**`：新增 SDK 固定来源/版本与集成边界专项测试及 Unity `.meta`。
- `docs/UPSTREAM.md`：记录官方来源、commit、包版本、许可证、校验与升级/补丁策略。
- `docs/WECHAT_SDK_COMPATIBILITY.md`：记录 Unity/SDK 兼容矩阵、编译结论与 T120 前置条件。
- `docs/PLATFORM_WECHAT.md`、`docs/DECISIONS.md`：同步平台方案及固定依赖决策。
- `docs/BUGS.md`：登记 SDK 在 Unity 6000.5 下仍存在的非阻断弃用/未使用字段编译 warning。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`、`PACKAGE_VALIDATION.md`：同步任务状态、当前 SDK 真相和下一任务。
- `artifacts/evals/T110/**`：Git 基线、来源快照/哈希、Unity 测试 XML、编译日志摘要、白名单和最终验证记录。
- Unity 导入时若自动生成其他受版本控制文件：仅在确认属于 SDK 导入的必要结果后补充本白名单；否则通过 Unity Editor/API 恢复。

## 禁止改动

- 不修改 Gameplay、场景、Prefab、配置 Excel/JSON、正式资源或 T100 构建产物。
- 不执行 G2/G3/G4，不写 AppID/AppSecret，不安装或操作微信开发者工具。
- 不手工编辑 Unity YAML，不提交 `Library/`、下载缓存或完整外部仓库。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
