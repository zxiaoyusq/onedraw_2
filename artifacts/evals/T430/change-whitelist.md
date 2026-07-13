# T430 Change Whitelist

- Git基线：`main` / `6d074e3f4ca640220e984f97aad428fb061a5b4b`，开始时工作区为空。
- 需要保护的用户已有改动：无；后续若出现白名单外改动，一律视为用户/工具临时改动并先核实来源。
- 任务目标：完成 T430 的配置驱动敌人移动、攻击、防御策略注册表与攻击预警，并为灵偶护盾补齐可复用减伤语义。
- 明确不做：T120/T130 微信开发者工具与打包；T440 对象池；T450 内容装配；场景、Prefab、ProjectSettings、Packages 或微信 SDK 变更。

## 预计改动白名单

- `Assets/_Game/Scripts/Actors/**`：新增策略注册表、策略运行时、防御规则、攻击预警，并扩展敌人 Buff 容器的配置驱动减伤语义；允许配套 `.meta`。
- `Assets/_Game/Tests/EditMode/T430/**`、`Assets/_Game/Tests/PlayMode/T430/**`：新增 T430 专项测试与 `.meta`。
- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：同步 Schema/内容版本、`DamageReduction` 枚举、护盾 Buff 与技能效果引用；前者为唯一内容源，后者保持字节一致镜像。
- `config/schema/gameplay.schema.json`、`config/examples/gameplay_config.sample.json`：同步 Schema v4 与导出样例。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`：由配置导出器生成。
- `Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`：升级受支持 Schema/内容版本。
- `Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T250/**`、`Assets/_Game/Tests/PlayMode/T230/**`、`Assets/_Game/Tests/PlayMode/T240/**`、`Tools/ConfigExporter/Tests/**`：只允许更新受版本、记录数与 hash 影响的冻结断言。
- `Tools/ConfigExporter/README.md`、`docs/CONFIG_PIPELINE.md`、`docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`：记录 Schema v4、护盾减伤与策略语义。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：只更新 T430 状态、证据与配置指纹。
- `artifacts/evals/T430/**`：基线、测试结果、日志和最终验证记录。

## 禁止改动

- 禁止修改场景、Prefab、美术资源、`Packages/**`、`ProjectSettings/**`、微信 SDK/构建产物以及 T440 及后续任务实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
