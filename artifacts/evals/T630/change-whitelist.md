# T630 Change Whitelist

- Git基线：`main@1fa1d465c5c6bd8a81ea1933e56763928f64fd9a`，任务开始时工作树干净。
- 需要保护的用户已有改动：无；后续出现的非白名单差异一律停止并审查。
- 任务目标：从项目提供的PSD/图像源导出并接入背景、主角、六怪、Boss、UI图标、投射物与VFX原型资源；统一透明PNG、尺寸、压缩、命名、Pivot、PPU、Sorting Layer、SpriteAtlas及来源/授权记录，并替换T240 Registry类型占位绑定。
- 明确不做：不把原始125MB PSD放入Runtime Assets，不把单张角色虚构为骨骼拆件，不制作正式全套动画，不修改玩法数值/配置内容，不补音频，不提前实现T640/T650/T700或恢复T120/T130及微信打包工作。

## 预计改动白名单

- `Assets/_Game/Art/{Backgrounds,Characters,Enemies,UI,Sprites,VFX,SpriteAtlases}/**`及Unity `.meta`：透明PNG原型、VFX Prefab、SpriteAtlas、导入元数据和同目录来源说明；禁止原始PSD进入Runtime Assets。
- `Assets/_Game/Prefabs/{Actors,VFX}/**`及Unity `.meta`：通过Unity Editor生成/更新主角、精英、Boss和表现Prefab，只保存Unity资源引用、渲染/碰撞结构及明确技术设置，不保存玩法数值。
- `Assets/_Game/Config/Registry/AssetRegistry.asset`：通过Unity Editor把现有稳定`assetKey`改绑为T630实际资源；不新增平衡数据或路径查找。
- `ProjectSettings/TagManager.asset`：仅允许Unity Editor补齐T630明确需要的Sorting Layer；禁止其他ProjectSettings变化。
- `Assets/_Game/Scripts/Editor/**`、`Tools/ArtPipeline/**`：T630可重复作者工具、导入器设置、来源清单与资源校验；不在Runtime热路径解析PSD。
- `Assets/_Game/Tests/EditMode/T630/**`、`Assets/_Game/Tests/PlayMode/T630/**`及Unity `.meta`：导入设置、透明度、Registry唯一绑定、Atlas覆盖、Prefab结构、玩家视觉路径和截图测试。
- 既有T240/T450/T600/T620测试：仅同步从共享占位到实际资源后的明确断言，不改变原任务行为覆盖。
- `docs/ART_PIPELINE.md`、`docs/ASSET_SOURCES.md`、`docs/ASSET_INTEGRATION.md`、`docs/TECH_SPEC.md`、`docs/TEST_PLAN.md`、`docs/DECISIONS.md`、`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：记录资源规范、来源/授权、验证、任务状态和下一任务。
- `artifacts/evals/T630/**`：Git基线、源文件清单/hash、导出审计、专项/全量测试、Unity日志、1920×1080视觉截图与最终验证报告。
- `.gitattributes`：收尾审查发现Unity 6000.5生成的Prefab与SpriteAtlas v2同现有`.asset/.unity`一样会为YAML空标量保留尾随空格；仅把`*.prefab`和`*.spriteatlasv2`纳入既有`-whitespace`规则，不改变文件内容或其他Git策略。

## 禁止改动

- 工作簿、Schema、FieldDictionary、导出器、受管JSON/hash/ConfigIds、音频资源、Scene、Input Actions、Packages、除Sorting Layer外的ProjectSettings、微信SDK与`Builds/**`。
- T640多比例/Safe Area矩阵、T650教程遮罩、T700回归扩写及其他后续任务实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
