# T675 Change Whitelist

- Git基线：`35ec70c9bb57061cc66065e3db8ce91a548ac78e`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Skills目录9个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Skills/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T675/**`

## 禁止改动

- 其他模块、配置、测试、场景、Prefab和资源。

## 收尾

- [x] Skills脚本新增仅为注释且删除0行。
- [x] T410专项、全量、配置漂移和白名单通过。
