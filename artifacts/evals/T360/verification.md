# T360 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T360；配置驱动的伤害、方向/弱点奖励、连斩、评分与能量收益纯规则，以及Mouse→多目标结算玩家路径。
- 明确不做：不实现T370投射物、T400玩家HP/当前能量/架势切换、T420敌人状态/HP/弱点窗口；不修改场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK。
- 分支/提交：`main`；计划任务提交`T360: implement configured combat resolution`（本报告随该提交入库）。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：`6000.5.1f1` / `onedraw_2@272e911286835fad`
- 配置Schema/内容版本/hash：2 / 0.2.0-sample / `19dc788f890f995adb94458f74894b89514f85f3bfc9429659ddd2421a72f733`

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：双工作簿、Schema/FieldDictionary/样例/DTO/生成物/兼容测试；7个Combat纯规则文件；2个EditMode测试类、1个PlayMode玩家路径；配置/任务/进度/索引/决策文档与证据。
- 用户已有改动保护：任务基线已跟踪文件干净；只修改白名单。Unity Test Runner造成的EditorSettings副作用已用Editor API恢复，最终无ProjectSettings diff。
- `git diff --check`：PASS；暂存前与暂存后均无空白错误。
- 暂存白名单审查：PASS；共54个文件，全部属于`change-whitelist.md`，无未暂存文件，无ProjectSettings/场景/Prefab/Packages/微信SDK改动。

## 自动验证

- 静态/导出校验：PASS；生成物只读diff PASS；.NET 55/55；schema 2/content 0.2.0；28表647条；JSON 168,862字节；日志`config-static.log`。由于工程已有交互Editor，脚本按D-017使用`--skip-unity`，Unity分类由同一Editor的MCP job独立执行。
- EditMode：专项12/12、ConfigPipeline 19/19、全量90/90；MCP job与逐项专项结果见`unity-mcp-jobs.md`。打开Editor路径不生成仓库内NUnit XML，不伪报XML路径。
- PlayMode：专项1/1、ConfigPipeline 3/3、全量23/23；MCP job与真实启动日志见`unity-mcp-jobs.md`。打开Editor路径不生成仓库内NUnit XML，不伪报XML路径。
- Console新增Error/Warning：最终0；负向配置测试的预期schema拒绝日志已单独标注。

## 玩家与平台证据

- 真实玩家路径和可断言值：PASS；Mouse横划→T300输入→T310采样→T320几何→T330 Horizontal→T350按路径命中两个Collider2D→T360结算。目标101弱点=48伤害/398分/11能量；目标202为第2连斩=13/123/3；累计61/521/14，连斩数2。
- 标准Web：NOT RUN（T360纯规则与Editor玩家路径不要求新构建）
- 微信转换：NOT RUN（按用户决定延期T120/T130）
- DevTools：NOT RUN（按用户决定延期且本机仍缺工具）
- 真机：NOT RUN（按用户决定延期且无设备证据）
- 截图/日志/产物：`workbook-validation.md`、`config-static.log`、`unity-mcp-jobs.md`；29表渲染位于忽略目录`artifacts/tmp/T360-spreadsheet/previews-after/`。

## 结论

- 已知问题：无T360新增已知问题；既有微信DevTools/真机、SDK和Web问题保持原状态。
- 结论：PASS
