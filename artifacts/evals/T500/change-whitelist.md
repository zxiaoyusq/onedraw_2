# T500 Change Whitelist

- Git基线：`f3e511e65b9a8ebe68f1a162deccb54d715a6322` (`main`)。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：实现完全读取`Levels/Waves/Spawns/SpawnPoints/EnemyModifiers`的Level/Wave/Spawn时间轴、归一化出生区域、暂停时钟和`AllDefeated/TimeElapsed/BossDefeated`结束条件。
- 明确不做：不实现T510倒计时/暂停UI/终极/胜败状态机，不制作T520/T530/T540具体关卡内容，不修改场景手摆波次，不恢复T120/T130微信平台工作。

## 预计改动白名单

- `Assets/_Game/Scripts/Levels/**`：新增无`MonoBehaviour`依赖的配置目录、`SpawnScheduler`、`WaveRunner`、`LevelRunner`、显式世界端口与归一化出生采样。
- `Assets/_Game/Tests/EditMode/T500/**`：新增`SpawnTimelineTests`和Unity生成`.meta`。
- `Assets/_Game/Tests/PlayMode/T500/**`：新增`WaveRunnerPlayModeTests`和Unity生成`.meta`。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅增加`OneStrokeDemon.Levels`测试程序集引用。
- `artifacts/evals/T500/**`：基线、合同审计、测试XML、Unity/配置日志、玩家路径和最终验证证据。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：同步T500关卡时间轴与结束条件合同。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验收通过后更新T500状态、证据、计数和下一任务。

## 条件性白名单

- `Assets/_Game/Scripts/Config/**`：仅当现有显式查询API不足以只读获取T500表时，补充不改变DTO/Schema的数据访问接口与对应旧测试。
- `Assets/_Game/Scripts/Actors/**`：仅当T450池缺少T500所需的只读活动/释放适配点时，补充通用生命周期接口；不得加入关卡ID或刷怪数值。
- 不预计修改xlsx、FieldDictionary、Schema、导出器或受管生成物；若确认当前表无法表达T500验收，必须先修订白名单并完成整个配置闭环。

## 禁止改动

- 不修改`.unity`、`.prefab`、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不提前实现T510及后续战斗流程、教程、结算、HUD和正式资源。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单；Unity生成的脚本/测试目录`.meta`随对应白名单目录纳入。
- [x] `git diff --check`通过，场景、Prefab、Registry、Packages、ProjectSettings、SDK和Builds均无diff。
- [x] 仅暂存上述34个白名单文件，并已审查`git diff --cached --check`、stat和完整文件名清单。
