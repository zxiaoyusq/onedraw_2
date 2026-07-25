# T694 预计改动白名单

- `Assets/_Game/Art/Characters/Animated/Moyan/`：两组用户源PNG/JSON、两个AnimationClip和共享AnimatorController。
- `Assets/_Game/Art/Characters/Moyan/moyan_idle.png`及meta：仅在新Prefab和Registry绑定成功后删除旧单帧资源。
- `Assets/_Game/Prefabs/Actors/PlayerMoyan.prefab`：主角动画Prefab。
- `Assets/_Game/Art/SpriteAtlases/Characters.spriteatlasv2`：一次性重建主角图集输入。
- `Assets/_Game/Config/Registry/AssetRegistry.asset`：仅将稳定键`char_moyan_idle`改绑主角Prefab。
- `Design/Config/GameConfig.xlsx`与`config/一笔镇妖_游戏配置表模板.xlsx`：仅将对应AssetManifest行从静态Sprite路径改为Prefab路径/类型并升级内容版本。
- `Assets/_Game/Config/Generated/`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`与`config/examples/gameplay_config.sample.json`：仅由ConfigExporter同源生成。
- `Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`与`ConfigPipelineE2ETests.cs`：仅更新本次内容变更后的冻结hash基线。
- `Assets/_Game/Scripts/Editor/Art/`：幂等T694批量导入入口。
- `Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`：允许玩家配置资源为Sprite或Prefab，并在有效普通笔势后触发纯表现攻击状态。
- `Assets/_Game/Scripts/Bootstrap/T694PlayerAnimationContract.cs`：共享运行时Animator触发器协议。
- `Assets/_Game/Tests/EditMode/T694/`与`Assets/_Game/Tests/PlayMode/T694/`：资源合同及生产玩家路径验证；T230/T240/T630受影响冻结断言同步到新content hash、Registry类型计数、动画图集与无损源图合同。
- `Assets/_Game/Tests/PlayMode/T600/`、`T610/`、`T650/`：只同步配置Ready日志的content版本；T660只替换Unity 6.5弃用的无序对象查询重载以保持零编译警告。
- `artifacts/evals/T694/`：预检计划、日志、截图、验证与差异证据。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/ASSET_SOURCES.md`：任务状态、配置合同、设计决定与来源登记。

明确排除用户已有`Design/Config/~$GameConfig.xlsx`删除状态、玩法数值、手势/伤害判定、场景、其他角色资源和平台设置。
