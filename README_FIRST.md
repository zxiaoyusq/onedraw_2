# 从这里开始：一笔镇妖 Unity微信小游戏开发包

本仓库已在根目录初始化为Unity `6000.5.1f1` 2D工程，并包含供Claude Code或Codex持续执行的工程合同、任务树、配置模板和验收规范。

## 立即使用

1. 使用Unity `6000.5.1f1` 打开当前仓库根目录。
2. 在Claude Code或Codex中按 `AGENTS.md` 和 `project-index.yaml` 的当前任务继续执行。
3. 每次只执行第一个依赖均为DONE的READY任务。
4. 正式配置源已经放在 `Design/Config/GameConfig.xlsx`。
5. 每轮只做一个原子任务、一个提交，并保存 `artifacts/evals/TASK-ID/verification.md`。

## 包内关键文件

- `一笔镇妖_Unity微信小游戏开发计划_ClaudeCodex版.md`：完整单文件开发计划。
- `AGENTS.md`：代理执行硬规则，Codex可直接读取。
- `CLAUDE.md`：Claude Code入口。
- `PROMPT_TO_START.txt`：第一条可直接粘贴的指令。
- `project-index.yaml`：当前任务、版本和人工平台门。
- `docs/GAME_DESIGN_MVP.md`：玩法真相。
- `docs/MVP_SCOPE.md`：范围真相。
- `docs/TECH_SPEC.md`：技术真相。
- `docs/CONFIG_SCHEMA.md`：配置契约。
- `docs/TASKS.md`：49个原子任务、依赖、验收和测试。
- `tasks/task-index.json`：机器可读任务索引。
- `prompts/`：Bootstrap、下一任务、验收、配置变更、微信Spike和候选版本提示词。
- `Design/Config/GameConfig.xlsx`：可直接使用的配置工作簿。
- `config/examples/`：与工作簿对应的示例JSON。
- `config/schema/`：JSON Schema。
- `Tools/ConfigExporter/README.md`：独立导出器实现合同。
- `reference/ENGINEERING_RETROSPECTIVE.md`：你上传的工程复盘参考。
- `reference/PSD_ASSET_NOTES.md`：PSD资源接入判断。

## 配置表覆盖

工作簿共有 29 个工作表，包含全局参数、玩家、架势、手势、伤害、护甲、弱点、移动策略、敌人、攻击、弹幕、Buff、技能、效果链、关卡、波次、出生点、精英修饰、Boss阶段、奖励、教程、文本、音频、VFX、资源清单、枚举和字段字典。

所有示例数值只用于灰盒验证，不是最终平衡值。

## 首要原则

先完成合同、仓库、标准Web与微信平台Spike，再实现玩法；所有数值、敌人、技能、效果、关卡和文案都必须走：

```text
Excel → 导出器 → 校验 → 稳定JSON → Unity只读索引 → 运行时系统
```

Inspector和ScriptableObject只保存Unity资源与场景引用，不能成为第二数值库。
