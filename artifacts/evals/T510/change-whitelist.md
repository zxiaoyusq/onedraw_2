# T510 Change Whitelist

- Git基线：`0c517d932cb435bed92edb1e5fe347714e0bc421`（`main`）。
- 需要保护的用户已有改动：基线工作树干净，无用户未提交改动。
- 任务目标：实现配置驱动的`Countdown/Playing/UltimateDrawing/Paused/Victory/Defeat`状态机、玩家事件门、一次性互斥结算与统一战斗时间源，并接入T500关卡事实及T400/T410玩家与终极结果。
- 明确不做：不制作T520/T530/T540具体关卡接线，不实现HUD/结算界面/正式演出，不修改场景或Prefab，不恢复T120/T130微信平台工作。

## 预计改动白名单

- `Assets/_Game/Scripts/Levels/**`：新增无`MonoBehaviour`依赖的战斗流程状态机、配置映射、事件合同、统一时间源及必要的T500流程协调器。
- `Assets/_Game/Tests/EditMode/T510/**`：新增`BattleFlowTests`、`NoAdvanceBeforePlayerActionTests`及Unity生成的`.meta`。
- `Assets/_Game/Tests/PlayMode/T510/**`：新增暂停/失焦、终极事件门和互斥结算真实运行时路径测试及Unity生成的`.meta`。
- `artifacts/evals/T510/**`：Git基线、合同审计、测试XML、Unity/配置日志、玩家路径和最终验证证据。
- `docs/CONFIG_SCHEMA.md`、`docs/DECISIONS.md`、`docs/TEST_PLAN.md`：同步T510配置映射、时间域、同帧胜败优先级、暂停/失焦和玩家事件门合同。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验收通过后更新T510状态、证据、计数和下一任务。

## 条件性白名单

- `Assets/_Game/Scripts/Input/**`：仅当现有取消接口不足以让流程统一取消活动笔迹时，补充通用取消端口；不得加入关卡或技能数值。
- `Assets/_Game/Scripts/Actors/**`、`Assets/_Game/Scripts/Skills/**`：仅当现有事件/只读结果不足以向状态机提交死亡或终极结果时，补充通用事件适配；不得复制配置数值或改动既有结算公式。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅在T510测试需要现有未引用程序集时追加引用。
- 不预计修改xlsx、FieldDictionary、Schema、导出器、DTO或受管生成物；若现有配置不能表达T510合同，必须先修订白名单并完成整个配置闭环。

## 禁止改动

- 不修改`.unity`、`.prefab`、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds。
- 不提前实现T520/T530/T540关卡组装、T600 HUD、T610结算、T620表现、T630正式资源或后续平台任务。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单；Unity生成的脚本/测试目录`.meta`随对应白名单目录纳入。
- [x] `git diff --check`通过，场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、SDK和Builds均无diff。
- [x] 仅暂存白名单文件，并审查`git diff --cached --check`、stat和完整文件名清单。
