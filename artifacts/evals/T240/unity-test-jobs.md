# T240 Unity Test Runner Evidence

- Unity：`6000.5.1f1 (0d9463e84828)`
- MCP实例：`onedraw_2@272e911286835fad`
- 运行日期：2026-07-13
- 编译刷新：Unity MCP `force/all/request`完成；最终Console编译Error 0。

## 专项EditMode

- Job：`bdd3ec91bab8439484bf113c22a451d0`
- 过滤：`OneStrokeDemon.Tests.EditMode.T240`下全部5项。
- 结果：total 5 / passed 5 / failed 0 / skipped 0 / `Passed`。
- 覆盖：Canonical 76键分类和持久化类型；空/空对象/重复/缺失/额外/错型整包拒绝；Canonical正向构建门与坏Registry构建失败；资源替换保持稳定ID；SerializedField白名单不含平衡值。

## 专项PlayMode

- Job：`3468132779e94986a29913d3382e4dc1`
- 结果：total 1 / passed 1 / failed 0 / skipped 0 / `Passed`。
- 覆盖：Bootstrap先加载配置与Registry摘要，再进入MainMenu；Runtime发布76项并完成Prefab/Sprite/AudioClip/Scene类型化查询。

## 全量EditMode

- Job：`dc4faae01adb4f8a891fa7d12a3bf179`
- 程序集：`OneStrokeDemon.Tests.EditMode`
- 结果：total 30 / passed 30 / failed 0 / skipped 0 / `Passed`。

## 全量PlayMode

- Job：`04fc06d21ea14f14a62451bbc6e86e58`
- 程序集：`OneStrokeDemon.Tests.PlayMode`
- 结果：total 5 / passed 5 / failed 0 / skipped 0 / `Passed`。

本次测试通过已连接的Unity Editor Test Runner执行，MCP返回结构化job结果，不生成批处理NUnit XML；job ID、精确计数和过滤范围保存在本证据中，未把未生成的XML伪报为产物。
