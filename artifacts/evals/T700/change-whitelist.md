# T700 Change Whitelist

- Git基线：`main@3ab3dd48c6738152ca68dab87d5116cfaed511e8`；`git status --short --branch`仅输出`## main`。
- 需要保护的用户已有改动：无；基线工作树干净。后续如出现白名单外差异，先判定来源并停止覆盖。
- 任务目标：审计并补齐纯规则EditMode回归矩阵，覆盖手势、伤害/连斩/评分、配置契约、技能效果、玩家/敌人/战斗状态机和Boss阶段的边界、无效输入、重复事件与顺序独立性。
- 明确不做：不使用Scene/PlayMode验证纯算法；不实现T710集成/E2E、T720配置覆盖扫描、T730性能任务或T640布局；不修改配置工作簿/生成物、Scene、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；不恢复T120/T130。

## 预计改动白名单

- `Assets/_Game/Tests/EditMode/T700/**`及Unity Editor生成的`.meta`：新增纯规则回归矩阵、局部测试数据建造器与明确的`T700`分类；不引入Scene或PlayMode依赖。
- `Assets/_Game/Tests/EditMode/**`：只在审计发现现有纯规则断言本身错误、不稳定或缺少Category时做最小修正；不重写已稳定的历史测试。
- `Assets/_Game/Scripts/{Input,Combat,Actors,Skills,Levels}/**/*.cs`：仅允许修复T700新回归测试直接暴露的纯规则正确性缺陷；必须保持配置驱动和无`MonoBehaviour`依赖，若无缺陷则不修改产品代码。
- `docs/TEST_PLAN.md`：记录T700纯规则矩阵、顺序独立性和反重复事件验收口径。
- `docs/DECISIONS.md`、`docs/BUGS.md`：仅在测试审计导致新的规则决策或发现/修复缺陷时更新。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：更新T700生命周期、测试统计、矩阵摘要与下一个可执行任务。
- `artifacts/evals/T700/**`：Git基线、覆盖审计/矩阵、专项与全量Unity日志/XML、配置只读校验、最终白名单审查及`verification.md`。
- `artifacts/tmp/T700-*/**`：不提交的Unity隔离验证副本与临时输出。

## 禁止改动

- 上述白名单外的用户文件和产品代码；所有xlsx/JSON/hash/ConfigIds、Scene/Prefab/Registry/Input Actions/Packages/ProjectSettings/微信SDK/Builds、美术/字体/音频资产；T710及后续任务实现。

## 收尾审查

- [ ] `git status --short`中的每一项都属于白名单。
- [ ] `git diff --check`通过。
- [ ] 仅暂存白名单文件，并审查`git diff --cached`。
