# T693 Change Whitelist

- `ProjectSettings/ProjectSettings.asset`: only disable Portrait and Portrait Upside Down autorotation; preserve the user's existing Standalone batching hunk.
- `Assets/_Game/Tests/EditMode/T693/AndroidOrientationSettingsTests.cs` and Unity-generated metadata: lock the landscape-only Player Settings contract.
- `docs/TASKS.md` and `docs/PROGRESS.md`: record task state and verification.
- `artifacts/evals/T693/`: task evidence.

Excluded: scenes, prefabs, Build Profiles, WeChat settings, gameplay/config data, `ProjectSettings/QualitySettings.asset`, `ProjectSettings/UnityConnectSettings.asset`, and `Assets/Resources.meta`.
