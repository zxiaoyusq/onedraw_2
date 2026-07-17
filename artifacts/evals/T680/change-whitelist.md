# T680 Change Whitelist

- Git基线：`6c711ca535c0cec0fbbeee095e56e5507799d253`
- 保护用户已有改动：`AGENTS.md`，不修改、不暂存。
- 目标：让ConfigExporter为`ConfigIds.g.cs`确定性生成中文职责注释，并通过导出器重新生成。

## 白名单

- `Tools/ConfigExporter/Generation/ConfigIdsGenerator.cs`
- `Tools/ConfigExporter/Tests/ConfigPipelineE2ETests.cs`
- `Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`
- `Assets/_Game/Config/Generated/gameplay_config.json`与`.hash`仅允许导出器执行后字节不变
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`
- `artifacts/evals/T680/**`

## 禁止改动

- 工作簿、schema、DTO、配置内容、其他产品代码、场景、Prefab和资源。

## 收尾

- [x] 生成物含中文头部、元数据、分组和ID说明，且不手改。
- [x] .NET/ConfigPipeline专项、全量、漂移和白名单通过。
