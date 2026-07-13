# T120 Change Whitelist

- Git基线：`3cc548cfb287bb3dd2922824642ca1cb2a5445b4`（`main`，任务开始时除本证据目录外工作树干净）。
- 需要保护的用户已有改动：无；若出现白名单外改动，先判定来源并保留，不覆盖。
- 任务目标：基于 T110 固定的 embedded WXSDK，在 Unity `6000.5.1f1` 完成 G2 微信转换，并对本机可用的 G3 DevTools 与至少一台 G4 真机分别保存可审查结论。
- 明确不做：不把 G2 成功外推为 G3/G4；不写 AppID/AppSecret；不接业务平台 API；不迁移 Unity 或升级 SDK；不开始 T130。

## 预计改动白名单

- `Assets/_Game/Scripts/Editor/OneStrokeDemon.Editor.asmdef`、`Assets/_Game/Scripts/Editor/Build/**`、`Tools/CI/build-wechat.sh`：增加可重复、非交互的微信 Spike 构建入口与命令封装；只允许项目 Editor 构建程序集增加 `WxEditor` 引用。
- `Assets/_Game/Tests/EditMode/T120/**`：增加构建参数、输出路径和平台门口径的专项测试及 Unity `.meta`；测试程序集不直接引用 SDK 预编译 DLL。
- `ProjectSettings/ProjectSettings.asset`：仅允许 Unity/SDK 为微信构建目标写入且经差异审查确认必要的设置；禁止手工编辑 Unity YAML。
- `docs/PLATFORM_WECHAT.md`、`docs/WECHAT_SDK_COMPATIBILITY.md`、`docs/UPSTREAM.md`、`docs/BUGS.md`、`docs/DECISIONS.md`：同步 G2/G3/G4 实测结果、上游行为、阻碍与必要决策。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`、`PACKAGE_VALIDATION.md`：同步当前任务状态、平台门和下一任务。
- `artifacts/evals/T120/**`：Git 基线、转换原始日志与摘要、产物清单/hash/体积、测试 XML、工具/设备探测、截图索引、白名单和最终验证。
- Unity 导入若自动生成其他版本控制文件：仅在确认是本任务必要结果后先补充白名单；转换产物、缓存和含敏感信息的本机配置不得提交。

## 禁止改动

- 不修改 Gameplay、场景、Prefab、配置 Excel/JSON、正式资源、T100/T110 证据或 embedded SDK 源码。
- 不提交 `Builds/`、`Library/`、微信项目私有配置、登录凭据、AppID/AppSecret或开发者工具缓存。
- 缺微信开发者工具、登录态或真机时，对应层级只能记录 `BLOCKED/NOT RUN`，不得伪造截图或 PASS。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过；原始Unity日志按原样保留，非原始日志的未跟踪文件另行通过空白符检查。
- [x] 用户明确延期平台门后，仅暂存本白名单文件形成T120阻塞检查点；提交前审查`git diff --cached`，不混入T200实现。
