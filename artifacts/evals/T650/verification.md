# T650 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T650；以T520教程事件为唯一推进真相，实现配置驱动的教程遮罩、手势示意、高亮、显式跳过/回看和一次性完成标记。
- 明确不做：不实现T640/T700及后续任务；不修改教程动作、关卡、波次或战斗数值；不修改Scene/Prefab/Registry/Input Actions/Packages/ProjectSettings/微信SDK/Builds；不恢复用户延期的T120/T130。
- 分支/提交：`main`；任务提交`T650: add event-driven tutorial overlay`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与执行路径：`6000.5.1f1`；主工程由已打开Editor持有，测试用`artifacts/tmp/T600-unity-project`隔离同步副本执行，避免第二实例锁冲突。
- 配置Schema/内容版本/hash：`5` / `0.6.2-sample` / `7e2a0880c289b4dc7299dee0149bfe2bcc86ed55fa92fa392e5cd874ad77b91e`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：受管配置与三份工作簿；教程Skip/延迟战斗门；存档v2/目录验证；`TutorialDirector`/遮罩/程序化手势/运行时工厂；T650专项测试与受影响冻结值；合同文档、索引和本证据目录。全部属于预计白名单。
- 用户已有改动保护：基线工作树干净，无用户未提交改动；未触及白名单禁止区。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；仅暂存T650白名单文件，无Scene/Prefab/Registry/Input Actions/Packages/ProjectSettings/SDK/Builds差异。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity` PASS；ConfigExporter .NET 58/58；严格只读三生成物漂移0。工作簿30个Sheet前后渲染并复核，公式错误0；正式源、镜像与交付副本SHA-256均为`1278124703f32c4d6b5c3ac017984e148b0fb3ab5378d9fba04c47236620ffa2`。
- EditMode XML：专项3/3/0（`editmode-results.xml`）；ConfigPipeline 19/19/0（`affected-editmode-results.xml`）；全量195/195/0（`full-editmode-results.xml`）。
- PlayMode XML：专项1/1/0（`playmode-results.xml`）；Metal截图路径1/1/0（`playmode-screenshot-results.xml`）；全量46/46/0（`full-playmode-results.xml`）。
- Console新增Error/Warning：0。批处理起始时License Client IPC首次握手失败/令牌尚不可用，但同一进程随后取得授权并运行完整测试，不是Unity Console产品错误。Metal专项重报T610既知的4处CS0618测试API warning；T650无新编译warning。

## 玩家与平台证据

- 真实玩家路径和可断言值：从Bootstrap/MainMenu加载真实配置，创建真实BattleHUD/教程遮罩；首次显示“划过妖怪即可攻击”、Any/横向手势、BattleArea高亮和“继续战斗”按钮，无overflow/truncate。点击跳过后步骤序列Completed但本关未结束，实际击败15怪后Victory；重开读取完成标记自动跳过展示，仍实际击败15怪后第二次Victory。进度存储总写入次数=1，回看不改变序列状态。详见`player-path.md`。
- 标准Web：NOT RUN（用户明确延期平台/打包工作，T650不需新建Web产物）。
- 微信转换：NOT RUN（同上）。
- DevTools：BLOCKED（延续T120已知缺少开发者工具）。
- 真机：BLOCKED（延续T120已知缺少开发者工具和可用设备路径）。
- 截图/日志/产物：`tutorial-overlay-1920x1080.png`，1920×1080 RGB/Metal，SHA-256 `86587e44620e6064e197cb4bb7b3fd1ad159b33a0af6b8d3a4531c6f8603599f`；配置日志、NUnit XML与Unity日志均在本目录。交付工作簿为`outputs/T650/GameConfig.xlsx`。

## 结论

- 已知问题：License Client批处理起始IPC首次握手报错但随后授权成功；图形专项保留T610既知CS0618测试API warning。均未造成编译/测试失败或产品运行异常。T640多比例、T130平台持久化、Web/微信DevTools/真机均不属于T650 PASS范围。
- 结论：PASS。T650验收条件完成；T700为下一个READY任务。
