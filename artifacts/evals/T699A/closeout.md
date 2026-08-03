# T699A Closeout

## 起始状态

- 任务开始时间：2026-08-01（Asia/Shanghai）。
- 起始提交：`f26f937d T699: fix enemy contact damage and approach`。
- 起始Git状态仅有用户保留改动：` D Design/Config/~$GameConfig.xlsx`；本任务不恢复、不删除、不暂存该路径。
- 当前缺口：生产远程攻击只累计不可见计数，没有真实弹体、移动、碰撞扣血或路径相关的画笔交互。

## 预计改动白名单

- 权威配置与同步镜像：`Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`。
- 配置生成物：`Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`。
- 配置冻结测试：`Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`Tools/ConfigExporter/Tests/ConfigPipelineE2ETests.cs`。
- 生产运行时：`Assets/_Game/Scripts/Actors/EnemyStrategyRuntime.cs`、`Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`、`Assets/_Game/Scripts/Bootstrap/ProductionBattleWorld.cs`，以及按职责拆出的T699A新脚本与Unity `.meta`。
- 接口受影响测试夹具：`Assets/_Game/Tests/PlayMode/T430/`、`T450/`、`T460/`、`T530/`、`T540/`、`T690/`内实现`IEnemyAttackWorld`的文件。
- 配置快照测试：`Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`、`Assets/_Game/Tests/EditMode/T370/ProjectileCutTests.cs`。
- 全量验证发现的冻结快照同步：`Assets/_Game/Tests/PlayMode/T230/RuntimeConfigBootstrapPlayModeTests.cs`、`T240/AssetRegistryBootstrapPlayModeTests.cs`、`T370/ProjectileReflectPlayModeTests.cs`、`T600/HudBindingPlayModeTests.cs`、`T610/LocalizationGlyphPlayModeTests.cs`、`T650/TutorialSkipPlayModeTests.cs`。这些文件只同步本任务生成的content/hash与弹速位移期望。
- T699A测试：`Assets/_Game/Tests/EditMode/T699A/`、`Assets/_Game/Tests/PlayMode/T699A/`及Unity `.meta`。
- 文档与索引：`docs/TASKS.md`、`docs/PROGRESS.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`、`project-index.yaml`、本文件及生产Game视图PNG。

## 验证摘要

- 配置：schema 6 / content `0.6.7-sample` / content hash `e0dabca95f0d20cc86bdcf3eb83e56db90bc2bebb513631f708a7d28a48b489d`；30表/763条、29组381个ID常量。
- 工作簿：权威源与模板镜像均98,273字节、SHA-256 `620ca6aaa9af1012f2d2f4e059663b91a3b88fa9996e49f40bd046ef8183c29f`，`cmp`字节一致；31个Sheet已渲染复核，公式错误扫描为0。
- 配置门：ConfigExporter 60/60，通过受管JSON/hash/ConfigIds只读verify与样例diff；JSON为200,340字节，文件SHA-256 `22c758a5f89375336b7a9b3d7209bd96b0ddd7ac0c9fae8d43b1fe04541b40d5`。
- 聚焦测试：T699A EditMode 5/5；T699A PlayMode 2/2。分别覆盖五类弹速/寿命/航程合同，以及可见慢速移动、碰撞前不扣血、命中扣血、真实InputSystem画笔击落和池化回收。
- 最终全量：EditMode 215/215（4.03秒）；PlayMode 59/59（55.14秒）。首次真实全量PlayMode的6项失败均为旧content/hash/位移冻结期望，按生成快照同步后通过；两次域刷新后的Test Framework孤儿初始化在`PlayModeRunTask`空引用且0/59未执行，清理框架任务后完成有效测试。
- 目视：从Bootstrap进入MainMenu与教程Battle，Game视图可见敌人与玩家之间的蓝白`proj_ghost_fire`；运行时查询记录位置`(420.4, 572.2)`、速度180参考像素/秒。截图`production-projectile-visible.png`的SHA-256为`08f36eddfc54a7cfa560055fbeb9947840f7d61bef10034dab2d5ae1333898a0`。
- 范围：未修改Scene、Prefab、Registry、ProjectSettings、敌人/关卡/波次/出生点配置或投射物资产绑定；T700未提前实施。

## 用户改动保护

- `Design/Config/~$GameConfig.xlsx`的起始删除状态持续保留并排除在提交外。
