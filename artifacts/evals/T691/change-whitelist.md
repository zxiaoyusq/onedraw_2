# T691 预计改动白名单

- `.gitignore`
- `Assets/**/*.meta`：仅补交解除忽略后发现、且对应已跟踪 Unity 资产的必要 meta
- `docs/TASKS.md`
- `docs/PROGRESS.md`
- `artifacts/evals/T691/**`

明确排除并保护用户已有改动：

- `ProjectSettings/ProjectSettings.asset`
- `ProjectSettings/UnityConnectSettings.asset`

`ProjectSettings/QualitySettings.asset` 仅为 Git 状态缓存差异，内容哈希与索引一致，不纳入提交。
