# T420 Verification

## 追溯

- 日期：`2026-07-14`
- 任务与范围：T420；实现配置驱动的通用敌人定义、状态机、Damageable、Weakpoint、Buff容器、T360/T370/T410伤害入口与生命周期事件。
- 明确不做：T430策略注册表/Telegraph、T440通用对象池、T450敌人内容装配、T460 Boss阶段、T510战斗流程、场景/Prefab接线和微信平台任务。
- 分支/提交：`main`；本证据随原子提交`T420: implement configured enemy runtime state`落库。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1` / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：`3` / `0.3.0-sample` / `ef7eec3aa29dffb593164526d50eff867e05fabb09fdbcbfc4347d620fb7b3c2`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增Actors通用敌人运行时、Skills敌人目标适配、T420 EditMode/PlayMode测试；同步配置运行时语义、架构决定、任务/进度/索引与证据。详见`static-audit.md`。
- 用户已有改动保护：任务开始工作树干净；测试运行器产生的`ProjectSettings/EditorSettings.asset`临时差异已恢复，最终只保留白名单内容。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；仅暂存T420白名单文件，`git diff --cached --check`、name-status与stat将在提交前复核。

## 自动验证

- 静态/导出校验：PASS；`Tools/CI/verify-config.sh --skip-unity`确认ConfigExporter构建0警告/0错误、56/56测试和三受管产物0漂移，详见`config-static.log`。
- EditMode XML：MCP未导出XML；T420 7/7、全量117/117，Job见`unity-mcp-jobs.md`。
- PlayMode XML：MCP未导出XML；T420 1/1、全量29/29，Job见`unity-mcp-jobs.md`。
- Console新增Error/Warning：最终Refresh编译与清理后`0/0`。

## 玩家与平台证据

- 真实玩家路径和可断言值：PASS；真实Mouse斜划命中`boss_tomb_king`弱点，目标`42001`、护甲`120→119`、打断成功并进入Stun，详见`player-path.md`。
- 标准Web：NOT RUN（不属于T420，且用户要求继续主内容）。
- 微信转换：NOT RUN（T120/T130按用户要求延期，保持既有状态）。
- DevTools：NOT RUN（同上；本机仍缺工具）。
- 真机：NOT RUN（同上；无可用工具/设备证据）。
- 截图/日志/产物：`config-static.log`、`static-audit.md`、`unity-mcp-jobs.md`、`player-path.md`。

## 结论

- 已知问题：T420范围内无。平台阻塞保持既有记录，不外推或伪造平台PASS。
- 结论：PASS。
