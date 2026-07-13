# T300 Change Whitelist

- Git基线：`fadccbf3f722bc6260dbe85b8950406d886b0bf3`（`main`；任务开始工作树干净）。
- 需要保护的用户已有改动：无；若后续出现白名单外差异，保留并先判定来源，不覆盖。
- 任务目标：实现只消费Input System的单指统一输入，UI起笔阻挡，动态Safe Area到配置参考像素的确定性换算，以及失焦、暂停、禁用和设备断开取消；由Bootstrap使用配置中的参考宽高初始化可跨场景服务。
- 明确不做：不实现T310采样/长度裁剪、笔势识别、轨迹视觉、命中或战斗；不支持多指战斗；不依赖`Screen.dpi`；不修改策划配置、场景、Prefab或微信平台/打包状态。

## 预计改动白名单

- `Assets/_Game/Scripts/Input/**`及Unity生成`.meta`：实现`IPointerInput`事件合同、纯状态处理、`ReferencePixelConverter`、Safe Area/UI查询、`InputSystemPointerAdapter`和跨场景Runtime；仅允许引擎引用与运行态坐标/生命周期逻辑，不含玩法阈值或采样算法。
- `Assets/_Game/Scripts/Bootstrap/BootstrapController.cs`：从已验证配置读取`reference_width/reference_height`并初始化输入Runtime，失败阻断场景推进；不得写入第二套默认参考分辨率。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅增加Input/UI测试所需程序集引用。
- `Assets/_Game/Tests/EditMode/T300/**`、`Assets/_Game/Tests/PlayMode/T300/**`及目录`.meta`：新增参考坐标、Safe Area、UI起笔、单活动指针、鼠标/触摸统一接口及生命周期取消测试。
- `docs/TECH_SPEC.md`、`docs/WORKFLOW.md`、`docs/DECISIONS.md`：必要时同步T300输入坐标/生命周期合同，不扩展T310以后范围。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T300状态、验证摘要和下一任务T310。
- `artifacts/evals/T300/**`：保存基线、白名单、专项/全量Unity测试、真实输入路径、Console和最终验证摘要。
- `artifacts/tmp/T300/**`：忽略的Unity XML与原始日志，不提交。

## 禁止改动

- 不修改`Design/Config/GameConfig.xlsx`、模板xlsx、Schema、JSON/hash/ConfigIds生成物、Packages、ProjectSettings、Input Actions、`.unity/.prefab/.asset`、Registry、美术、微信SDK/构建或平台外部状态。
- 不手工创建`.meta`或Unity YAML；不提交`Library/Logs/Temp`与原始日志；不把参考分辨率、Safe Area或UI边界变成Inspector数值库。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
