# T310 Change Whitelist

- Git基线：`0f145d4085587b58d73c51e0a2261435325af2cb`（`main`，开始时工作树干净）。
- 需要保护的用户已有改动：无；若执行中出现白名单外改动，停止并审查来源。
- 任务目标：实现配置阈值驱动的纯C#笔迹采样、不可变结果、最小点距过滤、最大长度精确裁剪、稳定点数上限，并通过`IPointerInput`事件形成可复用的一笔采集器。
- 明确不做：不实现RDP、重采样、笔势识别、轨迹表现、命中、场景/Prefab/YAML改动，也不恢复T120/T130。

## 预计改动白名单

- `Assets/_Game/Scripts/Input/*.cs{,.meta}`：新增纯采样数据/规则/状态机及统一输入采集器；必要时补充测试可见性。
- `Assets/_Game/Scripts/Combat/*.cs{,.meta}`：仅允许新增`StrokeRuleConfig`到采样设置的显式映射，保持Input程序集不依赖Config。
- `Assets/_Game/Tests/EditMode/T310/**`：新增边界与配置映射专项测试及Unity元文件。
- `Assets/_Game/Tests/PlayMode/T310/**`：仅在需要验证真实统一输入事件链时新增专项测试及Unity元文件。
- `Assets/_Game/Tests/{EditMode,PlayMode}/*.asmdef`：仅允许添加测试所需的既有程序集引用。
- `artifacts/evals/T310/**`：基线、白名单、测试XML/日志、真实路径记录与最终验证结论。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：完成后同步任务状态、验证数字和下一任务。

## 禁止改动

- 禁止修改Excel、Schema、导出器、生成JSON/hash/ConfigIds、Input Actions、Packages、ProjectSettings、Unity场景/Prefab/Asset、微信SDK与构建产物。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
