# T300 Regression Notes

1. 首次PlayMode专项中，Touch测试坐标超出当时Game View高度，因此Safe Area入口按设计拒绝；测试改为按`Screen.width/height`比例生成设备内坐标后Mouse/Touch合同通过，产品代码没有放宽Safe Area门。
2. 首次全量PlayMode暴露先前Bootstrap测试遗留的跨场景`PointerInputRuntime`会在后续`InputTestFixture.SaveAndReset`期间读取旧设备缓存。T300设备测试现在在基类重置Input System前先重置持久Runtime，并在基类恢复前销毁测试Adapter；最终全量12/12，无忽略日志或`LogAssert.Expect`掩盖。
3. PlayMode测试产生的`ProjectSettings/EditorSettings.asset`临时差异已恢复任务基线，未纳入改动。
