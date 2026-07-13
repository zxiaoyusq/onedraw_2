# T450 Config Verification

- 执行：`Tools/CI/verify-config.sh --skip-unity`；静态/导出层PASS，按参数正确报告`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN`。Unity层由同版本已打开Editor的MCP job独立完成。
- ConfigExporter build：0 warning / 0 error。
- ConfigExporter .NET tests：56/56通过。
- 导出摘要：schema `4`，content `0.5.0-sample`，hash `d524ffcda4693c9cb65e5e21d5ab753472a14b2233b2ae670ecc4b81f1251ee8`，28表/660条，JSON 172,045字节，27组/313个ID常量。
- 三生成物漂移：0；JSON/hash/ConfigIds均未修改。
- 受管生成物SHA-256：JSON `e86bef20...b1e`，hash旁车 `6b638f90...c57`，ConfigIds `0be18d32...115`。
- 正式/镜像工作簿SHA-256均为`cc5e9b11...c42`，字节一致；T450未修改配置表内容或字段合同。
