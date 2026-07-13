# T240 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T240；建立只保存`assetKey → Unity对象/明确场景引用`的Canonical AssetRegistry、Runtime只读类型化索引、Editor作者工具/校验器/构建前门和Bootstrap阻断。
- 明确不做：不实现T250一键配置生成与漂移检查；不导入正式美术/音频；不复制玩法平衡值；不修改xlsx、Schema、生成JSON、Packages、ProjectSettings或微信平台代码。
- 分支/提交：`main`；计划提交 `T240: implement asset registry validation`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1 (0d9463e84828)`；`onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema `1`；content `0.1.1-sample`；`16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增AssetRegistry SO/条目/场景引用、一次性校验服务、类型化接口、Runtime发布与稳定错误码；配置服务只读枚举76项AssetManifest；新增Editor作者工具、菜单校验及构建前门；由Unity Editor创建Canonical Registry和三个按类型共享占位资源并绑定Bootstrap；新增EditMode/PlayMode测试；同步决策、配置流水线、资源接入、任务/进度/索引和证据。
- 用户已有改动保护：任务开始工作树干净，未覆盖用户改动。Unity PlayMode临时改写的`ProjectSettings/EditorSettings.asset`已恢复到任务基线且未纳入任务差异；场景/Prefab/asset/meta均由Unity Editor/MCP创建或保存，未手工编辑Unity YAML。
- `git diff --check`：PASS（最终收尾复核）。
- 暂存白名单审查：PASS；实际路径逐项对照`change-whitelist.md`，无ProjectSettings、Packages、xlsx、Schema、Generated配置、微信SDK、MainMenu/Battle或正式Art改动。

## 自动验证

- 静态/导出校验：Canonical Registry恰好76项；Runtime Registry代码无`UnityEditor`引用，Registry Runtime/Editor绑定无`addressOrPath`读取；ConfigExporter .NET `46/46`，正式`validate --strict` PASS并保持28表645条与冻结hash；Unity最终编译Error 0；详情见`registry-summary.md`与`config-validation.md`。
- EditMode：专项`5/5`（MCP job `bdd3ec91bab8439484bf113c22a451d0`）；全量`30/30`（job `dc4faae01adb4f8a891fa7d12a3bf179`）。本次使用已连接Unity MCP Test Runner，未生成批处理XML；结构化job结果见`unity-test-jobs.md`。
- PlayMode：专项`1/1`（MCP job `3468132779e94986a29913d3382e4dc1`）；最终全量`5/5`（job `04fc06d21ea14f14a62451bbc6e86e58`）。本次使用已连接Unity MCP Test Runner，未生成批处理XML；结构化job结果见`unity-test-jobs.md`。
- Console新增Error/Warning：真实Bootstrap路径清空Console后检查为`0/0`。

## 玩家与平台证据

- 真实玩家路径和可断言值：Unity Editor从Bootstrap进入Play Mode；先输出配置摘要，再输出Registry摘要`entries=76 prefabs=40 sprites=18 audioClips=17 scenes=1`，活动场景随后为`MainMenu`。合法Registry由PlayMode回归断言发布及四类类型化查询；坏Registry由EditMode断言阻断构建。
- 标准Web：NOT RUN（T240不要求构建；既有T100结论不外推）。
- 微信转换：NOT RUN（按用户决定延期；既有T120 G2证据不在本任务重跑）。
- DevTools：BLOCKED（既有`blocked_missing_tool`，本任务不恢复）。
- 真机：BLOCKED（既有`blocked_missing_device_and_devtools`，本任务不恢复）。
- 截图/日志/产物：`unity-test-jobs.md`、`runtime-smoke.md`、`registry-summary.md`、`config-validation.md`、Canonical `Assets/_Game/Config/Registry/AssetRegistry.asset`及受管占位资源。

## 结论

- 已知问题：75个非场景键当前按类型共享三个受管占位资源，仅证明完整引用与类型合同；正式资源与逐Prefab内容需在后续玩法任务和T630逐项替换。T250流水线与微信G3/G4维持既有后续范围，不影响T240验收。
- 结论：PASS
