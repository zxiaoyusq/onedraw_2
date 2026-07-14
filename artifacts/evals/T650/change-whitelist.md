# T650 Change Whitelist

- Git基线：`main@aa49ea40a90d6493a5b04cb17e60b77732b3bc82`；`git status --short --branch`仅输出`## main`。
- 需要保护的用户已有改动：无；基线工作树干净。后续若出现白名单外差异，先判定来源并停止覆盖。
- 任务目标：实现事件驱动教程遮罩、配置手势示意、显式跳过/回看与可持久化一次性完成标记；证明重开及提前跳过不会锁死关卡。
- 明确不做：不实现T640多设备适配、T700及后续任务；不修改玩法动作/关卡/波次数值；不改Scene、Prefab、Input Actions、AssetRegistry、Packages、ProjectSettings、微信SDK或Builds；不恢复T120/T130平台工作；不把固定延时作为教程动作完成条件。

## 预计改动白名单

- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`、`outputs/T650/GameConfig.xlsx`：同步`Texts`中的跳过/回看UI文案与content版本，保持两源字节一致并输出交付副本。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：由正式工作簿确定性导出；只允许content版本/hash、两条Texts及生成ID变化。
- `Tools/ConfigExporter/Tests/*.cs`、`Assets/_Game/Tests/EditMode/T230/*.cs`、`Assets/_Game/Tests/EditMode/T250/*.cs`、`Assets/_Game/Tests/PlayMode/T230/*.cs`、`Assets/_Game/Tests/PlayMode/T240/*.cs`、`Assets/_Game/Tests/PlayMode/T600/*.cs`、其他精确命中配置冻结值的既有测试：仅同步新content/hash/记录数冻结断言。
- `Assets/_Game/Scripts/Levels/TutorialFlow.cs`、`Assets/_Game/Scripts/Levels/TutorialLevelCoordinator.cs`：增加显式Skip协议与完成后PlayerConfirmed门的安全释放；不改变配置动作完成规则。
- `Assets/_Game/Scripts/Levels/ProgressSave.cs`、`Assets/_Game/Scripts/Levels/ResultService.cs`：增加教程完成ID的一次性进度标记、save v1→v2内建迁移与配置目录校验。
- `Assets/_Game/Scripts/Presentation/BattleHudView.cs`、`Assets/_Game/Scripts/Presentation/BattleHudViewFactory.cs`：只公开教程遮罩需要的SafeArea/架势/终极目标引用。
- `Assets/_Game/Scripts/Presentation/Tutorial*.cs`及Unity生成的`.meta`：新增纯事件驱动Director、状态合同、目标注册表、遮罩/手势View与可组合Runtime工厂。
- `Assets/_Game/Tests/EditMode/T650/**`、`Assets/_Game/Tests/PlayMode/T650/**`及Unity生成的`.meta`：新增TutorialGateTests与TutorialSkipPlayModeTests。
- `Assets/_Game/Tests/EditMode/T550/*.cs`：只同步save v2迁移/序列化冻结断言及教程标记回归。
- `Assets/_Game/Tests/EditMode/T610/*.cs`：若字形清单测试按Texts全集断言，只允许同步新文案覆盖断言；字体二进制与TMP资产不得变化（新中文仅使用现有字形）。
- `Config/README.md`、`docs/CONFIG_SCHEMA.md`、`docs/TECH_SPEC.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：同步content版本、教程表现/一次性进度协议、测试与边界说明；Schema和字段字典不变。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：T650生命周期、验证统计、配置hash/记录数和下一任务状态。
- `artifacts/evals/T650/**`：Git基线、工作簿渲染与检查、配置/Unity日志和XML、玩家路径、最终白名单审查及verification证据。
- `artifacts/tmp/T650-*/**`：不提交的工作簿与Unity隔离验证临时目录。

## 禁止改动

- 上述白名单外的用户文件和产品代码；所有Scene/Prefab/Registry/Input Actions/Packages/ProjectSettings/微信SDK/Builds；T640、T700及后续任务实现；现有字体TTF、charset、TMP字体资产；外部PSD和用户提供素材。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。

## 实际改动审查

- 配置闭环：正式/镜像/交付xlsx、受管JSON/hash/ConfigIds、样例、冻结测试、配置文档与渲染/日志证据。
- 产品代码：仅`Levels`中的教程跳过/最终门和进度v2，以及`Presentation`中的HUD目标暴露、Director、View和工厂。
- 测试：新增T650 EditMode/PlayMode及Unity生成`.meta`，并只同步受影响的存档/配置冻结断言。
- 文档与证据：`TASKS/PROGRESS/project-index`、技术/配置/测试/决策合同、T650日志/XML/工作簿预览/截图。
- 禁止区差异：Scene 0、Prefab 0、Registry 0、Input Actions 0、Packages 0、ProjectSettings 0、微信SDK 0、Builds 0、字体二进制/TMP资产 0。
