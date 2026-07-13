# T240 Canonical AssetRegistry Evidence

- Canonical资源：`Assets/_Game/Config/Registry/AssetRegistry.asset`
- 文件SHA-256：`f4b628ccf7ef85e3e49d1235bc5c36711adae0fd7125ad03fae080c59e07f05c`
- 配置hash：`16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`
- 总条目：76；Prefab 40；Sprite 18；AudioClip 17；Scene 1。
- Editor菜单校验输出：

```text
ASSET_REGISTRY_VALIDATION_PASS path=Assets/_Game/Config/Registry/AssetRegistry.asset ASSET_REGISTRY_READY source=AssetRegistry:Assets/_Game/Config/Registry/AssetRegistry.asset configHash=16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c entries=76 prefabs=40 sprites=18 audioClips=17 scenes=1
```

## 引用边界

- `AssetRegistrySO`只序列化`entries`；每个`AssetRegistryEntry`只序列化`assetKey`和`UnityEngine.Object`；`AssetSceneReference`只序列化明确的`scenePath`。
- Runtime配置程序集没有`UnityEditor`引用；Registry Runtime/Editor作者与校验代码不读取`addressOrPath`，Prefab/Sprite/AudioClip不通过路径或GUID解析。
- `scene_battle`指向已启用的`Assets/_Game/Scenes/Battle.unity`。
- 当前75个非场景键按类型复用三个Unity Editor创建的受管占位资源：`PlaceholderPrefab.prefab`、`PlaceholderSprite.asset`、`PlaceholderAudio.asset`。作者工具重跑保留每键已有的合法引用。
- `AssetRegistryBuildPreprocessor`在Build前复用Canonical校验；缺失、重复、额外、空、错型、非Prefab或未启用场景均失败。

静态审查还确认本任务没有修改`Design/Config`、Schema、Generated配置、Packages或ProjectSettings；Unity PlayMode产生的EditorSettings副作用已恢复。
