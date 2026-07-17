# T678 Change Whitelist

- Git基线：`448a2e2f005fe027c54d9b3500eae430da8cea74`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Bootstrap目录6个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Bootstrap/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T678/**`

## 禁止改动

- 其他模块、配置、测试、场景、Prefab和资源。

## 收尾

- [x] Bootstrap脚本新增仅为注释且删除0行。
- [x] T660/ConfigPipeline专项、全量、配置漂移和白名单通过。
