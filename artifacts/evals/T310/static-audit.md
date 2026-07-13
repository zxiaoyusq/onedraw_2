# T310 Static Audit

- 4个相关asmdef均为合法JSON；Runtime依赖仍为Input→Core、Combat→Core/Config/Input，无循环依赖。
- `project-index.yaml`可解析，当前任务为T320且`stroke_sampling.status=pass`。
- 5个新增Runtime C#、2个新增测试C#均存在Unity `.meta`配对。
- 新增Runtime采样代码中`new List`、`new GameObject`、`Screen.dpi`和硬编码`ConfigIds.StrokeRules`命中数均为0。
- 配置只读检查：三生成物逐字节diff PASS，ConfigExporter构建0 warning/0 error，.NET 54/54；见`config-verify.log`。
- `git diff --check`：PASS。
- Excel、Schema、导出器、生成JSON/hash/ConfigIds、Input Actions、Packages、ProjectSettings、场景、Prefab和微信SDK最终差异均为0。
