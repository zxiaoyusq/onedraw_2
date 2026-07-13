# T040 Change Whitelist

- 基线：`main` / `ad942bd0 T030: establish scene and assembly skeleton`，开始时工作树干净。
- `Tools/CI/**`：新增Unity批处理测试命令、NUnit XML结果检查、Web构建命令入口、任务证据初始化器和无Unity依赖的harness自检夹具。
- `Assets/_Game/Scripts/Editor/Build/**`：新增标准WebGL构建Editor入口；本任务只编译和验证参数，不执行实际Web构建。
- `Assets/_Game/Tests/EditMode/T040/**`及EditMode asmdef：新增工作流合同测试并引用Editor程序集。
- `templates/verification.md`、`templates/change-whitelist.md`、`artifacts/evals/README.md`、`docs/TEST_PLAN.md`、`docs/WORKFLOW.md`：建立可复用证据与一任务一提交说明。
- `artifacts/evals/T040/**`：保存白名单、XML、过滤后的摘要、自检和验证结论；原始Unity日志仅放`artifacts/tmp/`且不提交。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T040状态和工作流索引。
- 不修改场景、Prefab、玩法代码、配置Excel/JSON或平台SDK；不执行T100标准Web构建，不宣称Web/微信/DevTools/真机PASS。
