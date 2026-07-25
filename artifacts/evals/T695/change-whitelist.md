# T695 预计改动白名单

- `Assets/_Game/Art/VFX/Animated/EnemyDeath/`：用户源PNG、逐帧JSON、11帧AnimationClip、AnimatorController及死亡特效Prefab。
- `Assets/_Game/Art/SpriteAtlases/VFX.spriteatlasv2`：纳入动画VFX目录并由Unity重建。
- `Assets/_Game/Config/Registry/AssetRegistry.asset`：绑定稳定键`vfx_enemy_death`到新Prefab。
- `Design/Config/GameConfig.xlsx`与`config/一笔镇妖_游戏配置表模板.xlsx`：在权威配置及镜像中新增死亡VFX、反馈映射和资源清单，并升级内容版本。
- `Assets/_Game/Config/Generated/`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`与`config/examples/gameplay_config.sample.json`：仅由ConfigExporter同源生成。
- `Tools/ConfigExporter/Tests/`：仅同步本次配置导出后的冻结版本、hash、记录数和稳定ID基线。
- `Assets/_Game/Scripts/Editor/Art/T695EnemyDeathVfxAuthoring.cs`：幂等T695导入、切片、动画、Prefab、图集和Registry绑定入口。
- `Assets/_Game/Scripts/Editor/Art/T630ArtAssetAuthoring.cs`：让通用VFX图集纳入动画VFX目录，并避免通用静态Prefab流程重复处理动画资源。
- `Assets/_Game/Scripts/Presentation/`与`Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`：增加敌人死亡反馈语义、预热池、死亡位置快照播放及池化Animator重播复位。
- `Assets/_Game/Tests/EditMode/T695/`与`Assets/_Game/Tests/PlayMode/T695/`：动画资源合同、池化复用和生产击杀路径验证。
- `Assets/_Game/Tests/EditMode/T230/`、`T240/`、`T250/`、`T620/`、`T630/`与相关PlayMode测试：仅同步配置版本、Registry计数、反馈语义和资源导入断言。
- `artifacts/evals/T695/`：批次清单、导入计划、全量测试XML、视觉截图、验证与差异证据。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/ASSET_INTEGRATION.md`、`docs/DECISIONS.md`与`project-index.yaml`：任务状态、配置合同、资源计数、设计决定和项目索引。

明确排除用户已有`Design/Config/~$GameConfig.xlsx`删除状态、敌人生命/伤害/掉落/波次等玩法数值、场景和平台设置。
