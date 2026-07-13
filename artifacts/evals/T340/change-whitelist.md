# T340 Change Whitelist

- Git基线：`793172eb08305b7b7831339c2e9550c62ea2b728`（`T330: implement gesture classification`），分支`main`，开始时工作树干净。
- 需要保护的用户已有改动：无；本目录由`Tools/CI/new-task-evidence.sh T340`在该基线上新建。
- 任务目标：实现消费T320同一不可变点集的低分配`StrokeTrailView`、配置驱动刀/符样式、淡出和固定容量对象池，并以PlayMode及真实Mouse玩家路径验证。
- 明确不做：不实现T350命中或碰撞；不让视觉决定分类/命中；不修改配置内容、场景、Prefab、输入动作、Packages、ProjectSettings或平台状态；不实例化逐段材质。

## 预计改动白名单

- `Assets/_Game/Scripts/Combat/StrokeTrailPath.cs`及`.meta`：在Combat边界把T320几何结果桥接为不复制点集的只读轨迹路径。
- `Assets/_Game/Scripts/Presentation/StrokeTrailSettings.cs`及`.meta`：定义经验证的轨迹池与样式值对象。
- `Assets/_Game/Scripts/Presentation/StrokeTrailSettingsFactory.cs`及`.meta`：从现有Stances、StrokeRules和VfxCues建立运行时设置。
- `Assets/_Game/Scripts/Presentation/StrokeTrailView.cs`及`.meta`：LineRenderer显示、淡出、回收前完整重置和共享材质约束。
- `Assets/_Game/Scripts/Presentation/StrokeTrailPool.cs`及`.meta`：固定数组预热、最多三条活动残留、最旧复用与无热路径容器分配。
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`：仅增加Presentation测试依赖。
- `Assets/_Game/Tests/PlayMode/T340/**`及目录`.meta`：池复用、共享点集、配置切换、淡出、分配预算和真实Mouse玩家路径测试。
- `ProjectSettings/TagManager.asset`：仅通过Unity Editor API新增现有`VfxCues.sortingLayer`所引用的`VFX` Sorting Layer，使配置排序不静默退回Default。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：仅在验证通过后同步T340完成、计数和下一READY任务。
- `artifacts/evals/T340/**`：基线、白名单、配置/静态/Unity测试/玩家路径和最终验证证据。

## 禁止改动

- `GameConfig.xlsx`、`FieldDictionary.md`、Schema/导出器、生成JSON/hash/ConfigIds等配置内容与流水线。
- `Assets/_Game/Scenes/**`、Prefab、美术资源、Input Actions、`Packages/**`、除上述`TagManager.asset`单项外的`ProjectSettings/**`和微信SDK/DevTools状态。
- T350及后续任务的命中、伤害、投射物或战斗流程实现。

## 收尾审查

- [ ] `git status --short`中的每一项都属于白名单。
- [ ] `git diff --check`通过。
- [ ] 仅暂存白名单文件，并审查`git diff --cached`。
