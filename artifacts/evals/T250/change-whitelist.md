# T250 Change Whitelist

- Git基线：`3c3a027e7784c1ea4a3c7d37e455d370fdf5f3d8`（`main`；任务开始工作树干净）。
- 需要保护的用户已有改动：无；若后续出现白名单外差异，保留并先判定来源，不覆盖。
- 任务目标：由T210/T220同一完整配置模型确定性生成JSON、hash旁车和`ConfigIds.g.cs`；提供默认执行.NET生产校验/测试、生成物字节漂移检查及Unity配置EditMode/PlayMode分类测试的一条命令；任何漂移或测试失败返回非零。
- 明确不做：不修改策划工作簿内容、模板镜像、Schema语义或样例内容；不实现T300输入/手势；不恢复T120/T130、微信DevTools、打包或真机；不手工维护生成JSON/hash/ID常量。

## 预计改动白名单

- `Tools/ConfigExporter/Cli/**`、`Tools/ConfigExporter/Model/**`、`Tools/ConfigExporter/Services/**`、`Tools/ConfigExporter/Generation/**`、`Tools/ConfigExporter/IO/**`、`Tools/ConfigExporter/Diagnostics/**`：增加`generate/verify`命令、同模型三生成物、只读字节漂移诊断与原子写；不得复制xlsx解析、排序、校验或hash算法。
- `Tools/ConfigExporter/Tests/**`、`Tools/ConfigExporter/README.md`：新增ConfigPipelineE2E、确定性、三类漂移、CLI退出码和生成C#标识符测试；不生成或提交派生坏xlsx。
- `Tools/CI/verify-config.sh`、`Tools/CI/run-unity-tests.sh`、`Tools/CI/test-harness-smoke.sh`：新增一键配置闭环、Unity Test Category参数和非破坏性脚本合同；默认不得跳过Unity，`--skip-unity`只能明确输出PARTIAL而不能伪报全绿。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`及Unity生成`.meta`：仅由导出器生成/刷新；JSON应与T230快照字节一致，hash旁车只含规范contentHash和LF。
- `Assets/_Game/Scripts/Config/Generated/**`及目录`.meta`：仅由导出器生成`ConfigIds.g.cs`并由Unity生成`.meta`；放在`OneStrokeDemon.Config` asmdef作用域，禁止手工维护ID或平衡值。
- `Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T240/**`、`Assets/_Game/Tests/PlayMode/T230/**`、`Assets/_Game/Tests/PlayMode/T240/**`：只允许添加`ConfigPipeline`分类，不改变既有断言。
- `Assets/_Game/Tests/EditMode/T250/**`及目录`.meta`：新增生成hash/ConfigIds/Runtime/AssetRegistry一致性测试，不复制第二份配置模型。
- `docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/WORKFLOW.md`、`docs/DECISIONS.md`：同步生成物所有权、asmdef路径、一键命令、漂移门和完成定义。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T250状态、证据与下一任务T300。
- `artifacts/evals/T250/**`：基线、白名单、.NET/生成物/脚本/Unity测试、真实玩家路径、Console与最终验证证据。
- `artifacts/tmp/T250/**`：忽略的临时生成物、Unity XML和原始日志，不提交。

## 禁止改动

- 不修改`Design/Config/GameConfig.xlsx`、模板xlsx、`config/schema/gameplay.schema.json`、`config/examples/gameplay_config.sample.json`、Packages、ProjectSettings、场景、Prefab、Registry、美术、微信SDK/构建或平台外部状态。
- 不提交`bin/obj/Library/Logs/Temp`、临时导出目录或Unity原始日志；不让verify默认更新受管文件，不把生成物漂移降级为warning。
- `.meta`只允许Unity Editor/MCP生成；若PlayMode测试临时改写EditorSettings，必须恢复任务基线且不得暂存。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
