# T690 预计改动白名单

- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `Design/Config/GameConfig.xlsx`
- `config/一笔镇妖_游戏配置表模板.xlsx`
- `Assets/_Game/Config/Generated/gameplay_config.json`
- `Assets/_Game/Config/Generated/gameplay_config.hash`
- `Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`（仅允许导出器确定性结果）
- `Assets/_Game/Config/Registry/AssetRegistry.asset`
- `Assets/_Game/Art/Enemies/FireFish/**`
- `Assets/_Game/Art/SpriteAtlases/Enemies.spriteatlasv2`
- `Assets/_Game/Prefabs/Actors/EnemyFireFish.prefab*`
- `Assets/_Game/Scripts/Editor/Art/T690FireFishAnimationAuthoring.cs*`
- `Assets/_Game/Scripts/Editor/Art/T630ArtAssetAuthoring.cs`
- `Assets/_Game/Scripts/Editor/OneStrokeDemon.Editor.asmdef`
- `Assets/_Game/Scripts/Config/AssetRegistryException.cs`
- `Assets/_Game/Scripts/Config/GameplayConfigException.cs`
- `Assets/_Game/Tests/EditMode/T630/AssetImportValidationTests.cs`
- `Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`
- `Assets/_Game/Tests/EditMode/T240/AssetRegistryValidationTests.cs`
- `Assets/_Game/Tests/EditMode/T690/FireFishAnimationAssetTests.cs`
- `Assets/_Game/Tests/PlayMode/T690/FireFishAnimationPoolPlayModeTests.cs`
- `Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`
- `Assets/_Game/Tests/PlayMode/T240/AssetRegistryBootstrapPlayModeTests.cs`
- `Assets/_Game/Tests/EditMode/**T690**`
- `Assets/_Game/Tests/PlayMode/**T690**`
- `artifacts/evals/T690/**`
- `outputs/T690/GameConfig.xlsx`
- `config/examples/gameplay_config.sample.json`
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`
- `Tools/ConfigExporter/Tests/ConfigPipelineE2ETests.cs`
- `Tools/ConfigExporter/Generation/ConfigIdsGenerator.cs`
- `Assets/_Game/Tests/EditMode/T040/WorkflowContractTests.cs`
- `Assets/_Game/Tests/EditMode/T120/WechatBuildEntryTests.cs`

编译解阻说明：配置重新导出会触发相关程序集全量编译；上述两个异常类型原有的
`Source` 属性会隐藏 `System.Exception.Source`，在当前警告即错误策略下导致编译失败。
本任务仅显式声明 `new`，不改变运行时行为。

配置回归说明：`enemy_fire_fish` 从 Sprite 改为 Prefab 会更新配置内容哈希，并使
Registry 的 Prefab/Sprite 分类计数各增减 1；对应既有精确快照断言随权威源更新。

跨平台生成说明：配置重生成在 Windows 暴露 `StringBuilder.AppendLine` 的 CRLF 漂移；
生成器按冻结契约在编码前统一为 LF，确保 Linux/Windows 字节一致。

全量回归解阻说明：两个既有构建路径测试写死 `/`，在 Windows 对正确的绝对路径产生
假失败；断言改为 `Path.GetFullPath` / `Path.Combine`，保持原契约并跨平台一致。

明确排除并保护用户已有改动：

- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/UnityConnectSettings.asset`
