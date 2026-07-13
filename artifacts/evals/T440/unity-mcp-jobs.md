# T440 Unity MCP Test Jobs

- Unity：`6000.5.1f1`
- 实例：`onedraw_2@272e911286835fad`
- 项目：`/Users/cqmizhangxiaoyu2/dev/u3d/onedraw_2`
- 目标平台：WebGL Editor
- 执行日期：2026-07-14（Asia/Shanghai）

## 专项 EditMode

- 最终Job ID：`f9953c14112b44048e837a657a06844e`
- 过滤：category `T440`
- 结果：`Passed`，5 total / 5 passed / 0 failed / 0 skipped
- 覆盖：配置映射、预热与共享容量、Reject、ReuseOldest、旧/重复/未知租约、泄漏报告与Restart generation。

## 专项 PlayMode

- 最终Job ID：`8c61ddc3094d4de9b16974631174629f`
- 过滤：category `T440`
- 结果：`Passed`，1 total / 1 passed / 0 failed / 0 skipped
- 用例：`RestartThreeTimesPlayModeTests.SpawnKillClearAndRestartThreeTimesLeavesNoOldState`
- Runtime日志：schema `4`、content `0.5.0-sample`、hash `d524ffcda4693c9cb65e5e21d5ab753472a14b2233b2ae670ecc4b81f1251ee8`、28表/660条、Registry 76项。

## 全量 EditMode

- 最终Job ID：`a463f01b0b2f4a39a877ba6df5369ca9`
- 结果：`Passed`，127 total / 127 passed / 0 failed / 0 skipped

## 全量 PlayMode

- 最终Job ID：`8b1825099a314e8a84d59dffcda576af`
- 结果：`Passed`，31 total / 31 passed / 0 failed / 0 skipped

本次通过已连接Unity Editor Test Runner执行。MCP返回结构化job结果，Unity Test Runner同时把原生`TestResults.xml`写入用户级测试目录；每轮结束后复制到本证据目录，并用`Tools/CI/check-unity-test-results.py`再次解析通过。专项/全量四份XML分别为`editmode-results.xml`、`playmode-results.xml`、`full-editmode-results.xml`和`full-playmode-results.xml`。
