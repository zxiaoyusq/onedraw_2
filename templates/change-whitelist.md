# TASK-ID Change Whitelist

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
