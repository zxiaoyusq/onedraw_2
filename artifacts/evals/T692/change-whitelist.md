# T692 预计改动白名单

- `Assets/_Game/Scenes/Bootstrap.unity`
- `Assets/_Game/Scenes/MainMenu.unity`
- `Assets/_Game/Scenes/Battle.unity`
- `Assets/_Game/Scripts/Editor/OneStrokeDemon.Editor.asmdef`
- `Assets/_Game/Scripts/Editor/Art/T692GlobalLightSortingLayerAuthoring.cs`
- 上述新增 Unity 资产对应的 `.meta`
- `Assets/_Game/Tests/EditMode/T692/`
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`
- `Assets/_Game/Tests/PlayMode/T692/`
- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `artifacts/evals/T692/`

明确排除用户已有的 `ProjectSettings/**` 改动和未跟踪的 `Assets/Resources.meta`；不修改火鱼贴图、动画、Prefab、Registry、配置表或玩法代码。
