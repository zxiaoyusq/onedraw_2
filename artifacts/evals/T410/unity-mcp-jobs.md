# T410 Unity MCP Test Jobs

- Unity：`6000.5.1f1`
- 实例：`onedraw_2@272e911286835fad`
- 日期：`2026-07-13`

## 专项与配置闭环

| 范围 | 模式 | Job ID | 结果 |
| --- | --- | --- | --- |
| T410 | EditMode | `006d9164a8ff4a70bd0d3e2aaf864225` | PASS，4/4 |
| T410 | PlayMode | `88c2d4842dd045ebbdfbfb6f51e03331` | PASS，1/1 |
| ConfigPipeline | EditMode | `088cbaee0e8b4d89a885494647c08f58` | PASS，19/19 |
| ConfigPipeline | PlayMode | `e1623fc019ec479db9ae6a55d9871eff` | PASS，3/3 |

## 全量回归

| 范围 | 模式 | Job ID | 结果 |
| --- | --- | --- | --- |
| 全量 | EditMode | `c1a7cc3222674323b937f5b7a67cfcb6` | PASS，110/110 |
| 全量 | PlayMode | `c966677002b44f95880733c2d131f77c` | PASS，28/28 |

所有Job均由MCP返回`succeeded`且`failures_so_far=[]`。MCP任务接口未导出独立XML文件，因此以可查询Job ID保存追溯证据。

## 最终Editor状态

- 全量PlayMode造成的`ProjectSettings/EditorSettings.asset`临时差异已还原为基线值`m_EnterPlayModeOptions: 0`，该文件不进入任务改动集。
- 清空预期测试日志后执行Unity全量Refresh并请求编译；Editor状态为`idle`、`is_compiling=false`、`is_domain_reload_pending=false`、`ready_for_tools=true`。
- 最终Console：Error `0`，Warning `0`。
