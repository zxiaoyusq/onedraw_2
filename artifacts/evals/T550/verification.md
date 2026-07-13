# T550 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）。
- 任务与范围：T550；配置驱动最终评分/星级/奖励、幂等ResultService、ProgressSave v1与迁移端口、坏存档回退、Restart/NextLevel会话替换。
- 明确不做：云存档、付费货币、PlayerPrefs/微信存储适配、T600结算UI、微信DevTools/真机/打包；未恢复T120/T130。
- 分支/提交：`main`；本证据与实现同属原子提交`T550: complete result and progress loop`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`；主工程已被用户Editor实例打开，专项/分类/全量测试在同步当前Assets/Packages/ProjectSettings的`artifacts/tmp/T550-unity-project`隔离副本执行，未伪报主实例MCP结论。
- 配置Schema/内容版本/hash：schema `4` / content `0.5.5-sample` / `aa391c48c8c9478113937b2372cbc78ab90ee2f4448732ed0329068fddf25bb1`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：双工作簿及3项Global；受管JSON/hash/ConfigIds与冻结测试；只读`GetLevels()`；Levels程序集内4个纯C#结果/存档/导航文件；T550 EditMode/PlayMode；CONFIG_SCHEMA/DECISIONS/TEST_PLAN/TASKS/PROGRESS/project-index；证据与T550工作簿输出。未修改场景、Prefab、Registry、Packages、ProjectSettings或微信SDK。
- 用户已有改动保护：基线`e43cf73ee70141c5777a5da0b1cd341797223fb8`工作树干净；所有差异均由本任务产生。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；99个路径逐项属于白名单，`git diff --cached --check`通过，未暂存/未跟踪路径为0。

## 自动验证

- 静态/导出校验：工作簿29 Sheet全部渲染并视觉复核，公式错误0；正式源/镜像/输出SHA-256均为`fb4033d5cdf4dddf7d90800f300f25066ae950b58377029fd92a75f8feb580f2`。`verify-config.sh --skip-unity`完成构建0 warning/0 error、严格生成/三产物漂移门与.NET 56/56；JSON 182,404字节/695条、311个主索引/56个组索引、27组347个ID。静态扫描未发现PlayerPrefs、微信SDK静态调用、`Task.Run`或自建线程；`git diff --check`通过。
- EditMode XML：T550专项 12 / 12 / 0（`editmode-results.xml`）；ConfigPipeline 19 / 19 / 0（`config-editmode-results.xml`）；全量 174 / 174 / 0（`full-editmode-results.xml`）。
- PlayMode XML：T550专项 1 / 1 / 0（`playmode-results.xml`）；ConfigPipeline 3 / 3 / 0（`config-playmode-results.xml`）；全量 42 / 42 / 0（`full-playmode-results.xml`）。
- Console新增Error/Warning：最终通过日志中无`error CS`、未处理异常或失败测试；无T550新增编译warning。项目既有SDK/异常Source警告未被本任务改动或冒充为新增问题。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载正式配置；教程关以战斗分2000、弹反2、未受伤、用时120.9秒结算为4480分/2星，按表解锁`lv_002_cave`并增加100非付费积分。随后同一导航入口连续Restart 3次再NextLevel；generation为5，共5个会话全部Dispose，旧Marker GameObject均销毁，活动会话和池租约最终为0。
- 标准Web：NOT RUN（T550不改构建/平台层）。
- 微信转换：NOT RUN（按用户要求继续绕过T120与微信打包）。
- DevTools：NOT RUN（仍属T120既有阻塞范围）。
- 真机：NOT RUN（仍属T120/T750平台验收范围）。
- 截图/日志/产物：`workbook-before/`、`workbook-after/`、`config-pipeline-partial.log`、6组Unity XML/日志；可交付工作簿`outputs/T550/GameConfig.xlsx`。

## 结论

- 已知问题：`IProgressSaveStore`只定义端口，Editor/Web/WeChat具体持久化适配仍按执行顺序留给T130；T600结算HUD尚未实现。这两项均为明确后续范围，不阻塞T550规则与生命周期验收。
- 结论：PASS。
