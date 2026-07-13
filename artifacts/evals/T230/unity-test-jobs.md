# T230 Unity Test Runner Evidence

- Unity：`6000.5.1f1 (0d9463e84828)`
- MCP实例：`onedraw_2@272e911286835fad`
- 运行日期：2026-07-13
- 编译刷新：Unity MCP `force/all/request` 完成，最终Console编译错误0。

## 专项EditMode

- Job：`959e524b8e3a48ad83369df8c121a54f`
- 过滤：`OneStrokeDemon.Tests.EditMode.T230`下全部12项。
- 结果：total 12 / passed 12 / failed 0 / skipped 0 / `Passed`。
- 覆盖：正式快照一次加载、只读索引、未知ID、DTO/Schema 28表248字段对齐，以及空/缺失/null/未知/重复/注释JSON、版本、hash、索引键和根/Global不一致整包拒绝。

## 专项PlayMode

- Job：`cacf4baa0db74f1880021fd5549c12ad`
- 结果：total 2 / passed 2 / failed 0 / skipped 0 / `Passed`。
- 覆盖：合法TextAsset先加载索引再进入MainMenu；不兼容schema记录上下文、阻断场景切换且不发布Runtime。

## 全量EditMode

- Job：`06e6276932154e6291178fa347796440`
- 程序集：`OneStrokeDemon.Tests.EditMode`
- 结果：total 25 / passed 25 / failed 0 / skipped 0 / `Passed`。

## 全量PlayMode

- Job：`24d514a0ab9e46749b8fb6f214800b41`
- 程序集：`OneStrokeDemon.Tests.PlayMode`
- 结果：total 4 / passed 4 / failed 0 / skipped 0 / `Passed`。

本次测试通过已连接的Unity Editor Test Runner执行，MCP返回结构化job结果，不生成批处理NUnit XML；job ID、精确计数和过滤范围保存在本证据中，未把未生成的XML伪报为产物。
