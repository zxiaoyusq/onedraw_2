# T240 Runtime Player Path

- Unity Editor加载`Assets/_Game/Scenes/Bootstrap.unity`后进入Play Mode。
- BootstrapController的Canonical Registry字段由Unity MCP绑定到`Assets/_Game/Config/Registry/AssetRegistry.asset`并由Editor保存，未手工编辑Unity YAML。
- 观察到启动摘要：

```text
CONFIG_RUNTIME_READY source=TextAsset:gameplay_config schema=1 content=0.1.1-sample hash=16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c tables=28 records=645 primaryIndexes=270 groupIndexes=49
ASSET_REGISTRY_READY source=AssetRegistry:asset_registry configHash=16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c entries=76 prefabs=40 sprites=18 audioClips=17 scenes=1
```

- 启动后活动场景：`Assets/_Game/Scenes/MainMenu.unity`。
- 清空Console后执行该路径，最终新增Error 0、Warning 0。
- 全量PlayMode job `04fc06d21ea14f14a62451bbc6e86e58`独立断言Registry已发布、Count为76，并可类型化读取Prefab、Sprite、AudioClip和Battle场景引用。
