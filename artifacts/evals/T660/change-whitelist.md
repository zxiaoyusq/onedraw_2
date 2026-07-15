# T660 Change Whitelist

- Git基线：`main@3ab3dd48c6738152ca68dab87d5116cfaed511e8`。
- 需要保护的用户已有改动：用户已初始化Git；本任务开始时没有用户未提交改动。工作树中的`docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`和`artifacts/evals/T700/**`均为刚暂停的T700审计记录；保留T700证据但不纳入T660提交，三份状态文档只允许做本次任务重排与T660记录。
- 任务目标：新增生产可玩入口与Battle组合根，使玩家从`Bootstrap -> MainMenu`点击正式按钮，选择普通关或Boss关，进入由真实配置、输入、敌人/波次、HUD、教程、结算和重开组成的Battle运行时。
- 明确不做：不继续T700测试矩阵；不实现T710/T720/T730或依赖T120的T640；不恢复微信SDK、转换、开发者工具、真机或正式打包；不把玩法数值/敌人/波次/文案写入Inspector或C#；不追求最终商业美术与动画品质。

## 预计改动白名单

### 2026-07-15 人工评审修复增补

- 评审基线：`main@9d2f35e06ee56b5835e76ece774594d3873f0f9b`；工作树仅有受保护且保持未跟踪的`artifacts/evals/T700/**`。
- 现场缺陷：生产战斗没有装配T340既有`StrokeTrailPool`，完成笔迹虽可结算但玩家看不到轨迹；技能效果的`audioKey`协议ID被直接当作AssetRegistry资源键，切换架势时以`ARREG009 sfx_switch`失败。
- 本轮只允许修改`Assets/_Game/Scripts/Bootstrap/BattleCompositionRoot.cs`、`Assets/_Game/Scripts/Bootstrap/ProductionBattleWorld.cs`、`Assets/_Game/Tests/PlayMode/T660/ProductionPlayableEntryPlayModeTests.cs`及本任务证据/状态文档；如Unity导入产生现有脚本`.meta`变化则拒绝纳入。
- 继续禁止修改Scene/Prefab/Registry/Input Actions、玩法配置、Packages、ProjectSettings、微信SDK、Builds与`artifacts/evals/T700/**`。音频修复必须沿`AudioCues.audioKey -> assetKey`既有配置关系解析，不新增第二映射或吞掉未知键。

- `Assets/_Game/Scripts/Bootstrap/**`：新增主菜单入口、关卡选择、跨场景启动意图与Battle生产组合根；允许更新程序集引用。
- `Assets/_Game/Scripts/Actors/**`、`Assets/_Game/Scripts/Levels/**`、`Assets/_Game/Scripts/Presentation/**`：仅允许补充生产组合所需、可复用且不复制规则的运行时适配器/只读端口。
- `Assets/_Game/Scripts/Editor/**`：仅允许新增Unity Editor场景装配命令；场景和资源引用必须由Unity Editor写入。
- `Assets/_Game/Scenes/MainMenu.unity`、`Assets/_Game/Scenes/Battle.unity`：仅允许Unity Editor生成的正式入口/组合根组件变更，禁止手工编辑YAML。
- `Assets/_Game/Tests/EditMode/T660/**`、`Assets/_Game/Tests/PlayMode/T660/**`：新增菜单选择、组合根和真实玩家路径回归测试；`.meta`只由Unity Editor生成。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`：仅允许为T660纯规则测试补充Bootstrap程序集引用。
- `Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T250/**`、`Assets/_Game/Tests/EditMode/T610/**`、`Assets/_Game/Tests/PlayMode/T230/**`、`Assets/_Game/Tests/PlayMode/T240/**`、`Assets/_Game/Tests/PlayMode/T300/**`、`Assets/_Game/Tests/PlayMode/T600/**`、`Assets/_Game/Tests/PlayMode/T610/**`、`Assets/_Game/Tests/PlayMode/T620/**`、`Assets/_Game/Tests/PlayMode/T650/**`：仅允许同步冻结配置版本/hash/记录数/ID计数断言、字库字符清单断言、正式菜单UI存在后的测试隔离，以及接口扩展导致的最小测试桩适配。
- `Assets/_Game/Art/UI/Fonts/OneStrokeDemonUI.charset.txt`、`Assets/_Game/Art/UI/Fonts/OneStrokeDemonUI-Regular.ttf*`、`Assets/_Game/Art/UI/Fonts/FONT_SOURCE.md*`、`Assets/_Game/Art/UI/Fonts/Resources/Fonts/OneStrokeDemon UI Latin SDF.asset*`、`Assets/_Game/Art/UI/Fonts/Resources/Fonts/OneStrokeDemon UI Chinese SDF.asset*`、`Assets/TextMesh Pro/Resources/TMP Settings.asset*`：仅允许同步本任务新增配置文案字符、按固定上游重建交付TTF子集，并由既有Unity Editor字体生成器重建静态TMP字库及引用。
- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`、`config/field-dictionary.yaml`、`config/schema/**`、`config/examples/gameplay_config.sample.json`、`Assets/_Game/Config/Generated/**`、`Assets/_Game/Scripts/Config/**`：仅在入口新文案/配置确有需要时同步工作簿、字段字典、导出结果、DTO/ID与验证；不新增Inspector数值库。
- `Tools/ConfigExporter/Tests/**`：仅允许同步本次内容版本、冻结hash、记录数和ID常量数断言；不改变导出规则。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`docs/TECH_SPEC.md`、`docs/TEST_PLAN.md`、`docs/DECISIONS.md`、`project-index.yaml`：任务重排、T660合同、配置冻结基准、生产入口语义、字库派生基线与验证结果。
- `artifacts/evals/T660/**`：基线、命令日志、测试XML、截图和最终验证证据。
- Unity自动生成的上述新增目录/脚本对应`.meta`：仅允许Unity Editor导入后生成的GUID元数据。

## 禁止改动

- `artifacts/evals/T700/**`不纳入T660提交；除状态重排外不继续T700实现。
- 不修改`Assets/_Game/Input/**`、`Packages/**`、`ProjectSettings/**`、`Assets/WechatSDK/**`、`Builds/**`、现有Prefab/Registry/Input Actions或用户提供的PSD源文件。
- 不在本任务范围内的文件、资源和外部状态。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单；受保护的`artifacts/evals/T700/**`保持未跟踪且不纳入提交。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
