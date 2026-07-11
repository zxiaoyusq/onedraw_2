# PROGRESS

- 日期：2026-07-11
- 当前成熟度：T020 Unity渲染、输入、UI测试与包版本基线已固定
- 当前任务：T030
- 状态：READY
- Unity精确版本：6000.5.1f1（已由ProjectVersion.txt与本机安装核验）
- 微信SDK来源或版本：PENDING_VERIFICATION
- Active Scene：Assets/Scenes/SampleScene.unity
- 配置版本：schema 1 / content 0.1.0-sample

## 已完成

- T000：玩法、MVP范围、技术边界、配置唯一真相源和完成定义已统一。
- T010：建立根 `.gitignore`，现有Unity工程与开发合同已纳入唯一Git根。
- T020：Graphics与Low/High质量档统一使用URP 2D；Unity MCP依赖固定到commit。
- T020 EditMode 3/3、PlayMode 1/1通过；Mouse与Touchscreen均可驱动同一个Pointer Action。
- TMP与Unity Test Framework程序集已加载，测试程序集可发现并独立运行。
- Unity 6000.5.1f1成功加载SampleScene并进入Play Mode 2秒；探针记录Console Error/Exception/Assert为0。
- 已确认SampleScene包含Main Camera与Global Light 2D，且已在EditorBuildSettings启用。
- 接受现有Unity `6000.5.1f1` 与“仓库根目录即Unity工程根”的基线决策。
- 已确认当前仓库内只有一个Git根；Android、WebGL、macOS和Windows构建模块已安装。
- 玩法、MVP、技术、配置、平台、测试和原子任务计划已建立。
- 已生成Excel配置模板、示例JSON和schema。
- 已纳入工程复盘中的唯一真相源、平台前置Spike、配置闭环、证据分层和一任务一提交方法。

## 当前风险

1. 当前官方微信Unity转换SDK分发渠道和Unity 6000.5.1f1兼容性尚未验证。
2. Unity MCP服务可访问，但实例桥接仍未自动重连；T030需要场景操作，开始时必须优先恢复并核验active instance。
3. PSD主角和怪物大多为单张Sprite；中文字体包体、Web内存和真机触摸延迟仍需后续验证。

## 下一步

只执行T030：建立目标目录、程序集边界和Bootstrap/MainMenu/Battle三场景骨架，并恢复Unity MCP场景操作链路。不要开始T040或业务玩法。
