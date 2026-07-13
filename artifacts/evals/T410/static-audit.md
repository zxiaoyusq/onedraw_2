# T410 Static Audit

- `Tools/CI/verify-config.sh --skip-unity`：PASS；ConfigExporter构建0错误/0警告，.NET测试56/56，三份受管产物漂移检查PASS。Unity部分明确以`--skip-unity`标记NOT RUN，已由独立MCP Job覆盖。
- JSON：Schema、样例、受管JSON、无效配置fixture均通过`jq empty`。
- `project-index.yaml`通过Ruby YAML解析。
- `git diff --check`：PASS。
- 双工作簿`cmp`：PASS。
- Skills目录反射API扫描：0命中。
- Skills目录`: MonoBehaviour`扫描：0命中。
- 任务状态：T410=`DONE`，依赖满足后的首个任务T420=`READY`。
- 禁止范围审查：场景、Prefab、Input Actions、Packages、ProjectSettings、Combat目录、微信SDK均无任务差异。

## 产物哈希

- 工作簿：`eb7cd040298bcf9c6b9a86dcc46b971663e4e4698153702f0ead15491e1311e3`
- `gameplay_config.json`：`4cb5b40f8bd314464c060b638bb1c00f5742466e6e7340783b41d8a1cf51f571`
- `gameplay_config.hash`：`148a9322614d03d12f3416197916f39270848c27640470f6a4c9da83c2514340`
- `ConfigIds.g.cs`：`75001b6e7703003e5be8cf8f4ba054a84f587ee8d51a758e11ccf029c8a38d2b`
