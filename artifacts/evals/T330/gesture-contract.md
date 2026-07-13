# T330 Gesture Classification Contract

- 输入：一份不可变`StrokeGeometryData`；分类、后续视觉和命中不各自生成不同几何真相。
- 规则来源：`IConfigProvider.GetStrokeRules()`返回稳定只读全表，`GestureRuleSetFactory`显式解析七种配置枚举；未知枚举、空规则、重复规则ID失败。
- 配置阈值：`minLengthRefPx`、`directionToleranceDeg`、`closeDistanceRefPx`、`minAreaRefPx2`、`minArcCurvature`、`chargeHoldSec`。Runtime没有复制这些平衡值或硬编码规则ID。
- 指标：处理点集长度、平均速度、无向首尾角`[0,180)`、归一化总曲率、闭合比/距离、绝对面积、起笔至首个有效采样的停留时长。
- 匹配：方向类使用无向角偏差；Arc使用曲率；Circle同时使用闭合、面积和曲率；Charged使用真实首段停留；Any只验证最小长度并兜底。
- 多匹配裁决：Circle > Charged > Arc > Horizontal/Vertical/Diagonal > Any；同优先级先取较高置信度，完全相同时取Ordinal较小规则ID。
- 置信度：每项配置约束在阈值处为0.5，向理想侧线性增加并夹紧至1；任一约束不满足则该规则不匹配。最终结果始终在0～1，无匹配为0。
- 蓄力语义：微抖若未通过`minPointDistanceRefPx`不会结束停留；整笔画得慢但首个有效移动早不会被误判为Charged。
