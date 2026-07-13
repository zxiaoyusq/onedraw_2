# T320 Regression Notes

- Unity Test Framework在PlayMode专项、全量测试及人工Play路径期间把`ProjectSettings/EditorSettings.asset`中的`m_EnterPlayModeOptions`从基线0临时写为1；均按白名单审查恢复，最终该文件无差异。
- 全量测试包含既有的无效配置负例，因此测试期Console会出现预期`CFGRT003`；清空后单独执行Bootstrap→MainMenu真实路径，最终Error/Warning均为0。
- 未发现T320新增产品问题。曲率与闭合的玩法阈值匹配属于T330；视觉/碰撞共享`StrokeGeometryData.Points`分别由T340/T350验证。
- 真实微信触摸、Safe Area和前后台仍沿用T120/T640/T710的平台门，不由本任务宣称通过。
