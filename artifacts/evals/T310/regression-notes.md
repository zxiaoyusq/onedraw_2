# T310 Regression Notes

- 首次尝试启动批处理EditMode时，Unity因同一工程已在Editor打开而以project lock拒绝；这是测试执行环境冲突，不是编译或用例失败，原始日志保留在`editmode-stroke-sampling.log`。随后使用该6000.5.1f1 Editor实例刷新、编译并运行全部测试。
- Unity Test Framework在PlayMode测试/运行期间两次把`ProjectSettings/EditorSettings.asset`中的`m_EnterPlayModeOptions`从基线0临时写为1；每次均按白名单审查恢复，最终该文件无差异。
- 全量测试包含既有的无效配置负例，因此测试期Console会出现预期`CFGRT003`；清空后单独执行Bootstrap→MainMenu真实路径，最终Error/Warning均为0。
- 未发现T310新增产品问题。真实微信触摸、Safe Area和前后台仍沿用T120/T640/T710的平台门，不由本任务宣称通过。
