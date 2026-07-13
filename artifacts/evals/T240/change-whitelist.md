# T240 Change Whitelist

- Git基线：`d76d695a92c887e2f135823d9668d147553f95de`（`main`；任务开始工作树干净）。
- 需要保护的用户已有改动：无；后续出现白名单外改动时保留并先判定来源，不覆盖。
- 任务目标：建立只保存 `assetKey → UnityEngine.Object`/场景引用的AssetRegistry；启动构建只读索引；用Editor校验76个AssetManifest键的覆盖、唯一、非空、类型与场景有效性；缺失/重复/错型在进入主菜单和构建前失败。
- 明确不做：不实现T250一键配置流水线；不导入正式美术/音频；不复制HP/CD/伤害或其他平衡值；不修改配置工作簿、Schema、生成JSON或微信平台代码。

## 预计改动白名单

- `Assets/_Game/Scripts/Config/**`：新增AssetRegistrySO、运行时只读服务/异常/摘要/场景引用包装与Runtime发布；只允许为Editor校验增加AssetManifest只读枚举，不增加路径查找、玩法数值或热路径反射。
- `Assets/_Game/Scripts/Bootstrap/BootstrapController.cs`：仅允许注入Registry资源、在配置成功后初始化Registry、记录摘要并在失败时阻断场景切换。
- `Assets/_Game/Scripts/Editor/AssetRegistry/**`：新增确定性占位资源/Registry生成、菜单校验和`IPreprocessBuildWithReport`构建前门；允许Editor使用AssetDatabase/PrefabUtility，Runtime不得依赖UnityEditor。
- `Assets/_Game/Config/Registry/**`：仅允许Unity Editor/MCP创建一个Sprite占位、一个AudioClip占位、一个Prefab占位、Battle场景引用和覆盖76个键的Canonical AssetRegistry；Registry条目只能保存key与Unity对象引用。
- `Assets/_Game/Scenes/Bootstrap.unity`：仅通过Unity Editor/MCP保存`AssetRegistrySO`场景引用，禁止手工编辑Unity YAML。
- `Assets/_Game/Tests/EditMode/T240/**`、`Assets/_Game/Tests/PlayMode/T240/**`及目录`.meta`：覆盖完整Canonical校验、缺失/重复/空/错型/额外键、场景与构建门、资源替换ID稳定和Bootstrap运行路径；不得复制第二份完整配置。
- `docs/DECISIONS.md`、`docs/CONFIG_PIPELINE.md`、`docs/ASSET_INTEGRATION.md`：同步Registry所有权、占位边界、构建门和验证结论。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T240状态、证据与下一任务T250。
- `artifacts/evals/T240/**`：基线、白名单、Unity测试、Canonical摘要、玩家路径、Console和最终验证证据。
- `artifacts/tmp/T240/**`：忽略的测试/日志临时输出，不提交。

## 禁止改动

- 不修改 `Design/Config/GameConfig.xlsx`、模板镜像、`config/schema/**`、`Assets/_Game/Config/Generated/**`、Packages、ProjectSettings、微信SDK/构建、MainMenu/Battle场景、正式Art/Prefabs目录或平台外部状态。
- 不提交Library/Logs/Temp、缓存、测试派生Registry；不把manifest的`addressOrPath`复制进Runtime索引；不在SO/Inspector/C#保存任何玩法平衡值。
- `.asset/.prefab/.unity/.meta`只能由Unity Editor/MCP创建或保存；若PlayMode测试临时改写EditorSettings，必须恢复任务基线且不得暂存。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
