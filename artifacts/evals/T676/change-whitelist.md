# T676 Change Whitelist

- Git基线：`3d25e8077b5fcc12d5ca106c741500f7788b05b3`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Levels目录14个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Levels/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T676/**`

## 禁止改动

- 其他模块、配置、测试、场景、Prefab和资源。

## 收尾

- [x] Levels脚本新增仅为注释且删除0行。
- [x] T500–T550专项、全量、配置漂移和白名单通过。
