# T330 Change Whitelist

- Git基线：`5be320dc9a1824a96bec2e9bd7e6db7a725926ea`（`T320: implement stroke geometry`），开始时`git status --short --branch`仅为`## main`。
- 需要保护的用户已有改动：无；开始时工作树干净。若执行中出现白名单外改动，停止并保留，不覆盖。
- 任务目标：实现纯C#、配置驱动的Any/Horizontal/Vertical/Diagonal/Arc/Circle/Charged分类，输出确定性置信度和几何摘要；补齐真正的起笔停留时长与StrokeRules只读全表映射。
- 明确不做：不实现T340轨迹、T350命中、机器学习或书法级识别；不修改玩法配置源及其生成物；不恢复T120/T130。

## 预计改动白名单

- `Assets/_Game/Scripts/Input/StrokeData.cs`：在不可变笔迹元数据中增加首个有效移动前的停留时长。
- `Assets/_Game/Scripts/Input/StrokeSampler.cs`：在不改变采样阈值/裁剪行为的前提下记录首个有效采样时间。
- `Assets/_Game/Scripts/Input/StrokeGeometryData.cs`：向分类器透传首段停留元数据。
- `Assets/_Game/Scripts/Input/Gesture*.cs`及Unity生成的对应`.meta`：新增笔势类型、规则、匹配结果和纯分类器。
- `Assets/_Game/Scripts/Config/IConfigProvider.cs`、`GameplayConfigSnapshot.cs`、`GameplayConfigService.cs`：暴露稳定、只读的StrokeRules全表，不改变DTO或JSON合同。
- `Assets/_Game/Scripts/Combat/GestureRuleSetFactory.cs`及Unity生成的对应`.meta`：把配置行显式映射为Input纯规则并严格解析枚举。
- `Assets/_Game/Tests/EditMode/T330/**`：新增分类、边界、误识别、确定性、配置映射和停留时长回归测试及Unity元数据。
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅增加Combat测试依赖以验证真实配置映射。
- `Assets/_Game/Tests/PlayMode/T330/**`：新增Mouse到采样、几何和分类的真实玩家路径测试及Unity元数据。
- `artifacts/evals/T330/**`：Git基线、白名单、专项/全量测试、Console和玩家路径证据。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：完成后同步T330状态、验证摘要和下一READY任务。

## 禁止改动

- 禁止修改Excel、FieldDictionary、Schema、导出器、Runtime JSON/hash/ConfigIds、场景、Prefab、Input Actions、Packages、ProjectSettings或微信SDK/构建状态。
- 禁止提前实现T340/T350或将识别阈值复制到Inspector/ScriptableObject。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
