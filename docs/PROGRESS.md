# PROGRESS

- 日期：2026-07-11
- 当前成熟度：T000合同基线已完成；现有Unity工程待T010纳管与冒烟
- 当前任务：T010
- 状态：READY
- Unity精确版本：6000.5.1f1（已由ProjectVersion.txt与本机安装核验）
- 微信SDK来源或版本：PENDING_VERIFICATION
- Active Scene：未通过MCP核验
- 配置版本：schema 1 / content 0.1.0-sample

## 已完成

- T000：玩法、MVP范围、技术边界、配置唯一真相源和完成定义已统一。
- 接受现有Unity `6000.5.1f1` 与“仓库根目录即Unity工程根”的基线决策。
- 已确认当前仓库内只有一个Git根；Android、WebGL、macOS和Windows构建模块已安装。
- 玩法、MVP、技术、配置、平台、测试和原子任务计划已建立。
- 已生成Excel配置模板、示例JSON和schema。
- 已纳入工程复盘中的唯一真相源、平台前置Spike、配置闭环、证据分层和一任务一提交方法。

## 当前风险

1. 当前官方微信Unity转换SDK分发渠道和Unity 6000.5.1f1兼容性尚未验证。
2. 根 `.gitignore` 尚未建立，现有Unity生成目录仍是未跟踪文件；T010必须先纳管边界再提交工程基线。
3. Unity Editor与MCP服务进程虽已启动，但当前会话未获得可调用的Unity MCP工具，尚未核验active instance、Console和Play Mode。
4. `com.coplaydev.unity-mcp` 当前仍引用浮动 `#main`，后续基线任务应固定可复现版本或commit。
5. PSD主角和怪物大多为单张Sprite；中文字体包体、Web内存和真机触摸延迟仍需后续验证。

## 下一步

只执行T010：保护现有Unity工程，建立根 `.gitignore`，核验6000.5.1f1工程可打开、Console无Error并完成空场景Play Mode冒烟。不要开始T020或业务代码。
