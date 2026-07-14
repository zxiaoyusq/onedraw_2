# T600 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）
- 任务与范围：T600；实现生命、能量、连斩、实时评分、架势、终极、暂停及胜负结算HUD，包含只读状态投影、Presenter按钮门、uGUI/TMP View、运行时Canvas工厂和Safe Area根。
- 明确不做：不实现T610字体/fallback、T620战斗反馈、T630正式美术、T640完整多比例/左右手适配、T650教程遮罩；不修改场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds，不恢复T120/T130平台工作。
- 分支/提交：`main`；提交信息`T600: implement battle HUD and result UI`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`。主工程Editor已被交互实例占用，使用`artifacts/tmp/T600-unity-project`隔离副本执行同版本Unity BatchMode导入、编译与测试；未使用MCP，不伪造MCP结果。
- 配置Schema/内容版本/hash：schema `4` / content `0.6.0-sample` / `54885fb2ce8373bad21af796d96a7a4cbc4ce6d8f41def3f909686b14ec87a1d`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增`BattleHudContracts/StateBinding/Presenter/View/ViewFactory`及T600 EditMode/PlayMode；为Combo/Score/Result增加只读通知；同步Presentation/Test asmdef；工作簿新增20条Texts并同步JSON/hash/ConfigIds/sample和0.6.x兼容线；更新冻结测试、任务/进度/配置/决策/测试文档及索引。
- 用户已有改动保护：开始时`main@71d7e939690e8debe6b6721a8b58e9eff82b881b`工作树干净；所有差异由T600产生，未覆盖用户已有改动。
- `git diff --check`：PASS。
- 暂存白名单审查：提交前逐项对照`change-whitelist.md`，无禁止路径；最终暂存差异已审查。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity` PASS；三生成物无漂移，ConfigExporter构建0 warning/0 error，.NET 56/56。正式源、镜像和`outputs/T600/GameConfig.xlsx`字节一致，SHA-256 `4429caa0846bd4da207380d37ffadca4ab48adb6dd43c73b5691fbcc18457f13`；29个Sheet全部渲染并逐张复核，公式错误0。
- EditMode XML：专项6 / 通过6 / 失败0 / `artifacts/evals/T600/editmode-results.xml`；全量180 / 通过180 / 失败0 / `artifacts/evals/T600/editmode-full-results.xml`。
- PlayMode XML：专项1 / 通过1 / 失败0 / `artifacts/evals/T600/playmode-results.xml`；全量43 / 通过43 / 失败0 / `artifacts/evals/T600/playmode-full-results.xml`。
- Console新增Error/Warning：最终专项与全量日志无编译错误、运行异常、Native Crash或新增Warning。一次全量EditMode启动在Burst本地缓存线程发生Unity原生崩溃；清理隔离副本Burst缓存后按相同命令重跑180/180通过，最终证据日志无崩溃。

## 玩家与平台证据

- 真实玩家路径和可断言值：`HudBindingPlayModeTests`从Bootstrap真实配置进入MainMenu后创建实际Canvas；断言HP/能量`100 / 100`、连斩4、评分521、架势`Demon Blade`，终极与暂停意图各转发一次；1920×1080屏幕上的`Rect(100,50,1720,980)`映射为精确归一化锚点；暂停显示主菜单；Victory显示4480分、2/3星和`Demon Score: +100`，Restart与NextLevel各转发一次，Dispose后一帧Canvas根销毁。
- 标准Web：NOT RUN（T600不要求构建；历史基线不外推为本次结果）。
- 微信转换：NOT RUN（用户已要求绕过T120/微信打包任务）。
- DevTools：BLOCKED（未安装微信开发者工具，且本任务明确不恢复平台门）。
- 真机：BLOCKED（缺DevTools/设备路径，本任务不伪造真机结论）。
- 截图/日志/产物：工作簿前后主Sheet及29 Sheet全量渲染位于`workbook-before/`、`workbook-after/`；配置门为`config-verification.log`；Unity专项/全量XML和日志均在本目录；工作簿交付副本为`outputs/T600/GameConfig.xlsx`。

## 结论

- 已知问题：T600使用TMP默认字体进行英文头less验证；中文字体、fallback、缺字与裁切截图归T610。当前Battle场景仍是灰盒且没有完整生产关卡组合根，HUD通过可组合运行时入口与Bootstrap真实配置装配验证；正式战斗组合接线不能由本任务虚构。T640仍需完成多比例、左右手和触控遮挡矩阵。
- 结论：PASS。
