# T370 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- 最终脚本Refresh/编译：完成；Console Error/Warning 0。

## 专项

| 阶段 | 模式 | Job | 结果 |
|---|---|---|---|
| 初次错误过滤尝试 | EditMode Category | `d9c6b40792944062a1d893cbc945abe5` | 0项；不能作为通过证据，随后改用完整类名。 |
| 初次实际专项 | EditMode | `10847c26c2db41aab1422d18fbfb3f9b` | 8/8 PASS。 |
| 初次专项 | PlayMode | `8b26bb1e08aa489e9a58792af3bfbff9` | 0/2；两项都因Unity把禁用`CircleCollider2D.radius=0`钳制为0.0001，而错误断言要求精确0。产品状态、禁用和复用覆盖均正常。 |
| 修正错误断言后 | PlayMode | `3a7e2d0d5c10471db1eb0fdedc5b6326` | 2/2 PASS。 |
| 运动优化后专项 | EditMode | `9fef0933d3124e888389cb5afbe26524` | 8/8 PASS。 |
| 运动优化后专项 | PlayMode | `b8429b7015b54d87a1dcf18216126a2e` | 2/2 PASS。 |
| inactive首次Spawn边界 | EditMode | `b545c02b68744e45a22004f0ea1ee1f7` | 8/8 PASS。 |
| inactive首次Spawn边界 | PlayMode | `8dca597441d94a8da4b907a21002f095` | 2/2 PASS；真实测试对象从inactive创建，激活后Collider保持配置状态。 |
| 最终诊断上下文审查 | EditMode | `b40613604ed24eebaba081c16a0c4786` | 8/8 PASS。 |
| 最终诊断上下文审查 | PlayMode | `ed4947c9835249e586264789d83b0384` | 2/2 PASS。 |

## 最终全量

| 模式 | Job | 总数 | 通过 | 失败 | 跳过 |
|---|---|---:|---:|---:|---:|
| EditMode | `29fc34140e5242fa9d8a57365c76a794` | 98 | 98 | 0 | 0 |
| PlayMode | `06d6ca9e04bb4216a48c493a24156a7e` | 25 | 25 | 0 | 0 |

全量测试过程中出现的`CFGRT003 schema 999`为既有负向启动用例，Test Runner另写自身结果路径并输出固定cleanup warning；测试完成后清空Console并复查Error/Warning为0。Test Runner修改的`EditorSettings.enterPlayModeOptions`已通过Unity Editor API恢复为`None`，最终没有ProjectSettings差异。
