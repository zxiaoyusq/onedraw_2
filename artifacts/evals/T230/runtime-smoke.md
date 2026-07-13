# T230 Runtime Player Path

- Unity Editor加载 `Assets/_Game/Scenes/Bootstrap.unity` 后进入Play Mode。
- BootstrapController场景字段由Unity MCP绑定到 `Assets/_Game/Config/Generated/gameplay_config.json` 并由Editor保存，未手工编辑Unity YAML。
- 观察到启动摘要：

```text
CONFIG_RUNTIME_READY source=TextAsset:gameplay_config schema=1 content=0.1.1-sample hash=16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c tables=28 records=645 primaryIndexes=270 groupIndexes=49
```

- 启动后活动场景：`Assets/_Game/Scenes/MainMenu.unity`。
- 清空Console后执行该路径，最终新增Error 0、Warning 0。
- 不兼容配置阻断由专项PlayMode job `cacf4baa0db74f1880021fd5549c12ad`独立断言。
