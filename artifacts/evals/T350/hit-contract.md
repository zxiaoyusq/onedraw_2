# T350 Hit Contract

- 输入点：命中解析器直接遍历`StrokeGeometryData.Points`，与`StrokeTrailPath.FromGeometry`持有同一引用；不会复制、重采样或按视觉状态改变命中。
- 查询形状：每两个相邻非零长度点调用一次固定缓冲`Physics2D.CircleCast`；扫圆等价于半径为所选`StrokeRules.hitRadiusRefPx`的分段胶囊。
- 容量：配置活动敌人18加活动投射物40得到58个唯一目标；技术上每目标允许主体/弱点两个Hitbox并加一个饱和哨兵，查询缓存固定117槽。
- 目标合同：`IHittable`只暴露非零稳定targetId和当前是否接受笔迹命中；`IStrokeHitbox`把Collider映射到逻辑目标并标记主体/弱点，不包含伤害数值。
- 结果合同：同一targetId跨多个Collider和线段只生成一条`HitRecord`；路径距离取首次接触，弱点标记对该目标做逻辑或，最后按距离再按targetId稳定排序。
- 追溯：记录strokeId、目标引用/ID、弱点、归一化路径参数、参考像素距离、完整笔势与结束时间；本层只解析，不修改目标或执行T360伤害。
