# T210 Change Whitelist

- Git基线：`d6105f67fc70078f273be5bb5d24bc38090df150`（`main`，任务开始时除新建T210证据目录外工作树干净）。
- 需要保护的用户已有改动：无；若后续出现白名单外改动，保留并先判定来源，不覆盖。
- 任务目标：实现独立.NET 8命令行工具，从冻结的29-Sheet xlsx读取28张数据表，按T200契约转换类型、稳定排序、计算contentHash、完成输出自校验和同文件系统原子替换，并提供确定性与表头测试。
- 明确不做：不实现T220的范围/枚举/唯一性/外键/跨表生产校验；不实现T230 Unity Runtime加载/DTO索引；不改正式工作簿、Schema或玩法内容；不创建受管Runtime JSON；不恢复T120/T130。

## 预计改动白名单

- `.gitignore`：只补充独立.NET项目的嵌套`bin/obj`忽略与ConfigExporter项目文件例外。
- `Tools/ConfigExporter/**`：独立.NET 8 CLI、Open XML读取、通用配置模型、稳定序列化/hash、原子写入、命令解析、固定依赖/锁文件、许可证记录和使用文档；不得把依赖放入Unity Runtime。
- `Tools/ConfigExporter/Tests/**`：ExporterDeterminismTests、ExporterHeaderTests及T210边界内的CLI/原子写入/区域设置测试。
- `docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`：同步已实现命令、固定依赖/许可证、T210与T220/T230边界。
- `PACKAGE_VALIDATION.md`：同步当前导出器/测试基线摘要。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T210状态、验证结论和下一任务。
- `artifacts/evals/T210/**`：基线、白名单、dotnet/依赖信息、测试结果、双导出/hash/样例对照、原子替换和最终验证证据。
- `artifacts/tmp/T210/**`：忽略的测试输出、临时工作簿和恢复缓存，不提交。

## 禁止改动

- 不修改 `Design/Config/GameConfig.xlsx`、模板镜像、`config/schema/**`、`config/examples/**`、`Assets/**`、`Packages/**`、`ProjectSettings/**`、微信SDK、场景、Prefab或玩法C#。
- 不提交NuGet全局缓存、SDK、`bin/obj`、临时JSON或测试变体；不把Open XML/test依赖复制到Unity工程。
- `validate`在T210只证明工作簿可读取、Sheet/表头/类型/Schema契约与确定性输出可成立；不得把它描述成T220完整内容校验。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过；项目/锁文件、许可证和所有测试输出可追溯。
- [x] 仅暂存白名单文件，并审查`git diff --cached`；正式xlsx、Schema、样例JSON和Unity路径无差异。
