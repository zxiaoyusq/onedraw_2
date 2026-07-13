# T340 Trail Contract

- 数据所有权：`StrokeTrailPath.FromGeometry`保留`StrokeGeometryData.Points`的同一`IReadOnlyList<Vector2>`引用；视觉不复制、不重新简化，也不生成碰撞数据。
- 配置映射：刀/符宽度分别来自`Stances.strokeWidthRefPx`（18/28参考像素）；寿命0.3秒、预热12、`VFX`排序层和order 20来自`vfx_slash`；最大80点来自全部`StrokeRules.maxPointCount`的最大值。
- 技术上限：遵循`TECH_SPEC`最多3条活动残留和96点硬上限；当前配置最大80点。池容量与VFX预热一致为12，第四条活动轨迹回收激活序列最旧者。
- 渲染边界：`StrokeTrailView`只写LineRenderer位置、宽度、颜色和排序，不包含GestureClassifier、Physics2D、Collider、IHittable或命中结果。
- 材质边界：初始化时注入单一Unity资源引用并只使用`sharedMaterial`；每次显示、每段和每帧都不创建Material。
- 回收合同：禁用Renderer并清空positionCount、宽度、颜色、排序、strokeId、架势、源点引用、寿命状态和子Transform；共享材质保留供复用。
- 分配合同：对象、组件和固定数组只在`Initialize`预热；预热后128次`Show`+到期回收的当前线程托管分配增量为0。
- 工程资源：通过Unity Editor SerializedObject API新增配置已引用的`VFX` Sorting Layer（ID 1424202891）；Unity同时把TagManager迁移到当前serializedVersion 3并补当前版本的Rendering Layers字段，未手工编辑Unity YAML。
