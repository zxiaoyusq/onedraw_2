# T400 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T400；配置驱动的玩家HP、当前能量、刀/符架势、切换冷却、即时效果意图和一次性死亡战斗事件。
- 明确不做：不实现T410技能CD/EffectGroup执行、T420敌人HP/状态机、T510战斗流程、T600 HUD或自由移动；不修改场景、Prefab、配置内容、Packages、ProjectSettings或微信SDK。
- 分支/提交：`main`；计划任务提交`T400: implement configured player combat state`（本报告随该提交入库）。
- 任务开始Git基线：`b5b86678869f1badd5b38e12cb2749e95134c805`；见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1` / `onedraw_2@272e911286835fad`
- 配置Schema/内容版本/hash：2 / 0.2.0-sample / `19dc788f890f995adb94458f74894b89514f85f3bfc9429659ddd2421a72f733`

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Actors程序集中的4个玩家战斗运行时文件及Unity `.meta`；EditMode/PlayMode测试程序集增加Actors引用；T400两个专项测试类；配置语义/决策/任务/进度/索引文档与本证据目录。
- 用户已有改动保护：任务开始工作树干净；所有产品改动均在预先记录白名单内。Test Runner把`EditorSettings.enterPlayModeOptions`改为1的副作用已通过Unity Editor API恢复为基线`None`，最终无`ProjectSettings`差异。
- 配置内容：未修改双xlsx、Schema、FieldDictionary、导出器、受管JSON/hash/`ConfigIds.g.cs`；配置版本/hash保持不变。
- `git diff --check`：PASS；最终28个文件全部属于`change-whitelist.md`，无配置生成物/场景/Prefab/Packages/ProjectSettings/微信SDK差异；暂存后复核无未暂存文件。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity`退出0；生成三件套漂移0，ConfigExporter构建0 warning/0 error，.NET 55/55；详见`config-static.log`。脚本按D-017明确输出PARTIAL，Unity层由同一打开Editor的MCP job执行。
- EditMode：专项8/8；实现完成后及状态/证据同步后全量均为106/106。打开Editor路径不生成仓库内NUnit XML，不伪报XML路径。
- PlayMode：专项2/2；实现完成后及状态/证据同步后全量均为27/27。打开Editor路径不生成仓库内NUnit XML，不伪报XML路径。
- Console新增Error/Warning：最终脚本刷新编译后0/0；job与玩家路径见`unity-mcp-jobs.md`。

## 玩家与平台证据

- 真实玩家路径和可断言值：PASS；Bootstrap→MainMenu配置加载→运行时PlayerCombatController。刀→符后轨迹18→28参考像素、伤害公式默认刀→符、切弹倍率0.8→1.4，`proj_seal_bolt`由架势不匹配变为反弹；发布`fx_switch_to_talisman`意图，冷却内回切无事件；T360能量进入上限并按Skill行扣除，同帧重复致死只发布一次死亡。
- 标准Web：NOT RUN（T400纯状态与Editor玩家路径不要求新构建）
- 微信转换/DevTools/真机：NOT RUN（按用户决定延期T120/T130，既有阻塞不变）
- 截图/日志/产物：`player-combat-contract.md`、`config-static.log`、`unity-mcp-jobs.md`；本任务为状态/规则测试，不伪造视觉截图。

## 结论

- 已知问题：无T400新增产品问题；既有微信DevTools/真机、SDK和Web问题保持原状态。
- 结论：PASS
