# T360 Unity MCP Test Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- 最终脚本刷新：编译完成，domain reload完成，Editor ready。

## 最终通过矩阵

| 范围 | 模式 | MCP job | 总数 | 通过 | 失败 | 跳过 |
|---|---|---|---:|---:|---:|---:|
| `DamageFormula,ComboScore` | EditMode | `7aa466e976ee495c8a704d02420507ca` | 12 | 12 | 0 | 0 |
| `CombatResolutionPipeline` | PlayMode | `7f17c39ebe7342e4bcc859f553341198` | 1 | 1 | 0 | 0 |
| 全量`OneStrokeDemon.Tests.EditMode` | EditMode | `2049df040cc1460c8dc8ea6cbfa32ed2` | 90 | 90 | 0 | 0 |
| 全量`OneStrokeDemon.Tests.PlayMode` | PlayMode | `9fd4d69720b449279b7fcc19df7659bb` | 23 | 23 | 0 | 0 |
| `ConfigPipeline` | EditMode | `1ac3d83518ec45adbbb098e1c7df1676` | 19 | 19 | 0 | 0 |
| `ConfigPipeline` | PlayMode | `2569e51498cf401cbe4e8982f0e43c93` | 3 | 3 | 0 | 0 |

PlayMode专项输出确认：schema 2、content 0.2.0-sample、hash `19dc788f890f995adb94458f74894b89514f85f3bfc9429659ddd2421a72f733`、28表647条、270个主索引、49个分组索引；AssetRegistry 76项；Pointer Runtime为Mouse/Touch和1920×1080参考空间。

## 首次失败与修正

- 首次专项EditMode job `26c6ae4aab0c4adcb5f39841774ed220`为11/12：测试用`1.8d`重建了配置中的`1.8f`边界。改为直接使用`ComboService.TimeoutSeconds`构造精确边界后通过；规则始终保持`elapsed <= timeout`延续。
- 首次配置PlayMode job `29d68251c5a744fb8396f436c0c42080`为2/3：T240启动日志仍断言旧配置hash/记录数。将该受影响测试加入白名单并同步新元数据后通过。
- GAME_DESIGN审查后新增`scorePerDamage`，上述最终矩阵是在最终hash与公式上重新执行的结果，早期通过记录不作为最终结论。

## Console与Editor副作用

- 最终清空测试期预期日志后，Console Error/Warning查询为0项。
- ConfigPipeline负向测试产生的schema 999拒绝日志属于测试期预期Error；Unity Test Runner还产生保存结果与清理提示，均未作为最终Console错误。
- PlayMode Test Runner两次把`EditorSettings.enterPlayModeOptions`临时改为`DisableDomainReload`；均通过Unity Editor API恢复为基线`None`，`ProjectSettings/EditorSettings.asset`最终无diff。
