# T220 Change Whitelist

- Git基线：`b61944f0a945299a4b126079827ae66ff2792192`（`main`；任务开始工作树干净）。
- 需要保护的用户已有改动：无；后续若出现白名单外改动，保留并先判定来源，不覆盖。
- 任务目标：在T210独立.NET 8导出器的同一内存模型上实现生产级配置校验；拒绝必填、ID、类型约束、范围、枚举、主键/组合键、普通/分组/通配符/conditional外键、组内order、Level/Wave/Spawn、星级、Boss阶段、策略ID和资源/文案/cue引用错误，并提供稳定错误码及Sheet/Excel行/字段定位。
- 明确不做：不修改正式xlsx、镜像、Schema或样例内容；不实现T230 Unity Runtime DTO/加载/索引；不创建T240 AssetRegistry或T250受管JSON流水线；不恢复T120/T130。

## 预计改动白名单

- `Tools/ConfigExporter/Model/**`、`Processing/**`、`Validation/**`、`Services/**`、`Cli/**`：扩展约束元数据、生产校验规则、服务集成、错误摘要；不得复制玩法数值或引入Unity依赖。
- `Tools/ConfigExporter/Tests/**`：新增`ConfigValidationTests`、内存坏配置构造器和可审查的坏配置用例清单；正式xlsx保持只读，不提交派生坏工作簿。
- `Tools/ConfigExporter/README.md`：同步T220已实现校验范围和错误码。
- `docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`、`PACKAGE_VALIDATION.md`：同步生产校验流程、边界和验证结论；不改变冻结内容契约。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T220状态、证据和下一任务。
- `artifacts/evals/T220/**`：基线、白名单、测试/CLI/坏配置定位、保护路径和最终验证证据。
- `artifacts/tmp/T220/**`：忽略的临时导出，不提交。

## 禁止改动

- 不修改 `Design/Config/GameConfig.xlsx`、模板镜像、`config/schema/**`、`config/examples/**`、`Assets/**`、`Packages/**`、`ProjectSettings/**`、微信SDK、场景、Prefab或玩法C#。
- 不提交NuGet缓存、`bin/obj`、临时JSON或派生坏xlsx；不使用Inspector/ScriptableObject/C#保存玩法平衡值。
- 测试坏数据只修改内存中的`RawWorkbook`/`ConfigDocument`副本；表格工作流在本任务保持正式工作簿只读，因此不产生需要渲染交付的新xlsx。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
