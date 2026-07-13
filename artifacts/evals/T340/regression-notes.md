# T340 Regression Notes

- 首次脚本Refresh只请求了scripts编译但未导入新资产，测试筛选返回0项；未把该结果记为PASS，随后强制全资产Refresh生成`.meta`并正确发现5项。
- 首次正确发现后的5项因`UnitySetUp`与继承自`InputTestFixture`的同步`SetUp`执行顺序导致配置加载后被重置而全部失败；将Bootstrap加载移入各测试体后解除Harness问题，运行时代码未因此修改。
- 下一轮仅淡出alpha断言失败：LineRenderer读回0.5019608而算法目标为0.5，属于Color32一个量化步长；断言容差改为`1/255`后通过。
- 收尾审查发现配置引用`VFX` Sorting Layer而工程只有Default；使用Unity Editor SerializedObject API新增该层并补非零ID断言。保存时Unity 6000.5.1f1自动把TagManager从serializedVersion 2迁移为3并补Rendering Layers字段，保留为可审查的Editor生成差异。
- Unity Test Framework在PlayMode期间临时把`ProjectSettings/EditorSettings.asset.m_EnterPlayModeOptions`从0写为1；每轮均用补丁恢复，最终该文件无差异。
- 全量PlayMode包含既有T230无效schema负例，Console按测试预期记录`CFGRT003`；该测试通过且不是T340回归。
- MCP Test Runner固定把`Saving results to: .../TestResults.xml`记为Exception并记录`Unity.PerformanceTesting.Editor.TestRunBuilder`的`IPostBuildCleanup` Warning；脚本Refresh隔离检查为Error 0 / Warning 0，消息堆栈不经过游戏代码。
- 未发现T340新增产品问题。正式轨迹美术材质/Prefab归T630；分段命中归T350；真实微信触摸继续受已延期T120/T640门约束。
