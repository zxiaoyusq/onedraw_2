# T210 Verification

## 追溯

- 日期：2026-07-13（Asia/Shanghai）
- 任务与范围：T210；实现独立.NET 8 xlsx读取、稳定JSON序列化/contentHash、`validate/export` CLI、输出自校验和同目录原子替换。
- 明确不做：不实现T220必填/范围/枚举/唯一性/外键/跨表语义；不实现T230 Runtime DTO/索引；不创建T250受管Runtime JSON/hash/ID常量；不修改工作簿、Schema、样例或Unity资产。
- 分支/提交：`main`；提交信息 `T210: implement deterministic config exporter`（本证据随该原子提交纳入）。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`；本任务只改Tools/文档，未调用Editor/MCP、未触发Unity Refresh。
- 配置Schema/内容版本/hash：schema `1` / content `0.1.1-sample` / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：`.gitignore`；`Tools/ConfigExporter/**`；`docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`、任务/进度/索引；`PACKAGE_VALIDATION.md`；本证据目录。
- 用户已有改动保护：任务开始工作树干净；正式xlsx、镜像、Schema、样例、Assets、Packages、ProjectSettings均未修改。
- `git diff --check`：最终审查PASS。
- 暂存白名单审查：最终暂存路径全部属于`change-whitelist.md`，无忽略的`bin/obj`、临时JSON或Unity路径。

## 自动验证

- 静态/导出校验：锁定还原PASS；`dotnet format --verify-no-changes` PASS；.NET build PASS（0 warning/0 error）；`validate --strict` PASS；两次`export --strict`字节一致；非法输入退出码3。
- .NET测试：8 / 8 / 0；`test-results/T210-tests.trx`。覆盖双导出、冻结hash/样例语义、源行反转、fr-FR、表头漂移、CLI错误码和原子写保护。
- EditMode XML：NOT RUN；改动不进入`Assets/`或Unity程序集。
- PlayMode XML：NOT RUN；改动不进入玩家运行路径。
- Console新增Error/Warning：NOT RUN；.NET编译为0 warning/0 error，Unity未刷新且没有Unity文件差异。

## 玩家与平台证据

- 真实玩家路径和可断言值：NOT RUN；T210产物尚未接入Runtime，真实路径不适用且不得外推。
- 标准Web：NOT RUN（沿用T100既有`PASS WITH KNOWN ISSUES`，本任务不重申新PASS）。
- 微信转换：NOT RUN（沿用T120既有G2 `PASS WITH KNOWN ISSUES`）。
- DevTools：BLOCKED（既有缺工具阻塞，按用户决定延期）。
- 真机：BLOCKED（既有缺设备与G3阻塞，按用户决定延期）。
- 截图/日志/产物：`dotnet-info.txt`、`dotnet-restore-locked.txt`、`dotnet-build.txt`、`dotnet-test.txt`、TRX、CLI日志、双导出字节/文件SHA-256记录；临时JSON位于忽略的`artifacts/tmp/T210/`，未提交。

## 结论

- 已知问题：T210 `--strict`仍只覆盖可导出性/结构契约/确定性；生产级坏配置拒绝属于已READY的T220。Runtime JSON尚未生成或接入。
- 结论：PASS；T210验收项全部满足，可以进入T220。
