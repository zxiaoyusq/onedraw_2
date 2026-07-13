# T330 Static Audit

- `project-index.yaml`可解析，当前任务T340/READY；`docs/TASKS.md`总览和详情仅T340一个READY，T330为DONE。
- Runtime程序集边界保持Input→Core、Combat→Core/Config/Input；仅PlayMode测试程序集新增Combat直接引用，无循环依赖。
- 6个新增Runtime C#、2个新增测试C#及T330目录均有Unity生成`.meta`配对。
- 新增Runtime代码中MonoBehaviour、GameObject、`Time`、`Task.Run`、托管线程、运行时反射、LINQ、`Screen.dpi`、微信SDK调用和特定`ConfigIds.StrokeRules`命中均为0。
- 代码中的0/45/90/135/180只定义无向坐标轴与对角线几何语义；所有容差、长度、闭合、面积、曲率和停留阈值均来自配置。
- 配置只读检查：三生成物逐字节diff PASS，ConfigExporter构建0 warning/0 error，.NET 54/54；见`config-verify.log`。
- `git diff --check`和最终暂存diff检查PASS；Excel、Schema、导出器、JSON/hash/ConfigIds、Input Actions、Packages、ProjectSettings、场景、Prefab及微信SDK最终差异均为0。
