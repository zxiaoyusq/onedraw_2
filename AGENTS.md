# AGENTS.md — Claude Code / Codex 项目执行合同

你正在实现《一笔镇妖》Unity微信小游戏。不要一次实现整款游戏。

## 每次会话必须按此顺序读取

1. `project-index.yaml`
2. `docs/GAME_DESIGN_MVP.md`
3. `docs/MVP_SCOPE.md`
4. `docs/TECH_SPEC.md`
5. `docs/CONFIG_SCHEMA.md`
6. `docs/TASKS.md`
7. `docs/PROGRESS.md`
8. 当前任务相关测试、实现和最近证据

冲突裁决：玩法看GAME_DESIGN；范围看MVP_SCOPE；技术看TECH_SPEC；配置看CONFIG_SCHEMA；任务状态看TASKS。

## 绝对规则

- 只做第一个依赖均为DONE的READY任务；任何时刻只做一个原子任务。
- 开始前记录Git状态、保护用户已有改动，并先写预计改动白名单。
- 所有玩法数值、敌人、技能、效果、关卡、波次、Boss、教程和文案必须来自配置表。
- Inspector/ScriptableObject只能保存Unity资源引用、场景引用和明确调试兜底，不能成为第二数值库。
- 配置变更必须同步受影响的Excel、字段字典、导出器、JSON、DTO、校验、文档和测试。
- 纯规则放在无MonoBehaviour依赖的C#中，并优先写EditMode测试。
- 场景、Prefab、资源导入用Unity Editor或MCP；不要手工编辑Unity YAML。
- 微信SDK必须固定版本或commit；转换成功不等于开发者工具或真机成功。
- 缺Editor、MCP、DevTools或真机时如实BLOCKED，不伪造截图、测试或PASS。
- Web运行时不解析xlsx，不使用Task.Run或自建托管线程作为核心方案。
- Gameplay不直接调用微信SDK静态API，统一通过 `IPlatformService`。
- 一个任务一个可回滚提交，提交信息以TASK-ID开头。
- 不在任务结束时顺手实现下一任务。

## 最小反馈环

```text
小范围修改
→ 静态检查/配置导出
→ Unity Refresh和编译
→ Console Error检查
→ 专项EditMode
→ 专项PlayMode
→ 真实玩家路径
→ 任务需要时全量回归
→ git diff --check / status / 白名单审查
→ 保存证据并提交
```

## 代码注释规范
- 新增或修改的 C# 代码都要有明确易懂的中文注释

## 完成后必须更新

- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `project-index.yaml`
- `artifacts/evals/TASK-ID/verification.md`
- 必要时 `docs/DECISIONS.md` 和 `docs/BUGS.md`
