# T100 Change Whitelist

- Git基线：`main` / `812f411f T040: establish build and verification workflow`，开始时工作树干净；详见`baseline-commit.txt`与`baseline-status.txt`。
- 需要保护的用户已有改动：无。
- 任务目标：完成标准Unity WebGL构建、本地HTTP运行，以及输入/音频/中文interop/PlayerPrefs存储冒烟与体积证据。
- 明确不做：不接微信SDK或转换，不运行DevTools/真机，不实现玩法，不把标准Web成功外推为微信成功，不宣称TMP中文字体通过。

## 预计改动白名单

- `Assets/_Game/Scripts/Platform/T100/**`与`OneStrokeDemon.Platform.asmdef`：新增仅`T100_WEB_SMOKE`构建定义启用的Web技术探针及Input System外部引用。
- `Assets/Plugins/WebGL/**`：新增测试构建使用的JS bridge，由Unity Editor导入并生成meta。
- `Assets/_Game/Scripts/Editor/Build/WebBuildEntry.cs`、`Tools/CI/build-web.sh`：增加显式`--smoke`构建开关和额外编译定义，默认标准入口行为保持不变。
- `Tools/CI/serve-web-build.py`：为实际生成的Brotli WebGL产物提供正确MIME与`Content-Encoding: br`的本地HTTP测试入口。
- `Assets/_Game/Tests/EditMode/T100/**`：新增`WebBuildSmoke`合同测试，验证探针只在显式Smoke构建启用。
- `Assets/_Game/Tests/EditMode/T030/AssemblyDependencyTests.cs`：把依赖图断言限定为`OneStrokeDemon.*`项目程序集，允许后续任务增加经专项验证的Unity包引用；循环检查逻辑不变。
- `Assets/Editor/T100ProjectStateCleanup.cs`：仅作为临时Unity Editor序列化清理入口，恢复Web构建副作用后删除，不进入提交。
- `Assets/DefaultVolumeProfile.asset`：允许Unity 6000.5/URP首次Web序列化补充`filter`字段；其他URP/ProjectSettings自动改写必须由Editor恢复且不得提交。
- `Builds/WebGL/**`：本地标准Web构建产物，仅用于HTTP/浏览器验证，已被Git忽略，不提交。
- `artifacts/evals/T100/**`：保存测试XML、过滤日志、构建清单/体积、浏览器截图和分层验证结论。
- `docs/WEB_BUILD_BASELINE.md`、`docs/PLATFORM_WECHAT.md`、`docs/BUGS.md`：记录G1标准Web事实、未覆盖边界、两类Web warning与MCP重连工具问题。
- `PACKAGE_VALIDATION.md`：同步已完成Harness/G1和当前READY任务，移除T030旧状态。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T100状态和构建证据索引。

## 禁止改动

- 不修改场景/Prefab/玩法配置/微信SDK；不提交`Builds/WebGL`或包含License/机器凭据的原始Unity日志；不开始T110。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
