# T320 Static Audit

- 4个相关asmdef均为合法JSON；Runtime依赖仍为Input→Core、Combat→Core/Config/Input，无循环依赖或新增程序集引用。
- `project-index.yaml`可解析，当前任务为T330且`stroke_geometry.status=pass`；TASKS总览只有T330一个READY任务。
- 4个新增Runtime C#、2个新增测试C#均存在Unity `.meta`配对。
- 新增Runtime几何代码中MonoBehaviour、`Time`、`Task.Run`、GameObject、`Screen.dpi`、特定`ConfigIds.StrokeRules`、LINQ和List命中数均为0。
- 配置只读检查：三生成物逐字节diff PASS，ConfigExporter构建0 warning/0 error，.NET 54/54；见`config-verify.log`。
- `git diff --check`：PASS。
- Excel、Schema、导出器、生成JSON/hash/ConfigIds、T310采样文件、Input Actions、Packages、ProjectSettings、场景、Prefab和微信SDK最终差异均为0。
