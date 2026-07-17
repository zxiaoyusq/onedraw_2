# T679 Change Whitelist

- Git基线：`f6faa3697dc19850c49cb3f97d60de6e8adaa350`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Editor目录及其子目录9个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Editor/**/*.cs`及目录根C#脚本
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T679/**`

## 禁止改动

- 其他模块、配置、测试、场景、Prefab、生成资源与构建产物。

## 收尾

- [x] Editor脚本新增仅为注释且删除0行。
- [x] Editor相关专项、全量、配置漂移和白名单通过。
