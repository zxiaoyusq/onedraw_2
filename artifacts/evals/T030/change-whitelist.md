# T030 Change Whitelist

- 基线：`main` / `fe1c3d92 T020: establish Unity subsystem baseline`，开始时工作树干净。
- `Assets/_Game/Art/**`、`Assets/_Game/Config/**`、`Assets/_Game/Prefabs/**`：创建技术规格规定的目录骨架及Unity生成的`.meta`。
- `Assets/_Game/Scripts/**`：创建Core、Config、Input、Combat、Actors、Skills、Levels、Presentation、Platform、Bootstrap与Editor程序集骨架；仅实现T030场景流接口和最小启动实现。
- `Assets/_Game/Scenes/**`：仅通过Unity Editor/MCP创建并保存Bootstrap、MainMenu、Battle三场景。
- `Assets/_Game/Tests/EditMode/**`、`Assets/_Game/Tests/PlayMode/**`：新增T030专项测试并按需更新测试asmdef引用。
- `ProjectSettings/EditorBuildSettings.asset`：仅允许Unity Editor更新三场景构建列表。
- `artifacts/evals/T030/**`：保存白名单、测试结果、日志摘要和验证结论。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T030状态和工程索引。
- 不删除或手改`Assets/Scenes/SampleScene.unity`，不修改玩法/配置内容，不开始T040。
