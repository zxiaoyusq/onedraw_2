# T350 Regression Notes

- Unity Test Runner每次PlayMode运行会把`ProjectSettings/EditorSettings.asset`的`m_EnterPlayModeOptions`临时从0改为1；每轮测试后均恢复为基线0，最终不纳入任务改动。
- Editor Console在脚本导入编译隔离检查后读取Error/Warning为0；全量PlayMode包含既有T230无效schema负例，按测试预期记录`CFGRT003`并通过。
- MCP Test Runner固定把`Saving results to: .../TestResults.xml`记为Exception，并记录`Unity.PerformanceTesting.Editor.TestRunBuilder`的`IPostBuildCleanup` Warning；二者堆栈不经过T350代码。除这些既有测试/基础设施消息外，专项与全量测试没有T350新增产品错误或警告。
- `Tools/CI/verify-config.sh --skip-unity`明确输出`PARTIAL`仅因为Unity部分由当前已连接Editor执行；其导出器构建、临时生成、三生成物漂移和.NET 54/54全部PASS，Unity侧由全量78/78与22/22覆盖。
- 标准Web、微信转换、DevTools和真机不属于T350验收；T120/T130继续按用户决定延期，没有把转换成功外推成平台运行成功。
