# T310 Unity Test Evidence

- StrokeSampling EditMode：9/9 PASS，job `df0d901a8f0d42f699e6acdd783f5d7a`。
- StrokeSampling PlayMode：1/1 PASS，job `596f8a16d9d14d62886ef2d4bc3b4540`。
- 最终全量EditMode（文档与任务状态同步后）：46/46 PASS，job `4f8f40fa639d4b0c879cf9095d89c22d`。
- 全量PlayMode：13/13 PASS，job `5343260955ba4f5f9935aaa3a7ebf952`。

专项覆盖抖动过滤、阈值等号、折线路径精确截断、点数上限、快照不可变与采样器复用、配置映射、取消/完成区分、超限单次发布，以及100次合法收点的0字节托管分配增量。PlayMode通过真实Input System Mouse设备和`InputSystemPointerAdapter`形成一笔并仅发布一个结果。
