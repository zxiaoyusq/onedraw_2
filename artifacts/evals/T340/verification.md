# T340 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T340；同一T320点集驱动的低分配LineRenderer轨迹、配置刀/符宽度和VFX寿命/池/排序、淡出、最多三条活动残留及完整池重置。
- 明确不做：不实现T350命中或碰撞，不让视觉决定分类/结果，不创建逐段材质，不修改配置内容、场景、Prefab或平台状态。
- 分支/提交：`main` / `T340: implement pooled stroke trails`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无漂移。

## 改动审查

- 预计白名单：见`change-whitelist.md`；发现配置Sorting Layer缺失后先扩展到单一TagManager文件，再经Unity Editor API修改。
- 实际改动：Combat只读点集桥、Presentation设置/映射/视图/池、T340 PlayMode、测试程序集Presentation引用、VFX Sorting Layer、任务索引/进度和证据。
- 用户已有改动保护：开始工作树干净；Test Runner的EditorSettings临时差异每轮恢复；无用户文件被覆盖。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；28个文件全部属于预先记录/经发现扩展的白名单，仅ProjectSettings/TagManager.asset为配置Sorting Layer所需Editor生成差异，禁止目录0项、未暂存差异0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：三生成物diff PASS、ConfigExporter 0 warning/0 error、.NET 54/54；asmdef JSON、`.meta`、唯一READY、依赖和禁止模式检查PASS。
- EditMode XML：最终72 / 72 / 0，job `75fd8e4f09474f72952af754983ab879`；MCP结果路径`~/Library/Application Support/DefaultCompany/onedraw_2/TestResults.xml`。
- PlayMode XML：专项5 / 5 / 0，job `f8a4ab189acb4eb8b13e3d8f75c73a95`；最终全量20 / 20 / 0，job `f7b4dd1aa2ec4c67a3a8944751abbe9f`；同上路径。
- Console新增Error/Warning：脚本Refresh隔离0 / 0；Test Runner基础设施固定消息单列于`regression-notes.md`。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载28表645条配置与统一输入后，真实Mouse笔迹经采样/几何进入轨迹池；View与Geometry共享同一Points引用，位置数一致、刀宽18、Renderer启用且抬起后指针不活动。
- 标准Web：NOT RUN（T340不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T340新增阻碍）。
- 真机：BLOCKED（既有T120门，非T340新增阻碍）。
- 截图/日志/产物：见`trail-contract.md`、`runtime-smoke.md`、`unity-test-jobs.md`、`static-audit.md`、`regression-notes.md`和`config-verify.log`。

## 结论

- 已知问题：无T340新增产品问题；正式轨迹美术资源归T630，真实微信触摸沿用既有平台门。
- 结论：PASS。
