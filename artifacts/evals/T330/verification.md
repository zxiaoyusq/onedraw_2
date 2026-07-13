# T330 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T330；配置驱动识别Any/横/竖/斜/弧/圆/蓄力，输出确定性置信度与几何摘要，并补齐首个有效采样前的真实停留时长和只读规则全表映射。
- 明确不做：不实现T340轨迹、T350命中、机器学习或书法级精度；不修改配置内容、场景、Prefab或平台状态。
- 分支/提交：`main` / `T330: implement gesture classification`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无漂移。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Input笔势规则/分类/结果和首段停留元数据；Config只读StrokeRules全表；Combat显式映射；T330 Edit/Play测试；任务索引、进度和证据。
- 用户已有改动保护：任务开始工作树干净；Unity Test Runner临时改写的EditorSettings字段每轮均恢复，最终禁止目录0项。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；37个文件全部属于预计白名单，禁止目录0项、未暂存差异0项、`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：`verify-config.sh --skip-unity`三生成物diff PASS、.NET 54/54；YAML/asmdef、`.meta`配对、唯一READY、程序集边界、禁止Runtime模式和Git白名单审查均PASS。
- EditMode：GestureClassifier 14/14（MCP job `6c5af262f9ef4b38b534f951dac93d02`）；文档与任务状态同步后最终全量72/72（job `4071bc06c6ac4f6b9ebaee719da859c1`）。MCP Test Runner结果路径为`~/Library/Application Support/DefaultCompany/onedraw_2/TestResults.xml`，摘要固化于`unity-test-jobs.md`。
- PlayMode：GestureClassifier 1/1（job `efdea89322cf4906964b4ac87b06d7b0`）；最终全量15/15（job `5b4791f1d84e475587a5505a2a666179`）；隔离详细玩家路径1/1（job `5c87b853b3c640c78f3959cac882374a`）。
- Console新增Error/Warning：脚本Refresh/编译隔离检查0/0；T330玩家路径无业务Error/Warning。MCP Test Runner自身固定记录1条`Saving results`（Exception标签）和1条`IPostBuildCleanup` Warning，未来自游戏代码，见`regression-notes.md`。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载28表645条配置、76键Registry和1920×1080统一输入后进入MainMenu；真实Input System Mouse水平拖拽经配置采样、配置几何和七条配置规则只发布一次`stroke_horizontal`，长度90～100、角度0、曲率0且置信度0.5～1。
- 标准Web：NOT RUN（T330不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T330新增阻碍）。
- 真机：BLOCKED（既有T120门，非T330新增阻碍）。
- 截图/日志/产物：见`gesture-contract.md`、`unity-test-jobs.md`、`runtime-smoke.md`、`static-audit.md`、`regression-notes.md`、`config-verify.log`；初次批处理因前台Editor占用而未执行测试的原始日志保留为`editmode-gesture-unity.log`。

## 结论

- 已知问题：无T330新增产品问题；真实微信触摸继续受T120/T640门约束，轨迹和命中分别保留给T340/T350。
- 结论：PASS。
