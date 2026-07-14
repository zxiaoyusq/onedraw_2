# T610 Change Whitelist

- Git基线：`bbd12724ac398ccd8dc2c7056ab44a332256d5e0`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：建立可再分发、可复现生成的中文TMP静态字体资产与全局fallback，覆盖全部配置中文文案、HUD动态数字及常用UI符号，并保存专项测试和PlayMode截图证据。
- 明确不做：不打包无关CJK全集或不必要超大Atlas；不修改配置数值/文案、场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；不提前实现T620反馈、T630正式美术、T640完整适配或T650教程遮罩，也不恢复T120/T130平台工作。

## 预计改动白名单

- `Assets/_Game/Art/UI/Fonts/**`及Unity生成`.meta`：保存OFL字体子集、许可证/来源、从配置生成的字符清单与TMP静态SDF资产；禁止提交未裁剪的CJK源字体或动态多Atlas。
- `Assets/TextMesh Pro/**`：由Unity Editor API导入uGUI包内固定版本的TMP Essential Resources，再删除未使用的LiberationSans Atlas、源字体和非移动SDF shader；保留移动端SDF shader、项目设置、样式与中文行首/行尾规则，并把默认字体和全局fallback指向T610资产，不自行编辑Unity YAML。
- `Assets/_Game/Scripts/Presentation/BattleHudViewFactory.cs`：让运行时创建的所有HUD文本显式使用项目字体策略，不改变HUD状态、业务逻辑或布局数值。
- `Assets/_Game/Scripts/Editor/Localization/**`、`Assets/_Game/Scripts/Editor/OneStrokeDemon.Editor.asmdef`及Unity生成`.meta`：新增可重复执行的字符清单、静态字体资产和TMP设置作者工具；仅使用Unity/TMP Editor API生成Unity资产。
- `Assets/_Game/Tests/EditMode/T610/**`、`Assets/_Game/Tests/PlayMode/T610/**`及目录`.meta`：新增配置中文、fallback、Atlas预算、动态数字、无缺字/裁切和PlayMode截图验证。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅补充T610测试所需的TMP/Presentation/Editor引用。
- `artifacts/evals/T610/**`：Git基线、字体来源/子集/字符覆盖清单、专项及全量Unity测试、PlayMode截图、Console和最终审查证据。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`docs/TECH_SPEC.md`、`docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`、`project-index.yaml`：记录字体数据所有权、字符覆盖、生成参数、验证结果、任务状态与下一任务；只在确有影响时更新相关文档。

## 禁止改动

- 不修改xlsx、受管JSON/hash/ConfigIds、配置Schema/DTO/导出器、场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不添加闭源或来源/许可不明确的字体，不提交完整CJK源字体，不把字体内容放在交付包外部。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
