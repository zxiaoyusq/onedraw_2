# T670 Change Whitelist

- Git基线：`e99c4d2bbfcac6f91548849e09d2eb9113d1da98`
- 需要保护的用户已有改动：`AGENTS.md`中新增的“代码注释规范”；本任务不修改、不暂存该文件。
- 任务目标：为`Assets/_Game/Scripts/Core`与`Assets/_Game/Scripts/Platform`的手写C#脚本补齐易懂的中文类型、方法和主要逻辑注释，不改变运行语义。
- 明确不做：不修改玩法、配置、测试逻辑、Unity场景/Prefab/YAML；不手工修改`ConfigIds.g.cs`；不提前处理T671及后续模块。

## 预计改动白名单

- `Assets/_Game/Scripts/Core/*.cs`：仅新增中文注释。
- `Assets/_Game/Scripts/Platform/**/*.cs`：仅新增中文注释。
- `docs/TASKS.md`：记录T660验收、注释任务拆分与T670状态。
- `docs/PROGRESS.md`：记录当前任务、结果和下一步。
- `project-index.yaml`：同步当前任务与注释进度。
- `artifacts/evals/T670/**`：Git基线、白名单、验证记录、测试XML与日志。

## 禁止改动

- `AGENTS.md`及其他用户改动。
- 不在本任务范围内的脚本、配置、资源、场景、包、构建产物和外部状态。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单，或是明确保护且不暂存的用户`AGENTS.md`改动。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
