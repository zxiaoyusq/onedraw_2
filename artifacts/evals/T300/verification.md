# T300 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T300；统一Mouse/Touch输入、UI起笔阻挡、动态Safe Area到配置参考像素转换、单活动指针所有权，以及失焦/暂停/禁用/断设备取消。
- 明确不做：不实现T310采样与长度/点数规则；不实现笔势、轨迹、命中或多指；不修改配置内容、场景、Prefab或微信平台状态。
- 分支/提交：`main` / `T300: implement unified pointer input`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无差异。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Input程序集事件/坐标/状态/Unity适配/Runtime；Bootstrap配置注入；T300 Edit/Play测试；程序集引用和输入合同文档。
- 用户已有改动保护：任务开始工作树干净；xlsx/Schema/生成配置、Packages、ProjectSettings、Input Actions、场景、Prefab和Registry无差异。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；40个文件全部属于预计白名单，禁止目录0项、未暂存差异0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：`verify-config.sh --skip-unity`三生成物diff PASS、.NET 54/54；asmdef JSON、Unity `.meta`配对、禁止目录、旧Input API/`Screen.dpi`扫描和`git diff --check`均PASS。
- EditMode：PointerInput 5/5（MCP job `e3fa0967625c401185c2f492ed03c2ce`）；全量37/37（job `81a09ac4c8b943ec86f5be2e948119d5`）。
- PlayMode：PointerInput 7/7（MCP job `1e21ecef7064480fab0bf25674fa7cda`）；全量12/12（job `72bcd799addf41a28f6938140cd5b594`）。
- Console新增Error/Warning：最终真实玩家路径0 / 0。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载28表645条配置与76键Registry，从配置初始化Mouse/Touch单指Runtime后进入MainMenu；摘要为`reference=1920x1080 safeArea=dynamic uiBeginBlock=true maxActivePointers=1`。
- 标准Web：NOT RUN（T300不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T300新增阻碍）。
- 真机：BLOCKED（既有T120门，非T300新增阻碍）。
- 截图/日志/产物：见`pointer-contract.md`、`unity-test-jobs.md`、`runtime-smoke.md`、`static-audit.md`和`regression-notes.md`。

## 结论

- 已知问题：无T300新增产品问题；Mouse/Touch由Unity Test Framework设备模拟验证，真实微信触摸/Safe Area与前后台仍保留在T120/T640/T710门。
- 结论：PASS。
