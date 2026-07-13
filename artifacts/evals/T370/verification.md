# T370 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T370；配置驱动的敌方投射物切断、不可切断、架势门、反弹归属/伤害追溯、确定性运动、T350 Stroke命中适配与完整单体回收复用。
- 明确不做：不实现T400玩家HP/能量/架势状态、T420敌人HP/状态机、T430攻击策略或T440通用对象池；不修改场景、Prefab、配置工作簿/Schema/生成物、Packages、ProjectSettings或微信SDK。
- 分支/提交：`main`；计划提交`T370: implement configured projectile interactions`（本报告随任务提交入库）。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1` / `onedraw_2@272e911286835fad`
- 配置Schema/内容版本/hash：2 / 0.2.0-sample / `19dc788f890f995adb94458f74894b89514f85f3bfc9429659ddd2421a72f733`

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：6个`Projectile*`运行时文件及Unity生成meta；T370 EditMode/PlayMode测试；配置语义/决策/任务/进度/索引文档与本证据目录。
- 用户已有改动保护：任务开始工作树干净；全部产品改动在预先记录白名单内。Test Runner产生的EditorSettings副作用已通过Unity Editor API恢复，最终无ProjectSettings差异。
- `git diff --check`：PASS；暂存前与暂存后均无空白错误。
- 暂存白名单审查：PASS；29个文件全部属于`change-whitelist.md`，无未暂存文件，无配置生成物/场景/Prefab/Packages/ProjectSettings/微信SDK改动。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity` PASS到静态层；生成三件套漂移0，ConfigExporter构建0 warning/0 error，.NET 55/55。脚本按D-017明确输出`PARTIAL`，Unity层由同一打开Editor的MCP job执行，不伪报完整脚本PASS。
- EditMode：专项8/8；状态同步后全量98/98；打开Editor路径不生成仓库内XML，job见`unity-mcp-jobs.md`。
- PlayMode：专项2/2；状态同步后全量25/25；打开Editor路径不生成仓库内XML，job见`unity-mcp-jobs.md`。
- Console新增Error/Warning：最终清空并复查0/0；全量中的既有负向配置错误及Test Runner固定消息已在`unity-mcp-jobs.md`单独说明。

## 玩家与平台证据

- 真实玩家路径和可断言值：PASS；Mouse横划→T300输入→T310采样→T320几何→T330 Horizontal→T350真实CircleCast命中`ProjectileHitTarget`→T370反弹。`proj_ghost_fire`由Enemy/7001切为Player/101，方向左→右，0.5秒移动130参考像素，命中Enemy/7001时发布表内8伤害且保留原始敌方来源；随后同对象以`proj_rockfall`/Enemy9001/target5002/半径34复用且无旧状态。
- 标准Web：NOT RUN（T370不要求新构建）
- 微信转换：NOT RUN（按用户决定延期T120/T130）
- DevTools：NOT RUN（按用户决定延期且本机缺工具）
- 真机：NOT RUN（按用户决定延期且无设备证据）
- 截图/日志/产物：`projectile-contract.md`、`unity-mcp-jobs.md`；本任务为规则/物理交互测试，不伪造视觉截图。

## 结论

- 已知问题：无T370新增产品问题；首次PlayMode错误断言及修正有完整记录。既有微信DevTools/真机、SDK和Web问题保持原状态。
- 结论：PASS
