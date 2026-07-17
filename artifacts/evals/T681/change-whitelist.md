# T681 Change Whitelist

- Git基线：`521360d719220d1f2c37a6c8e4e5504856daaca5`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：审计`Assets/_Game/Scripts`全量147个C#的中文类型、方法与主要逻辑注释覆盖，并完成最终回归。

## 白名单

- `Assets/_Game/Scripts/**/*.cs`仅允许审计发现的遗漏注释补齐；禁止语义改动
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T681/**`

## 禁止改动

- 配置内容、工作簿、schema、测试逻辑、场景、Prefab、资源、Packages和ProjectSettings。

## 收尾

- [x] 147/147脚本包含中文说明，方法覆盖审计无遗漏。
- [x] 完整配置门、全量EditMode/PlayMode、diff和白名单通过。
