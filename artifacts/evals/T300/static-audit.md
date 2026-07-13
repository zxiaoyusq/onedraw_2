# T300 Static Audit

- `Tools/CI/verify-config.sh --skip-unity`：三生成物只读diff PASS；ConfigExporter build 0 warning/0 error；.NET 54/54 PASS；输出明确PARTIAL，Unity证据由MCP job独立记录。
- `Screen.dpi`、`UnityEngine.Input`旧API扫描：0命中。
- Input Runtime中的硬编码`1920/1080`：0命中；参考宽高只通过`ConfigIds.GlobalKeys.ReferenceWidth/ReferenceHeight`读取。
- Input、EditMode、PlayMode三个asmdef JSON解析：PASS；项目程序集依赖仍无环。
- 新增Unity脚本和T300测试`.meta`配对：PASS，全部由Unity 6000.5.1f1生成。
- xlsx、Schema、JSON/hash/ConfigIds、Packages、ProjectSettings、Input Actions、场景、Prefab和Registry差异：0。
- `git diff --check`：PASS。
