# T340 Runtime Smoke

- Unity：6000.5.1f1，实例`onedraw_2@272e911286835fad`，Bootstrap进入MainMenu。
- 玩家路径：真实Input System Mouse按下→移动→抬起，经`InputSystemPointerAdapter`、`StrokeInputCollector`、`StrokeGeometry.Process`、`StrokeTrailPath.FromGeometry`进入`StrokeTrailPool.Show`。
- 共享点集断言：`StrokeTrailView.SourcePoints`与`StrokeGeometryData.Points`为同一对象引用，LineRenderer位置数等于处理点数且逐点坐标一致。
- 配置断言：真实配置加载schema 1 / content 0.1.1-sample / 28表645条；刀宽18、符宽28参考像素、寿命0.3秒、池12、活动上限3、最大80点、`VFX`排序层非Default且order 20。
- 连续划动：四笔快速显示后只保留strokeId 2/3/4，strokeId 1被最旧复用；三个活动Renderer都引用同一Material。
- 淡出与回收：半寿命alpha为0.5（允许LineRenderer 8-bit颜色量化），完整寿命后状态全部清空，同一View可切换为符宽重新使用。
- 热路径：16次预热后128次显示/回收托管分配增量0 B。
- 专项结果：PlayMode 5/5 PASS，job `f8a4ab189acb4eb8b13e3d8f75c73a95`。
