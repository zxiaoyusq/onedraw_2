# T330 Regression Notes

- Unity Test Framework在PlayMode测试期间将`ProjectSettings/EditorSettings.asset`的`m_EnterPlayModeOptions`从基线0临时写为1；每轮均用可审查补丁恢复，最终该文件无差异。
- 全量PlayMode包含既有T230无效配置负例，Console按测试预期记录`CFGRT003`；这不是T330回归。
- MCP Test Runner每次落盘结果时把`Saving results to: .../TestResults.xml`记为Exception类型，并记录1条`Unity.PerformanceTesting.Editor.TestRunBuilder`的`IPostBuildCleanup` Warning；测试摘要全部Passed，堆栈不经过游戏代码。脚本Refresh/编译隔离检查为Error 0 / Warning 0，玩家路径输出无业务异常。
- 首次批处理因同项目已有前台Editor被Unity拒绝，未进入编译或测试；之后使用该现有Editor执行，避免强制关闭用户实例。
- 未发现T330新增产品问题。视觉淡出/池化和分段命中分别属于T340/T350；真实微信触摸、Safe Area和前后台继续沿用T120/T640/T710平台门。
