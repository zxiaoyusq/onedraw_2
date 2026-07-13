# T240 Config Regression Evidence

- 环境无全局`dotnet`；按`docs/CONFIG_PIPELINE.md`使用Unity `6000.5.1f1`随附`.NET 8 SDK 8.0.318`。
- 测试命令通过`-p:UseAppHost=false`执行，避免macOS Unity SDK对已签名apphost重复签名；结果：ConfigExporter tests total 46 / passed 46 / failed 0 / skipped 0。
- 使用测试生成的`OneStrokeDemon.ConfigExporter.dll`执行正式只读严格校验；结果：

```text
CONFIG_VALIDATION_PASS
schema=1 content=0.1.1-sample hash=16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c tables=28 records=645 strict=True
VALIDATION_SCOPE=T220_PRODUCTION_CONFIG_CONTRACT
```

- T240未修改正式工作簿、镜像、Schema或`Assets/_Game/Config/Generated/gameplay_config.json`。
