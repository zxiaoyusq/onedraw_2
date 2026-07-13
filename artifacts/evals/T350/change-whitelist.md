# T350 Change Whitelist

- Git基线：`b2f91de940a40e0d8465e7246afb0ec06ab87c3f`（`T340: implement pooled stroke trails`），分支`main`，开始时工作树干净。
- 需要保护的用户已有改动：无；本目录由`Tools/CI/new-task-evidence.sh T350`在该基线上新建。
- 任务目标：实现消费T320同一处理点集的配置半径分段扫圆/胶囊NonAlloc命中、归一化路径排序、同目标同笔去重、弱点聚合，以及包含strokeId/目标/笔势/时间的`HitRecord`。
- 明确不做：不执行T360伤害、方向奖励、连斩、评分或能量；不实现T370投射物规则；不动态创建轨迹Collider；不修改配置内容、场景或Prefab；不恢复T120/T130。

## 预计改动白名单

- `Assets/_Game/Scripts/Combat/IHittable.cs`及`.meta`：定义逻辑目标和可选弱点Hitbox边界，不实施伤害。
- `Assets/_Game/Scripts/Combat/HitRecord.cs`及`.meta`：定义不可变命中记录，携带任务要求的追溯字段。
- `Assets/_Game/Scripts/Combat/StrokeHitSettings.cs`及`.meta`：定义每条配置规则半径和固定查询/唯一目标容量。
- `Assets/_Game/Scripts/Combat/StrokeHitSettingsFactory.cs`及`.meta`：从所选StrokeRule及Global活动上限建立命中设置。
- `Assets/_Game/Scripts/Combat/StrokeHitQuery.cs`及`.meta`：定义无分配候选缓冲和可替换查询接口。
- `Assets/_Game/Scripts/Combat/StrokeHitResolver.cs`及`.meta`：遍历同一简化点集、去重、弱点聚合、排序并写调用方缓冲。
- `Assets/_Game/Scripts/Combat/Physics2DStrokeHitQuery.cs`及`.meta`：使用Unity 6数组重载`Physics2D.CircleCast`实现等价分段胶囊NonAlloc查询缓存。
- `Assets/_Game/Tests/EditMode/T350/**`及目录`.meta`：配置映射、排序/去重/弱点/失配/容量/确定性和零分配纯规则测试。
- `Assets/_Game/Tests/PlayMode/T350/**`及目录`.meta`：真实Collider2D、同体弱点、多目标排序、共享T340点集、Mouse玩家路径和Physics2D热路径测试。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验证通过后同步T350完成、测试计数和下一READY任务。
- `artifacts/evals/T350/**`：基线、白名单、配置/静态/Unity测试/玩家路径及最终验证证据。

## 禁止改动

- `Design/Config/GameConfig.xlsx`、镜像工作簿、FieldDictionary、Schema、导出器、生成JSON/hash/ConfigIds和Config DTO。
- `Assets/_Game/Scenes/**`、Prefab、美术资源、Input Actions、`Packages/**`、`ProjectSettings/**`和微信SDK/DevTools状态。
- Actors敌人状态机/Damageable/WeakpointController、T360伤害/连斩/评分、T370投射物和后续任务实现。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
