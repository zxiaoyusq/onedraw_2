# T110 Verification

## 追溯

- 日期：2026-07-13。
- 任务与范围：确认当前官方微信Unity转换SDK，固定来源/版本/commit/许可证，建立Unity 6000.5.1f1兼容矩阵并完成导入编译。
- 明确不做：未执行G2转换、G3 DevTools或G4真机；未迁移Unity；未接Gameplay平台API。
- 分支/提交：`main`；提交在本任务验证后创建。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`，基线 `6e4fe22a40da7ed3883c68fa0e2587cbae8a9326`，无用户已有改动。
- Unity精确版本与MCP实例：`6000.5.1f1 (0d9463e84828)`；导入时实例 `onedraw_2@272e911286835fad`。
- SDK：官方 `minigame-tuanjie-transform-sdk` v0.1.33，commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`，上游tree `22b3686bdf558fa34c7d204c381281c721fe0b70`。
- 配置Schema/内容版本/hash：未改配置；schema 1 / content 0.1.0-sample / `ed8ab5789586c1e6b5c82b9f3185052f408cd61b53126cd8ec81b917c738756a`。

## 改动审查

- 预计白名单：见`change-whitelist.md`；原始CS0619出现后已先补充embedded包和BUGS范围。
- 实际改动：固定manifest来源；embedded官方SDK快照；一处版本条件兼容补丁；vendor目录whitespace属性例外；T110测试、平台/上游/兼容文档和证据。
- 上游差异：Unity embed未复制`.gitignore`，格式化`package.json`并加入`_fingerprint`；人工逻辑差异仅`WXRuntimeExtDef.cs`，另加包内`UPSTREAM.md`及Unity `.meta`。
- 用户已有改动保护：任务开始无用户改动；未修改Gameplay、场景、Prefab、配置或T100产物。
- `git diff --check`与`git diff --cached --check`：PASS；SDK原始whitespace通过仅限vendor目录的`.gitattributes`规则保留。
- 暂存白名单审查：PASS；682个暂存路径均属于补充后的白名单，无未暂存或非忽略未跟踪文件。

## 自动验证

- 来源/完整性：官方组织、README、发布分支、commit、tree、MIT与随包第三方许可证已审查；详见`upstream-source.md`、`license-summary.log`和`embedded-integrity.log`。
- 原始导入：UPM精确commit解析PASS；未修改上游编译FAIL，根因CS0619，`WxEditor`缺失为级联；见`sdk-import-original-errors.log`。
- 补丁后导入：完整domain reload；Unity MCP Console Error/Exception 0；SDK Runtime/Editor程序集加载。
- 专项EditMode：2/2 PASS（MCP）。
- EditMode XML：10 / 10 / 0 / `artifacts/evals/T110/editmode-results.xml`。
- PlayMode XML：2 / 2 / 0 / `artifacts/evals/T110/playmode-results.xml`。
- Console新增Error/Warning：补丁后Error/Exception 0；6个唯一SDK warning登记BUG-0004。

## 玩家与平台证据

- 真实玩家路径和可断言值：PlayMode回归验证Bootstrap自动进入MainMenu；2/2通过。
- 标准Web：沿用T100 `PASS WITH KNOWN ISSUES`，本任务NOT RUN新构建。
- 微信转换：NOT RUN（T120）。
- DevTools：NOT RUN（T120）。
- 真机：NOT RUN（T120）。
- 截图/日志/产物：无截图需求；XML、来源快照、原始错误、补丁diff、许可证及摘要均在本目录。

## 结论

- 已知问题：BUG-0004；上游对Unity 6仍有测试版限定；embedded包约138,288,489文件字节；G2～G4未执行。
- 结论：PASS WITH LOCAL PATCH。T110验收完成，只把T120置为READY，不宣称微信运行通过。
