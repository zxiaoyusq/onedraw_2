# T691 验证记录

- `Assets/_Game/Prefabs/Actors/EnemyFireFish.prefab.meta` 命中反向规则 `!/[Aa]ssets/**/*.meta`。
- `Tools/example.meta` 继续命中全局 `*.meta` 忽略规则。
- 解除忽略后仅发现 2 个历史未跟踪 meta。
- `Assets/Settings/Build Profiles/Android.asset.meta` 对应已跟踪资产，纳入 T691。
- `Assets/Resources.meta` 对应无受管内容的空本地目录，未删除且不纳入提交。
- 暂存区不包含任何 `ProjectSettings` 文件。
