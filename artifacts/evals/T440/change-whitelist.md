# T440 Change Whitelist

- Git基线：`main` / `be5d4f6d0257ac65d559a9151e11ff3a85613b32`，开始时工作区为空。
- 需要保护的用户已有改动：无；若后续出现白名单外差异，先核实并保留，不覆盖或暂存。
- 任务目标：完成 T440 的通用 `ObjectPoolService`、`IPoolable`、配置化预热/容量/耗尽策略、敌人/投射物/VFX/伤害数字完整回收，以及泄漏与三次重开验证。
- 明确不做：T120/T130 微信开发者工具与打包；T450 六类敌人内容装配；T460 Boss阶段；T500关卡流程；场景、Prefab、Packages、ProjectSettings或微信SDK变更。

## 预计改动白名单

- `Assets/_Game/Scripts/Core/**`：新增无业务数值的池合同、池服务、租约/快照与泄漏报告；允许配套`.meta`。
- `Assets/_Game/Scripts/Config/*Pool*.cs`：把Global、Enemies和VfxCues中的容量、预热及耗尽策略映射为Core池定义；允许配套`.meta`。
- `Assets/_Game/Scripts/Actors/EnemyController.cs`、`Assets/_Game/Scripts/Combat/ProjectileController.cs`：接入`IPoolable`并保证回收清空运行态与外部订阅。
- `Assets/_Game/Scripts/Actors/*Pool*.cs`、`Assets/_Game/Scripts/Combat/*Pool*.cs`、`Assets/_Game/Scripts/Presentation/*Pool*.cs`：从配置映射敌人/投射物/VFX/伤害数字池定义，新增VFX与伤害数字池项；允许配套`.meta`。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/EditMode/T440/**`、`Assets/_Game/Tests/PlayMode/T440/**`及目录`.meta`：新增T440专项测试和Core直接引用。
- `Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`：内容版本、投射物预热及四类池耗尽策略配置；镜像保持字节一致。
- `Assets/_Game/Config/Generated/gameplay_config.json`、`Assets/_Game/Config/Generated/gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`：仅由配置工具生成/同步。
- `Assets/_Game/Scripts/Config/GameplayConfigCompatibility.cs`及受版本、记录数、hash、ID数量影响的`Assets/_Game/Tests/EditMode/T230/**`、`Assets/_Game/Tests/EditMode/T250/**`、`Assets/_Game/Tests/PlayMode/T230/**`、`Assets/_Game/Tests/PlayMode/T240/**`、`Tools/ConfigExporter/Tests/**`：同步冻结断言。
- `config/README.md`、`Tools/ConfigExporter/README.md`、`docs/CONFIG_PIPELINE.md`、`docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：记录content 0.5.x、对象池语义、生成物摘要及专项测试归层。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：只更新T440完成状态、下一任务和验证摘要。
- `artifacts/evals/T440/**`：Git基线、配置审查、测试XML/日志与最终验证记录。

## 禁止改动

- 禁止修改场景、Prefab、美术资源、`Packages/**`、`ProjectSettings/**`、微信SDK/构建产物以及T450及后续任务实现。

## 收尾审查

- [ ] `git status --short`中的每一项都属于白名单。
- [ ] `git diff --check`通过。
- [ ] 仅暂存白名单文件，并审查`git diff --cached`。
