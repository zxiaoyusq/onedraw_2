# 01_NEXT_TASK：执行下一个原子任务

完整读取并遵守 `AGENTS.md`。

## 选择任务

1. 读取 `project-index.yaml` 和 `docs/TASKS.md`。
2. 只选择第一个状态为`READY`且依赖全部为`DONE`的任务。
3. 若没有这样的任务，说明阻塞链，不得自行跳过依赖。
4. 任何时刻只做一个任务。

## 实施前必须输出

- 当前任务ID、目标和依赖状态。
- 明确不做项。
- 预计修改文件白名单。
- 相关配置表、运行时代码、测试、场景/Prefab和证据。
- Git工作树基线及需要保护的已有改动。
- 专项EditMode、PlayMode和真实玩家路径。
- Unity/MCP/DevTools/真机等外部工具门。

## 实施规则

- 代码、Markdown、JSON、asmdef使用普通文件工具。
- 场景、Prefab、资源导入、Play Mode和Test Runner优先使用Unity Editor/MCP。
- 不手工编辑`.unity`、`.prefab`或`.asset` YAML。
- 纯规则不依赖MonoBehaviour，并有EditMode测试。
- 所有玩法内容来自配置；禁止在Inspector或C#添加第二份平衡值。
- 玩家确认必须由事件推进。
- 对象池对象必须在启用和回收两端完整重置。
- 缺外部工具时如实BLOCKED，不伪造完成。

## 最小反馈环

```text
小范围修改
→ 静态/导出校验
→ Unity Refresh和编译
→ Console Error
→ 专项EditMode
→ 专项PlayMode
→ 真实玩家路径
→ 必要时全量回归
→ git diff --check/status/白名单
→ 保存证据
```

## 收尾

- 更新 `artifacts/evals/TASK-ID/verification.md`。
- 更新 `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`。
- 必要时更新 `docs/DECISIONS.md` 或 `docs/BUGS.md`。
- 只暂存当前任务文件，审查`git diff --cached`。
- 提交信息以任务ID开头。
- 提交后停止。
