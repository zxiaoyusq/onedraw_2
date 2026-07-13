# T500 Verification

- 结论：PASS。T500已完成配置驱动Level/Wave/Spawn时间轴、归一化出生区域、AllEnemiesDefeated/TimeElapsed/BossDefeated/PlayerConfirmed结束语义、maxAlive背压与暂停冻结；未提前实现T510或具体关卡内容接线。
- Git基线：`f3e511e65b9a8ebe68f1a162deccb54d715a6322`，分支`main`；开始时工作树干净，无用户未提交改动。预计与实际改动均在`change-whitelist.md`，测试产生的ProjectSettings临时差异已恢复。
- Unity追溯：`onedraw_2@272e911286835fad`，Unity `6000.5.1f1`，WebGL；最终强制Refresh/编译和Console检查为Error 0、Warning 0。
- 配置追溯：schema 4 / content 0.5.1-sample / hash `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`；配置只读导出与三生成物漂移PASS，ConfigExporter .NET 56/56。
- 专项：EditMode 8/8/0（`editmode-results.xml`）；PlayMode 2/2/0（`playmode-results.xml`）。全量：EditMode 142/142/0（`full-editmode-results.xml`）；PlayMode 35/35/0（`full-playmode-results.xml`）。四份Unity原生XML均通过仓库结果检查器。
- 玩法断言：3关/9波/13条Spawn展开35次；四种SpawnPattern均在配置归一化区域内稳定取点；拒绝出生不提交、maxAlive释放后重试；暂停60秒零推进；大delta不能跨PlayerConfirmed；TimeElapsed、AllEnemiesDefeated和BossDefeated均独立通过。
- 玩家路径：Bootstrap真实配置下教学关3波10怪完成一次，Boss关6个前置敌人+1个配置Boss完成一次；详见`player-path.md`。
- 文档：已同步`CONFIG_SCHEMA`、`DECISIONS`、`TEST_PLAN`、`TASKS`、`PROGRESS`与`project-index.yaml`；T500为DONE，T510为首个READY任务。
- 明确未做：未改xlsx/FieldDictionary/Schema/导出器/JSON/DTO，未改场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；未实现T510战斗流程、T520/T530/T540具体关卡、T600 HUD或T630正式资源。T500 PlayMode世界端口不是完整敌人池/玩家战斗接线，不能外推为成品单局。
