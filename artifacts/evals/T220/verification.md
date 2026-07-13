# T220 Verification

## 追溯

- 日期：2026-07-13（Asia/Shanghai）
- 任务与范围：T220；在T210完整内存模型上实现生产级ConfigValidator、稳定错误码与Sheet/Excel行/字段定位、整包拒绝和可审查坏配置样例。
- 明确不做：不修改正式xlsx、镜像、Schema或样例；不生成Runtime JSON；不实现T230 DTO/加载/索引、T240 AssetRegistry或T250一键生成；不恢复T120/T130。
- 分支/提交：`main`；提交信息 `T220: implement production config validation`（本证据随该原子提交纳入）。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`；本任务只改独立.NET Tools/文档，未调用Editor/MCP、未触发Unity Refresh。
- 配置Schema/内容版本/hash：schema `1` / content `0.1.1-sample` / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：`Tools/ConfigExporter`生产校验/服务/CLI及测试；`README`、配置流水线、决策、任务/进度/索引、包验证报告；本证据目录。
- 用户已有改动保护：任务开始工作树干净；正式xlsx、模板镜像、Schema、样例、Assets、Packages、ProjectSettings均未修改，见`protected-path-diff.txt`。
- `git diff --check`：最终审查PASS。
- 暂存白名单审查：最终暂存路径全部属于`change-whitelist.md`；无`bin/obj`、临时JSON、派生坏xlsx或Unity路径。

## 自动验证

- 静态/导出校验：锁定还原PASS；Release build 0 warning/0 error；两个项目`dotnet format --verify-no-changes` PASS；正式`validate --strict` PASS；双`export --strict`字节一致。
- .NET测试：46 / 46 / 0；`test-results/T220-tests.trx`。其中ConfigValidationTests专项38/38（正式正例1 + 内存坏配置37）。
- EditMode XML：NOT RUN；改动不进入`Assets/`或Unity程序集，T220指定验证为独立.NET `ConfigValidationTests`。
- PlayMode XML：NOT RUN；Runtime尚未接入，玩家路径不属于T220。
- Console新增Error/Warning：NOT RUN；Unity未刷新且无Unity文件差异；.NET Release编译0 warning/0 error。

## 玩家与平台证据

- 真实玩家路径和可断言值：NOT RUN；T220没有Runtime配置消费者，不把Tools正例外推为游戏已接入。
- 标准Web：NOT RUN（沿用T100既有`PASS WITH KNOWN ISSUES`，本任务不重申新PASS）。
- 微信转换：NOT RUN（沿用T120既有G2 `PASS WITH KNOWN ISSUES`）。
- DevTools：BLOCKED（既有缺工具阻塞，按用户决定延期）。
- 真机：BLOCKED（既有缺设备与G3阻塞，按用户决定延期）。
- 截图/日志/产物：TRX及`dotnet-validation.txt`、`cli-validation.txt`、`schema-mirror-audit.txt`、`input-sha256.txt`、`protected-path-diff.txt`；双导出临时JSON位于忽略的`artifacts/tmp/T220/`，未提交。

## 结论

- 已知问题：T230/T250尚未实现Unity Runtime加载和受管JSON；T220只确认输入整包可被生产校验器接受或拒绝。微信G3/G4既有阻塞不在本任务范围。
- 结论：PASS；T220验收项全部满足，可以进入T230。
