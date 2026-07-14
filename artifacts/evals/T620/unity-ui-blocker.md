# T620 Unity UI Blocker（已解除）

- 日期：2026-07-14
- Unity：6000.5.1f1，已有进程打开当前工程。
- 批处理结果：同一工程已在Editor打开，T620 EditMode未生成结果XML。
- UI接管结果：按`computer-use`流程尝试连接已有Unity实例时，Mac处于锁屏状态且自动解锁失败。
- Unity MCP结果：服务端可访问，但`mcpforunity://instances`返回`instance_count=0`，Editor状态返回`no_unity_session`；锁屏中的Editor未接入可调用会话。
- 解除记录：Mac于2026-07-14 09:30前解锁，主Editor完成Refresh并生成当前四个程序集；MCP仍未恢复在线实例。
- 后续处理：同步当前工作树到既有隔离Unity工程，使用Unity 6000.5.1f1批处理完成T620专项、ConfigPipeline、全量回归及Metal 1920×1080感知截图。
- 最终影响：无；Roslyn静态编译未被当作Unity测试PASS，最终结论以NUnit XML和Metal截图为准。
