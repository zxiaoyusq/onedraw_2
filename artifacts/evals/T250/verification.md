# T250 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T250；把生产配置导出、完整校验、JSON/hash/ConfigIds生成物漂移检查和Unity配置测试接入一条命令。
- 明确不做：不修改xlsx/Schema/样例内容；不实现T300；不恢复T120/T130或执行微信打包、DevTools和真机工作。
- 分支/提交：`main` / `T250: automate config verification pipeline`
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1；当前工程GUI实例用于编译、专项/全量测试和玩家路径，隔离的当前工作树副本用于默认批处理一键入口。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：ConfigExporter同模型生成/校验、CLI和54项测试；CI一键脚本和Unity分类参数；受管hash/ConfigIds及Unity测试；配置流水线文档和任务证据。
- 用户已有改动保护：任务开始工作树干净；正式/镜像xlsx、Schema、样例、Packages、ProjectSettings、场景、Prefab、Registry均无差异；T230 JSON无字节差异。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；42个文件全部属于预计白名单，禁止目录0项、未暂存差异0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：默认`verify-config.sh`完整PASS；三生成物逐字节一致；.NET 54/54；受控hash漂移以`CFG013`和退出码3检出；两项目`dotnet format --verify-no-changes`、脚本语法/合同冒烟及`git diff --check`均PASS。
- EditMode XML：ConfigPipeline 19 / 19 / 0；全量32 / 32 / 0；隔离一键结果位于忽略目录`artifacts/tmp/T250/e2e-project/artifacts/tmp/T250/editmode-results.xml`。
- PlayMode XML：ConfigPipeline 3 / 3 / 0；全量5 / 5 / 0；隔离一键结果位于忽略目录`artifacts/tmp/T250/e2e-project/artifacts/tmp/T250/playmode-results.xml`。
- Console新增Error/Warning：真实Bootstrap玩家路径0 / 0。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载schema 1/content 0.1.1-sample配置28表645条和76键AssetRegistry后进入MainMenu；配置/Registry成功摘要存在，Console Error/Warning为0。
- 标准Web：NOT RUN（T250不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T250范围）。
- 真机：BLOCKED（既有T120门，非T250范围）。
- 截图/日志/产物：见`generated-artifacts.md`、`pipeline-e2e.md`、`unity-test-jobs.md`、`runtime-smoke.md`和`static-audit.md`。

## 结论

- 已知问题：无T250新增产品问题；GUI Unity占用当前工程时，完整批处理入口需在隔离的当前工作树副本运行，此次已验证通过。
- 结论：PASS。
