# T320 Unity Test Evidence

- StrokeGeometry EditMode：12/12 PASS，job `3b5804c64c8e4375be2026016cb7f27d`。
- StrokeGeometry PlayMode：1/1 PASS，job `609a6d996e9e4e2f93eff435f0fba9a3`。
- 最终全量EditMode（文档与任务状态同步后）：58/58 PASS，job `fbdffb0fb9b04bcda5735b6a763d10f3`。
- 全量PlayMode：14/14 PASS，job `53b956fa3adc4f579630bd414d5c3cbd`。

专项覆盖RDP端点与容差等号、跨拐角弧长重采样、处理点数上限、矩形长度/包围盒/面积/闭合、左右转与S形曲率、四分之一弧尺度不变性、圆回放确定性、空/重复/单点退化输入、不可变结果、源元数据和真实配置映射。PlayMode通过真实Input System Mouse设备完成输入→采样→几何处理链路并只得到一份结果。
