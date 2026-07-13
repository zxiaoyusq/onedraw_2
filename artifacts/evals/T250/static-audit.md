# T250 Static Audit

- `git diff --check`：PASS。
- ConfigExporter build：0 warning / 0 error。
- ConfigExporter Tests：54/54 PASS。
- ConfigExporter与Tests的`dotnet format --verify-no-changes --no-restore`：PASS。
- `bash -n`覆盖`verify-config.sh`、`run-unity-tests.sh`、`test-harness-smoke.sh`：PASS。
- CI harness smoke：PASS；覆盖缺失category、verify-config未知参数和help合同。
- 正式工作簿和模板镜像字节未变；Schema、样例、Packages、ProjectSettings、场景、Prefab和Registry无差异。
- Runtime JSON相对T230无diff；所有新增Unity资产均存在`.meta`。
- 默认入口为只读；更新受管生成物必须显式传入`--update`。
