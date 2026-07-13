# T510 Verification

## 追溯

- 日期：2026-07-14。
- 任务与范围：实现配置驱动`Countdown/Playing/UltimateDrawing/Paused/Victory/Defeat`状态机、玩家事件门、一次性互斥结算、统一时间源及T500协调器。
- 明确不做：未实现T520/T530/T540关卡内容、T550结果存档、T600 HUD、T620演出、T630正式资源或任何微信平台工作；未修改场景/Prefab。
- 分支/提交：`main`；提交信息`T510: implement deterministic battle flow`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`，WebGL，`onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 4 / content 0.5.1-sample / `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Levels新增`BattleFlow`与协调器；新增T510 EditMode/PlayMode测试；同步CONFIG_SCHEMA/DECISIONS/TEST_PLAN/TASKS/PROGRESS/project-index及T510证据。未触发条件性Actors/Skills/Input/asmdef或配置文件改动。
- 用户已有改动保护：开始时工作树干净；PlayMode临时把`EditorSettings.enterPlayModeOptions`改为禁用域重载，已通过Unity Editor API恢复为基线`None`，最终ProjectSettings无diff。
- `git diff --check`：PASS。
- 暂存白名单审查：全部实际文件属于白名单；场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、SDK和Builds无暂存diff。

## 自动验证

- 静态/导出校验：产品Levels无内容ID、MonoBehaviour/Unity Time/线程/xlsx依赖；project-index YAML解析通过；配置只读临时导出与三生成物逐字节一致，ConfigExporter构建0 warning/0 error，.NET 56/56。
- EditMode XML：专项8 / 通过8 / 失败0 / `editmode-results.xml`；全量150 / 通过150 / 失败0 / `full-editmode-results.xml`。
- PlayMode XML：专项2 / 通过2 / 失败0 / `playmode-results.xml`；全量37 / 通过37 / 失败0 / `full-playmode-results.xml`。
- Console新增Error/Warning：最终强制Refresh、编译和域重载后Error 0 / Warning 0。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap真实配置路径完成2秒Countdown；FocusLost/ApplicationPaused叠加暂停30秒时流程/战斗/关卡delta均为0，全部解除后恢复Playing。终极2.5秒边界仍等待，超过0.000001秒只取消、能量仍100、成功0；单调有效Circle事件成功1次、能量100→0；0.25倍持续0.8秒得到0.2战斗delta。旧gestureEventId重放被拒绝；同帧死亡/到时/完成只结算一次Defeat。
- 标准Web：NOT RUN（T510不要求构建）。
- 微信转换：NOT RUN（按用户决定继续绕过T120/T130）。
- DevTools：BLOCKED（沿用T120缺少微信开发者工具，不作为T510失败）。
- 真机：BLOCKED（沿用T120缺少设备与G3，不作为T510失败）。
- 截图/日志/产物：`player-path.md`、`static-audit.md`、`config-verification.md/.log`、`unity-mcp-jobs.md`及四份Unity原生NUnit XML。

## 结论

- 已知问题：首轮专项暴露配置float到double的极小尾差与测试断言容差问题，已在统一时间源边界归零并全量回归；无遗留T510缺陷。前台Editor占用工程时批处理入口被Unity正常拒绝，最终改由同一Unity Test Runner API经MCP执行并归档原生XML。
- 结论：PASS。T510验收全部满足，T520成为首个依赖完成的READY任务。
