# T440 Configuration Verification

- 命令：`Tools/CI/verify-config.sh --skip-unity`
- 结论：静态、导出、漂移与.NET测试全部通过；按参数预期以`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN reason=explicit_skip`结束，Unity专项与全量回归由同版本已打开Editor的MCP独立执行。
- Schema/content/hash：`4` / `0.5.0-sample` / `d524ffcda4693c9cb65e5e21d5ab753472a14b2233b2ae670ecc4b81f1251ee8`
- 数据：28表 / 660条；JSON 172,045字节；hash旁车65字节；27组/313个ID常量。
- ConfigExporter：构建0 warning / 0 error，.NET 56/56。
- 生成物：受管JSON/hash/ConfigIds与同输入临时重生成字节一致。
- 双工作簿SHA-256：`cc5e9b1136b8fe316d98c845613e126937a44d06dc20690874dba0d009784c42`，字节一致。
- 视觉复核：使用`@oai/artifact-tool`导入/修改/检查/渲染；公式错误扫描0，`workbook-tools/previews/`中的README、Global、Enums无裁切或布局异常。
