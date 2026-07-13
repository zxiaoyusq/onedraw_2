# T230 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T230；实现Unity Runtime完整配置DTO、严格一次加载、schema/content/hash检查、只读主键/分组索引、启动摘要和Bootstrap阻断。
- 明确不做：不实现T240 AssetRegistry；不实现T250 hash旁车、ConfigIds、一键生成或CI漂移检查；不实现玩法或恢复微信平台门。
- 分支/提交：`main`；计划提交 `T230: implement runtime config loading`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1 (0d9463e84828)`；`onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema `1`；content `0.1.1-sample`；`16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增28表/248字段DTO、严格解析/兼容/hash/候选快照/只读查询服务、Runtime发布与link.xml；提交生成JSON；Bootstrap通过TextAsset加载；Unity Editor绑定并保存Bootstrap场景；新增EditMode/PlayMode测试；固定Newtonsoft直接依赖；同步包、流水线、决策、任务/进度/索引文档。
- 用户已有改动保护：任务开始工作树干净；未覆盖用户改动。Unity Play Mode临时改写的 `ProjectSettings/EditorSettings.asset` 已恢复到任务基线，未纳入任务差异。
- `git diff --check`：PASS（最终收尾复核）。
- 暂存白名单审查：PASS；实际路径逐项对照 `change-whitelist.md`，无ProjectSettings、xlsx、Schema、样例、MainMenu/Battle、SDK、Prefab或美术改动。

## 自动验证

- 静态/导出校验：ConfigExporter .NET `46/46`；正式 `validate --strict` PASS；重新导出28表645条、168,071字节，生成快照与临时输出SHA-256均为 `91d2c312cd2caead5243ef76ee12b54dc53702dc0ba23d4d34b0726c111a066a`，`cmp` PASS。Unity编译0 error。
- EditMode：专项 `12/12`（MCP job `959e524b8e3a48ad83369df8c121a54f`）；全量 `25/25`（job `06e6276932154e6291178fa347796440`）。本次使用已连接Unity MCP Test Runner，未生成批处理XML；完整可审查结果见 `unity-test-jobs.md`。
- PlayMode：专项 `2/2`（MCP job `cacf4baa0db74f1880021fd5549c12ad`）；最终全量 `4/4`（job `24d514a0ab9e46749b8fb6f214800b41`）。本次使用已连接Unity MCP Test Runner，未生成批处理XML；完整可审查结果见 `unity-test-jobs.md`。
- Console新增Error/Warning：真实Bootstrap路径清空Console后检查为 `0/0`。

## 玩家与平台证据

- 真实玩家路径和可断言值：Unity Editor从Bootstrap进入Play Mode；输出 `CONFIG_RUNTIME_READY`，source=`TextAsset:gameplay_config`、schema=`1`、content=`0.1.1-sample`、28表、645记录、270主索引、49分组索引；活动场景随后为 `MainMenu`。不兼容schema PlayMode测试断言阻断场景切换且Runtime不发布。
- 标准Web：NOT RUN（T230不要求构建；既有T100结论不外推）。
- 微信转换：NOT RUN（按用户决定延期；既有T120 G2证据不在本任务重跑）。
- DevTools：BLOCKED（既有 `blocked_missing_tool`，本任务不恢复）。
- 真机：BLOCKED（既有 `blocked_missing_device_and_devtools`，本任务不恢复）。
- 截图/日志/产物：`unity-test-jobs.md`、`runtime-smoke.md`、`config-export.md`、`package-license.md`、受管 `Assets/_Game/Config/Generated/gameplay_config.json`。

## 结论

- 已知问题：T250尚未提供hash旁车、ConfigIds和自动漂移检查；这属于后续已排期范围，不影响T230验收。微信G3/G4维持既有阻塞，不构成T230阻塞。
- 结论：PASS
