# AGENTS.md — Claude Code / Codex 项目执行合同

你正在实现《一笔镇妖》Unity微信小游戏。不要一次实现整款游戏。做出满足需求的最小规模、正确且可验证的变更。

## 可按需查看的相关文档
- `docs/PROJECT_STRUCTURE.md`：工程结构
- `docs/GAME_DESIGN_MVP.md`：玩法规则
- `docs/MVP_SCOPE.md`：范围约束
- `docs/TECH_SPEC.md`：技术细节
- `docs/CONFIG_SCHEMA.md`：配置规范
- `docs/TASKS.md`：任务列表

冲突裁决：玩法看`docs/GAME_DESIGN_MVP.md`；范围看`docs/MVP_SCOPE.md`；技术看`docs/TECH_SPEC.md`；配置看`docs/CONFIG_SCHEMA.md`；任务状态看`docs/TASKS.md`。

## 绝对规则

- 只做第一个依赖均为DONE的READY任务；任何时刻只做一个原子任务。
- 开始前记录Git状态、保护用户已有改动，并先写预计改动白名单。
- 所有玩法数值、敌人、技能、效果、关卡、波次、Boss、教程和文案必须来自配置表。
- Inspector/ScriptableObject只能保存Unity资源引用、场景引用和明确调试兜底，不能成为第二数值库。
- 配置源与生成物的身份以 docs/CONFIG_SCHEMA.md 为准。配置变更应修改权威源，并通过导出器重新生成 JSON、DTO 或其他生成物；不得通过同时手改源文件和生成物来维持表面一致。。
- 纯规则放在无MonoBehaviour依赖的C#中，并优先写EditMode测试。
- 场景、Prefab、资源导入用Unity Editor或MCP；不要手工编辑Unity YAML。
- 微信SDK必须固定版本或commit；转换成功不等于开发者工具或真机成功。
- 仅当当前任务的验收标准明确依赖 Unity Editor、MCP、微信开发者工具或真机，而该环境不可用时，任务才标记为 BLOCKED。缺少与当前验收无关的环境不构成阻塞。不伪造截图、测试或PASS。
- Web运行时不解析xlsx，不使用Task.Run或自建托管线程作为核心方案。
- Gameplay不直接调用微信SDK静态API，统一通过 `IPlatformService`。
- 一个任务一个可回滚提交，提交信息以TASK-ID开头。
- 不在任务结束时顺手实现下一任务。

## 架构规则
- 保持运行时代码与编辑器代码分离。
- 在与现有代码库风格一致的前提下，独立可测试的领域逻辑优先使用纯 C# 实现。
- MonoBehaviour 类应聚焦于 Unity 生命周期与集成逻辑。
- 除非项目有其他约定，否则每个文件只包含一个主要公共类型。
- Unity API 调用必须在 Unity 主线程执行。
- 物理驱动的更新逻辑应酌情使用 FixedUpdate。
- 热点路径中避免重复调用 Find、FindObjectOfType、GetComponent，避免使用 LINQ、装箱、字符串格式化以及临时集合分配。
- 缓存稳定的组件与资源引用。
- 对象生命周期结束时，取消事件订阅并取消异步任务。
- 不要为了掩盖错误而捕获异常。
- 日志应输出可定位问题的上下文，但不要添加逐帧的冗余日志。
- 除非另有文档说明，否则修改代码产生的警告视为错误。

## 代码注释规范
- 新增或修改的公共类型、公共接口、核心领域规则、非显然逻辑、边界条件、平台限制及 workaround 应添加清晰的中文注释。不要为显而易见的赋值、分支或方法调用添加重复代码语义的注释。

## 完成后更新

- `docs/TASKS.md`
- `docs/PROGRESS.md`
- 必要时 `docs/DECISIONS.md` 和 `docs/BUGS.md`
