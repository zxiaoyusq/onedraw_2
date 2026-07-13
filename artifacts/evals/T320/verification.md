# T320 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T320；纯C# RDP、弧长重采样、长度、参考像素包围盒、面积、闭合与曲率，以及共享不可变几何结果和配置映射。
- 明确不做：不实现T330笔势分类、T340轨迹或T350命中；不修改T310采样合同、配置内容、场景、Prefab或平台状态。
- 分支/提交：`main` / `T320: implement stroke geometry`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无差异。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Input程序集几何设置/算法/不可变结果；Combat配置映射；T320 Edit/Play测试；任务索引、进度和证据。
- 用户已有改动保护：任务开始工作树干净；测试框架产生的EditorSettings临时差异已恢复；禁止目录最终0项。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；27个文件全部属于预计白名单，禁止目录0项、未暂存差异0项、敏感模式0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：`verify-config.sh --skip-unity`三生成物diff PASS、.NET 54/54；asmdef JSON、YAML、Unity `.meta`配对、唯一READY、禁止Runtime模式和`git diff --check`均PASS。
- EditMode：StrokeGeometry 12/12（MCP job `3b5804c64c8e4375be2026016cb7f27d`）；文档与任务状态同步后最终全量58/58（job `fbdffb0fb9b04bcda5735b6a763d10f3`）。
- PlayMode：StrokeGeometry 1/1（MCP job `609a6d996e9e4e2f93eff435f0fba9a3`）；全量14/14（job `53b956fa3adc4f579630bd414d5c3cbd`）。
- Console新增Error/Warning：最终真实玩家路径0 / 0。

## 玩家与平台证据

- 真实玩家路径和可断言值：真实Input System Mouse转向拖拽经T300/T310产生1份`StrokeGeometryData`，其strokeId、正长度、正包围盒、正总曲率和终止原因均可断言；Bootstrap正常进入MainMenu。
- 标准Web：NOT RUN（T320不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T320新增阻碍）。
- 真机：BLOCKED（既有T120门，非T320新增阻碍）。
- 截图/日志/产物：见`geometry-contract.md`、`unity-test-jobs.md`、`runtime-smoke.md`、`static-audit.md`、`regression-notes.md`和`config-verify.log`。

## 结论

- 已知问题：无T320新增产品问题；玩法阈值识别和视觉/命中共享消费分别保留在T330/T340/T350。
- 结论：PASS。
