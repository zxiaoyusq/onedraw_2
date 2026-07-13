# T310 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T310；配置阈值驱动的笔迹最小距离采样、最大长度精确裁剪、最大点数稳定终止、不可变结果、取消语义和统一输入事件桥接。
- 明确不做：不实现T320 RDP/重采样/几何量，不实现识别、轨迹、命中或玩法运行时选择；不修改配置内容、场景、Prefab或平台状态。
- 分支/提交：`main` / `T310: implement stroke sampling`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无差异。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Input程序集采样设置/状态机/不可变数据/事件采集器；Combat配置映射；T310 Edit/Play测试；EditMode测试程序集引用；任务索引、进度和证据。
- 用户已有改动保护：任务开始工作树干净；测试框架产生的EditorSettings临时差异已恢复；禁止目录最终0项。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；31个文件全部属于预计白名单，禁止目录0项、未暂存差异0项、敏感模式0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：`verify-config.sh --skip-unity`三生成物diff PASS、.NET 54/54；asmdef JSON、YAML解析、Unity `.meta`配对、禁止Runtime模式扫描和`git diff --check`均PASS。
- EditMode：StrokeSampling 9/9（MCP job `df0d901a8f0d42f699e6acdd783f5d7a`）；文档与任务状态同步后最终全量46/46（job `4f8f40fa639d4b0c879cf9095d89c22d`）。
- PlayMode：StrokeSampling 1/1（MCP job `596f8a16d9d14d62886ef2d4bc3b4540`）；全量13/13（job `5343260955ba4f5f9935aaa3a7ebf952`）。
- Console新增Error/Warning：最终真实玩家路径0 / 0。

## 玩家与平台证据

- 真实玩家路径和可断言值：真实Input System Mouse拖拽经T300适配器产生1个3点、正长度、`PointerEnded`的不可变笔迹；Bootstrap加载配置/Registry/输入并进入MainMenu。
- 标准Web：NOT RUN（T310不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T310新增阻碍）。
- 真机：BLOCKED（既有T120门，非T310新增阻碍）。
- 截图/日志/产物：见`stroke-contract.md`、`unity-test-jobs.md`、`runtime-smoke.md`、`static-audit.md`、`regression-notes.md`和`config-verify.log`。

## 结论

- 已知问题：无T310新增产品问题；真实微信触摸与生命周期验证仍保留在既有平台门。
- 结论：PASS。
