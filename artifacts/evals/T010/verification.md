# T010 Verification

- 日期：2026-07-11
- Git基线：`21cd33e1 T000 establish project contracts`，分支 `main`
- 计划提交：`T010: validate Unity project baseline`
- 范围：建立根 `.gitignore`，纳管现有Unity 2D工程和剩余开发合同；未实现业务代码，未创建后续模块目录或正式场景，未安装微信SDK。
- Unity版本：`6000.5.1f1 (0d9463e84828)`，与 `ProjectSettings/ProjectVersion.txt` 一致。
- Git根：仓库内仅 `/Users/cqmizhangxiaoyu2/dev/u3d/onedraw_2/.git`；无嵌套仓库。
- 忽略规则：`.DS_Store`、Library、Logs、Temp、UserSettings、`.sln/.csproj` 等生成物均由根 `.gitignore` 命中；配置生成JSON未被忽略。
- 场景：`Assets/Scenes/SampleScene.unity` 已在 `ProjectSettings/EditorBuildSettings.asset` 启用；基线对象包含Main Camera与Global Light 2D。
- 编译：Unity启动完成Initial Refresh，未出现 `error CS` 或 `Compilation failed`。
- EditMode：未执行；T010没有纯规则代码或专项EditMode测试。
- PlayMode：PASS。一次性Editor探针让SampleScene进入Play Mode并持续2秒后退出，记录 `T010_PLAYMODE_SMOKE_PASS ... errors=0`。
- Console：探针在开始前清空Console，并统计本次 `Error / Exception / Assert`，结果为0。
- 清理：临时 `Assets/Editor/T010SmokeProbe.cs`、对应meta、临时目录及MCP自动启动Editor偏好均已移除，未进入Git。
- MCP：服务端可访问，但实例桥接未自动重连；未据此伪造MCP active instance。真实Play Mode由Unity Editor内一次性探针执行。
- 包锁定：Unity MCP manifest引用 `#main`，`packages-lock.json` 当前解析hash为 `11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8`；显式包基线留给T020。
- 证据：`artifacts/evals/T010/playmode-smoke.log`
- 已知问题：后续需要Unity场景操作前必须恢复MCP实例桥接；微信平台门均未执行。
- 结论：PASS。T010完成，T020置为READY；未开始T020。
