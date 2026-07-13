# T510 Config Verification

- 命令：`Tools/CI/verify-config.sh --skip-unity`（前台Unity Editor占用工程，Unity部分由同轮全量Unity回归覆盖）。
- 结果：ConfigExporter构建成功，0 warning / 0 error；临时导出schema 4、content 0.5.1-sample、28表662条，与受管JSON/hash/ConfigIds三项逐字节一致。
- .NET：56/56通过。
- Unity配置测试：未在该脚本内重复启动批处理实例；最终全量EditMode 150/150与PlayMode 37/37包含既有ConfigPipeline分类测试。
- 配置写入：无。未使用`--update`，xlsx、Schema、FieldDictionary、JSON、hash、ConfigIds和DTO均无改动。
- 原始日志：`config-verification.log`。
