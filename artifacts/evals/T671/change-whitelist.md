# T671 Change Whitelist

- Git基线：`934fc2181b58ca03dfff3472f0101b6e22b0e271`
- 需要保护的用户已有改动：`AGENTS.md`中的中文注释规范；本任务不修改、不暂存该文件。
- 任务目标：为`Assets/_Game/Scripts/Config`下22个手写Runtime C#脚本补齐易懂的中文类型、方法、属性与主要逻辑注释，不改变运行语义。
- 明确不做：不修改工作簿、Schema、JSON/hash、配置数值、测试逻辑或Unity资源；不手工修改`Generated/ConfigIds.g.cs`；不提前处理T672。

## 预计改动白名单

- `Assets/_Game/Scripts/Config/*.cs`：仅新增中文注释，不包含`Generated/`。
- `docs/TASKS.md`：同步T671/T672状态与任务结果。
- `docs/PROGRESS.md`：记录T671结果与下一步。
- `project-index.yaml`：同步中文注释覆盖进度。
- `artifacts/evals/T671/**`：Git基线、白名单、验证记录、配置校验和Unity测试证据。

## 禁止改动

- `AGENTS.md`及其他用户改动。
- `Assets/_Game/Scripts/Config/Generated/**`与其他非Config Runtime模块。
- 不在本任务范围内的配置、资源、场景、包、构建产物和外部状态。

## 收尾审查

- [x] `git status --short`中的任务改动均属于白名单；唯一白名单外项目为受保护且不暂存的用户`AGENTS.md`改动。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
