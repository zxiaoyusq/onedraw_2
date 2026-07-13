# T410 Workbook QA

本任务按Spreadsheets工作流使用`@oai/artifact-tool`编辑、检查和渲染工作簿；没有直接修改xlsx内部XML。

## 变更闭环

- `Enums`新增`EffectType.Heal`与`EffectType.ClearProjectiles`。
- `SkillEffects`把`ClearProjectiles(Battle)`加入`fx_ultimate_seal`第2步，后续效果顺延并保持连续order。
- 初次导出以`CFG002`拒绝Schema与Enums不一致，确认EffectType是冻结合同后同步Schema、运行时兼容窗口、导出器注册表、JSON、ConfigIds、测试与文档；没有新增字段、Sheet或DTO属性。
- 两份权威工作簿字节一致，SHA-256均为`eb7cd040298bcf9c6b9a86dcc46b971663e4e4698153702f0ead15491e1311e3`。

## 视觉与公式检查

- `Enums`最终渲染：`workbook-enums.png`，新增枚举行可读、列宽与既有样式一致。
- `SkillEffects`最终渲染：`workbook-skill-effects.png`，终极5步链可读、order连续且未遮挡。
- artifact-tool公式错误扫描：`#REF!/#DIV/0!/#VALUE!/#NAME?/#N/A`合计`0`。
- 受管导出与漂移检查：PASS；650条、169,528字节、content hash `ef7eec3aa29dffb593164526d50eff867e05fabb09fdbcbfc4347d620fb7b3c2`。
