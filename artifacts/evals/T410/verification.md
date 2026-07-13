# T410 Verification

## 追溯

- 日期：`2026-07-13`
- 任务与范围：T410；实现配置驱动Skill→EffectGroup→有序Effect执行链、门控、显式执行器/目标选择器、玩家治疗适配和终极有效笔势路径。
- 明确不做：T420敌人状态机、T430攻击策略、T440对象池/真实弹池、T510完整战斗流程、HUD、微信平台任务。
- 分支/提交：`main`；本证据随原子提交`T410: implement data-driven skill effect pipeline`落库。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1` / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：`3` / `0.3.0-sample` / `ef7eec3aa29dffb593164526d50eff867e05fabb09fdbcbfc4347d620fb7b3c2`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增无MonoBehaviour依赖的Skills规则/执行器/适配器及测试；玩家模型补充不复活治疗；配置补齐Heal/ClearProjectiles并重导出；同步Schema、兼容窗口、注册表、冻结断言、文档与证据。
- 用户已有改动保护：任务开始工作树干净；最终仅保留白名单差异，测试运行器产生的ProjectSettings差异已恢复。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；仅暂存58个白名单文件，`git diff --cached --check`通过，缓存name-status/stat及关键文本差异已复核。

## 自动验证

- 静态/导出校验：PASS；详见`config-static.log`、`static-audit.md`和`workbook-qa.md`。.NET 56/56，配置漂移0，双工作簿字节一致。
- EditMode XML：MCP未导出XML；T410 4/4、ConfigPipeline 19/19、全量110/110，Job见`unity-mcp-jobs.md`。
- PlayMode XML：MCP未导出XML；T410 1/1、ConfigPipeline 3/3、全量28/28，Job见`unity-mcp-jobs.md`。
- Console新增Error/Warning：最终Refresh编译并清空预期测试日志后`0/0`。

## 玩家与平台证据

- 真实玩家路径和可断言值：PASS；无效笔势不扣100能量且0效果，有效Circle在2.5秒边界按5步执行，清2弹、伤害50、25%处决、Boss易伤2秒。详见`player-path.md`。
- 标准Web：NOT RUN（不属于T410，且用户要求当前继续主内容）。
- 微信转换：NOT RUN（T120保持BLOCKED，用户明确暂缓微信工具链）。
- DevTools：NOT RUN（同上）。
- 真机：NOT RUN（同上）。
- 截图/日志/产物：`workbook-enums.png`、`workbook-skill-effects.png`、`config-static.log`、`unity-mcp-jobs.md`。

## 结论

- 已知问题：T410范围内无。T120/T130平台状态保持既有记录，不伪造平台PASS。
- 结论：PASS。
