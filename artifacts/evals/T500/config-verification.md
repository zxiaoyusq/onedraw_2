# T500 Config Verification

- 命令：`Tools/CI/verify-config.sh --skip-unity`
- 结果：PASS（退出码0）；Unity分类由本任务MCP专项和全量回归独立覆盖，因此脚本按约定只报告`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN reason=explicit_skip`，未伪报完整一键PASS。
- ConfigExporter：Debug构建0 warning/0 error；.NET测试56/56通过、0失败、0跳过。
- 正式工作簿只读导出：schema `4`、content `0.5.1-sample`、28表、662条，content hash `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`。
- 三受管生成物漂移：JSON 172,699字节、hash旁车65字节、ConfigIds 23,885字节，逐字节比对PASS；本任务未修改xlsx、FieldDictionary、Schema、导出器、JSON、hash、ConfigIds或DTO。

结论：T500直接消费现有Levels/Waves/Spawns/SpawnPoints/EnemyModifiers查询合同，无需配置结构或内容变更。
