# T674 Change Whitelist

- Git基线：`e41e6ad85a5dca544f2c4086e7edf656360840a6`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：为Actors目录19个手写C#脚本补齐中文类型、方法和主要逻辑注释，只增加注释。

## 白名单

- `Assets/_Game/Scripts/Actors/*.cs`
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T674/**`

## 禁止改动

- 其他脚本模块、配置、测试、场景、Prefab、资源、Packages和ProjectSettings。

## 收尾

- [x] Actors脚本新增仅为注释且删除0行。
- [x] 专项、全量、配置漂移、diff和暂存白名单通过。
