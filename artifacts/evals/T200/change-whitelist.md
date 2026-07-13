# T200 Change Whitelist

- Git基线：`130569421166a20eb1c2345bab4c313fb03d8ad1`（`main`，任务开始时除新建T200证据目录外工作树干净）。
- 需要保护的用户已有改动：无；若出现白名单外改动，先判定来源并保留，不覆盖。
- 任务目标：审查并冻结正式配置工作簿、字段字典、枚举、ID规则、数据所有权、外键和样例JSON/schema，使T210可据此实现确定性导出器。
- 明确不做：不实现T210导出器或T220校验器；不创建Runtime DTO；不手改生成JSON；不恢复T120/T130；不修改玩法实现、场景、Prefab或Unity资源设置。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`：通过artifact-tool修复审计确认的字段/枚举/所有权/外键契约缺口，并保持现有格式约定。
- `config/一笔镇妖_游戏配置表模板.xlsx`：仅在正式工作簿的字段契约变化且必须保持示例模板同步时更新；不得成为第二配置真相源。
- `config/examples/gameplay_config.sample.json`、`config/schema/gameplay.schema.json`：仅同步工作簿字段契约和样例内容；不生成Runtime产物。
- `Design/Config/README.md`、`config/README.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`、`一笔镇妖_Unity微信小游戏开发计划_ClaudeCodex版.md`：冻结数据所有权、ID/空值/枚举/外键/排序规则和双工作簿边界，并同步非权威计划中的旧Boss玩法ID。
- `PACKAGE_VALIDATION.md`：同步最终工作表/字段/样例/校验摘要。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T200状态、配置hash和下一任务。
- `artifacts/evals/T200/**`：基线、审计报告、结构/公式/外键/样例/schema校验结果、全表视觉预览、白名单和最终验证。
- `artifacts/tmp/T200-spreadsheet/**`：仅本地artifact-tool脚本、中间导出和预览，不提交。

## 禁止改动

- 不修改`Tools/ConfigExporter`实现、`Assets/_Game/Config/Generated/**`、Runtime/Editor C#、测试程序集、场景、Prefab、正式美术、embedded SDK、Packages或ProjectSettings。
- 不把`config/一笔镇妖_游戏配置表模板.xlsx`或Inspector变成第二数值库；正式内容唯一源始终是`Design/Config/GameConfig.xlsx`。
- 不把Excel公式结果作为未声明的运行时输入；不静默修正无法从权威设计推导的平衡值。

## 收尾审查

- [ ] `git status --short`中的每一项都属于白名单。
- [ ] `git diff --check`通过；二进制xlsx通过结构化审计和hash复核。
- [ ] 仅暂存白名单文件，并审查`git diff --cached`。
