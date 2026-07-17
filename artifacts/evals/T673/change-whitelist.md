# T673 Change Whitelist

- Git基线：`d60f56f4c1a39e207cb61236920234d7a670a824`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Combat目录25个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Combat/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T673/**`

## 禁止改动

- 其他脚本模块、配置、测试、场景、Prefab、资源、Packages和ProjectSettings。

## 收尾

- [x] Combat脚本新增仅为注释且删除0行。
- [x] 专项、全量、配置漂移、diff和暂存白名单通过。
