# T460 Config Verification

- 命令：`Tools/CI/verify-config.sh --skip-unity`
- 结果：PASS（退出码0）；脚本按明确参数只运行静态层，因此结尾为`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN reason=explicit_skip`，Unity专项与全量结果另见原生XML。
- ConfigExporter：Debug build 0 warning / 0 error；.NET 56/56通过。
- 只读重生成与漂移门：28表、662条；JSON/hash/ConfigIds三生成物与正式受管文件字节一致。
- 元数据：schema `4`；content `0.5.1-sample`；content hash `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`。
- 生成物：JSON 172699字节，hash 65字节，ConfigIds 23885字节；27组315个常量。
- 文件SHA-256：JSON `da938b0d1c056d29358415ff5868ba302d76686a58c43388fc0847ea0991884d`；hash旁车 `224a89e892a96cbb26174ae4921fe022ba2e09ef7e97d58552aa82964f91c720`；ConfigIds `6c838111d1087014fc182d02f6ee0fe8c336f72c28b35691f1e271c0d85aa44e`。
