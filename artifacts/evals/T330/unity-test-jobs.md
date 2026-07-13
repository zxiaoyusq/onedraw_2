# T330 Unity Test Evidence

- GestureClassifier EditMode：14/14 PASS，job `6c5af262f9ef4b38b534f951dac93d02`。
- GestureClassifier PlayMode：1/1 PASS，job `efdea89322cf4906964b4ac87b06d7b0`。
- 文档与任务状态同步后最终全量EditMode：72/72 PASS，job `4071bc06c6ac4f6b9ebaee719da859c1`。
- 最终全量PlayMode：15/15 PASS，job `5b4791f1d84e475587a5505a2a666179`。
- 隔离详细玩家路径：1/1 PASS，job `5c87b853b3c640c78f3959cac882374a`；输出包含`CONFIG_RUNTIME_READY`、`ASSET_REGISTRY_READY`和`POINTER_INPUT_READY`。

EditMode覆盖横/竖/双向斜线、弧、圆、蓄力、Any、无匹配、平均速度、23度近水平线、小面积闭环、直线非弧、只读配置映射、未知类型、重复ID、置信度范围及完全确定的回放摘要。PlayMode通过真实Input System Mouse完成Bootstrap→统一输入→采样→几何→配置分类。

首次尝试批处理时检测到同项目已有前台Editor，Unity在测试前按设计拒绝第二实例；原始`editmode-gesture-unity.log`保留。随后全部测试在该6000.5.1f1实例内通过MCP Test Runner执行，不把这次Harness拒绝计为测试失败。
