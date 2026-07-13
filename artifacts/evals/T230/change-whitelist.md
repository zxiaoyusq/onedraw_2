# T230 Change Whitelist

- Git基线：`f6e41bdaecdd26fe35e38bbca91631674676d14d`（`main`；任务开始工作树干净）。
- 需要保护的用户已有改动：无；后续若出现白名单外改动，保留并先判定来源，不覆盖。
- 任务目标：实现Unity Runtime的完整配置DTO、一次性JSON解析、schema/content/hash兼容检查、显式只读ID/分组索引、启动摘要和Bootstrap阻断；业务代码不遍历可变原始数组，不在热路径反序列化。
- 明确不做：不实现T240 AssetRegistry；不实现T250一键生成/hash文件/ConfigIds或CI漂移检查；不实现玩法、平台抽象、微信DevTools或打包。

## 预计改动白名单

- `Assets/_Game/Scripts/Config/**`：新增纯C# DTO、加载服务、只读查询接口、兼容/结构校验、异常与启动摘要；不得包含玩法平衡值、反射注册或文件系统读取。
- `Assets/_Game/Scripts/Bootstrap/BootstrapController.cs`：只允许注入生成JSON的`TextAsset`资源引用、初始化配置服务、成功后进入主菜单、失败时阻断并记录上下文。
- `Assets/_Game/Config/Generated/gameplay_config.json{,.meta}`：只允许由T210/T220已验证的正式工作簿确定性导出；JSON不得手改。T250后续负责一键生成、hash旁车和漂移检查。
- `Assets/_Game/Scenes/Bootstrap.unity`：仅通过Unity Editor保存`gameplayConfig` TextAsset场景引用；禁止手工编辑Unity YAML或顺带改场景内容。
- `Assets/_Game/Tests/EditMode/OneStrokeDemon.Tests.EditMode.asmdef`、`Assets/_Game/Tests/EditMode/T230/**`：新增Config程序集引用、RuntimeConfigLoadTests、InvalidConfigTests及测试fixture；不复制第二份完整平衡数据。
- `Assets/_Game/Tests/PlayMode/OneStrokeDemon.Tests.PlayMode.asmdef`、`Assets/_Game/Tests/PlayMode/T230/**`：新增Config程序集引用和Bootstrap真实加载/阻断冒烟。
- `Packages/manifest.json`、`Packages/packages-lock.json`：把当前已解析的Unity官方`com.unity.nuget.newtonsoft-json 3.2.2`从MCP传递依赖提升为Runtime直接固定依赖；只允许该单项深度/声明变化。
- `docs/PACKAGE_BASELINE.md`：记录Runtime JSON解析器版本、上游Newtonsoft 13.0.2及Unity Companion/MIT许可证边界。
- `docs/CONFIG_PIPELINE.md`、`docs/DECISIONS.md`、`PACKAGE_VALIDATION.md`：同步Runtime加载合同、初始快照边界和验证结论。
- `docs/TASKS.md`、`docs/PROGRESS.md`、`project-index.yaml`：同步T230状态、证据与下一任务。
- `artifacts/evals/T230/**`：基线、白名单、Unity测试XML/日志、CLI/hash、Console/玩家路径、保护路径和最终验证证据。
- `artifacts/tmp/T230/**`：忽略的临时导出/测试/Unity日志，不提交。

## 禁止改动

- 不修改正式xlsx、模板镜像、Schema、审查样例、Tools/ConfigExporter实现、ProjectSettings、微信SDK、MainMenu/Battle场景、Prefab或美术资源；Packages只允许上述Newtonsoft直接依赖声明。
- 不提交Unity `Library/Logs/Temp`、NuGet缓存、临时JSON或测试派生坏配置；不在Inspector/ScriptableObject/C#复制HP、伤害、阈值、敌人、技能、关卡或文案。
- `.meta`和`Bootstrap.unity`只能由Unity Editor产生/保存；场景序列化字段只保存JSON资源引用，不保存配置内容或数值。

## 收尾审查

- [x] `git status --short`中的每一项都属于白名单。
- [x] `git diff --check`通过。
- [x] 仅暂存白名单文件，并审查`git diff --cached`。
