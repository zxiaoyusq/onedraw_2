# T677 Change Whitelist

- Git基线：`dfb83c8c8bb814641bc769e58100d4fc18d005cb`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Presentation目录17个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Presentation/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T677/**`

## 禁止改动

- 其他模块、配置、测试、场景、Prefab、字体和美术资源。

## 收尾

- [x] Presentation脚本新增仅为注释且删除0行。
- [x] 表现专项、全量、配置漂移和白名单通过。
