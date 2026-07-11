# 00_BOOTSTRAP：首次建立工程

你是本仓库的实现代理。先不要写玩法业务代码。

## 必须读取

1. `AGENTS.md`
2. `project-index.yaml`
3. `docs/GAME_DESIGN_MVP.md`
4. `docs/MVP_SCOPE.md`
5. `docs/TECH_SPEC.md`
6. `docs/CONFIG_SCHEMA.md`
7. `docs/TASKS.md`
8. `docs/PROGRESS.md`
9. `docs/DECISIONS.md`
10. `reference/ENGINEERING_RETROSPECTIVE.md`

## 本轮只执行

只执行 `T000`。不要擅自执行 `T010` 或任何后续任务。

## 开始前输出

- 文档冲突与待确认项。
- Git根、嵌套`.git`、已有改动和需要保护的文件。
- 当前可用Unity版本、模块、MCP/Editor状态。
- 目标、不做项、预计文件白名单。
- 验收步骤和可能BLOCKED的人工前置。

## 完成要求

- 统一玩法、范围、技术、配置和完成定义。
- 所有TBD已确认，或明确写为BLOCKED并说明需要谁决定。
- 更新 `project-index.yaml`、`docs/TASKS.md`、`docs/PROGRESS.md`。
- 保存 `artifacts/evals/T000/verification.md`。
- `git diff --check`，只提交T000产生的文件。
- 提交信息：`T000 establish project contracts`
- 完成后停止，不开始下一任务。
