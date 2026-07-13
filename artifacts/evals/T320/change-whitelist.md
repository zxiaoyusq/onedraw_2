# T320 Change Whitelist

- Git基线：`c438bbdc14ae880967908aaa9fccd6e1f91bad44`（`main`，开始时工作树干净）。
- 需要保护的用户已有改动：无；若执行中出现白名单外改动，停止并审查来源。
- 任务目标：实现纯C#的笔迹RDP简化、按弧长重采样、长度、包围盒、面积、闭合距离/比和稳定曲率指标，并产出供识别、视觉、命中共享的不可变处理结果。
- 明确不做：不实现T330笔势分类、不实现T340轨迹表现、不实现T350命中；不修改采样数值语义、场景、Prefab或平台任务。

## 预计改动白名单

- `Assets/_Game/Scripts/Input/StrokeGeometry*.cs{,.meta}`：新增无MonoBehaviour依赖的几何算法、设置和不可变处理结果。
- `Assets/_Game/Scripts/Combat/StrokeGeometrySettingsFactory.cs{,.meta}`：仅新增`StrokeRuleConfig`到RDP/最大处理点数设置的显式映射。
- `Assets/_Game/Tests/EditMode/T320/**`：新增直线、折线、弧、圆、重复点、极短输入和确定性测试及Unity元文件。
- `Assets/_Game/Tests/PlayMode/T320/**`：新增真实Input System笔迹进入统一几何结果的玩家路径测试及Unity元文件。
- `artifacts/evals/T320/**`：基线、白名单、测试/配置日志、几何语义、真实路径和最终验证结论。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：完成后同步任务状态、验证数字和下一任务。

## 禁止改动

- 禁止修改Excel、Schema、导出器、生成JSON/hash/ConfigIds、既有T310采样合同、Input Actions、Packages、ProjectSettings、Unity场景/Prefab/Asset、微信SDK与构建产物。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
