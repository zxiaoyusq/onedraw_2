# T672 Change Whitelist

- Git基线：`f50f8f3c8533e44ea1d264e7378fb5861da34736`
- 保护用户已有改动：`AGENTS.md`；本任务不修改、不暂存。
- 目标：为`Assets/_Game/Scripts/Input`下19个手写C#脚本补齐中文类型、方法与主要逻辑注释，不改变输入、采样、几何或识别语义。

## 预计改动白名单

- `Assets/_Game/Scripts/Input/*.cs`：只新增中文注释。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T672/T673状态与结果。
- `artifacts/evals/T672/**`：基线、白名单、测试和验证证据。

## 禁止改动

- `AGENTS.md`、Input Actions、配置、场景、Prefab、Unity资源、Packages、ProjectSettings及其他脚本模块。

## 收尾审查

- [x] 工作区任务改动均属于白名单；用户`AGENTS.md`保持白名单外且不暂存。
- [x] Input脚本只有285行注释新增、删除0行。
- [x] `git diff --check`和暂存白名单审查通过。

- Git基线：
- 需要保护的用户已有改动：
- 任务目标：
- 明确不做：

## 预计改动白名单

- `path/or/glob`：修改原因与允许的变更类型。

## 禁止改动

- 不在本任务范围内的文件、资源和外部状态。

## 收尾审查

- [ ] `git status --short`中的每一项都属于白名单。
- [ ] `git diff --check`通过。
- [ ] 仅暂存白名单文件，并审查`git diff --cached`。
