# T699D closeout

## 起始状态与保护项

- 起始分支：`main`，相对`origin/main`领先1个T699C提交。
- 用户已有改动：`Assets/_Game/Art/Characters/Animated/Moyan/PlayerMoyan.controller`、`Packages/manifest.json`、`Packages/packages-lock.json`；`Assets/_Game/Art/Enemies/11.anim`及其`.meta`、`Design/Config/~$GameConfig.xlsx`为删除状态。
- 上述路径未由本任务修改、恢复或暂存。

## 改动白名单

- 产品代码：`StrokeInputCollector.cs`、`StrokeTrailView.cs`、`StrokeTrailPool.cs`、`BattleCompositionRoot.cs`。
- 回归：T310采样器、T340轨迹池、T660生产入口、T600/T610/T650配置日志快照、T699A屏幕边界测试。
- 文档与证据：`docs/TASKS.md`、`docs/PROGRESS.md`、`docs/DECISIONS.md`、`project-index.yaml`及本文件。

## 根因与修复

- Charged配置和抬手后分类已经存在，但静止按住期间没有时钟事件；轨迹预览只有一个点时隐藏，因此玩家看不到蓄力过程。
- 采集器新增由调用方单调时钟推进的起笔停留事件，仅在首个有效移动点之前发布；抖动、移动、抬手、取消和释放边界均有明确语义。
- 生产入口用`chargeHoldSec=0.4s`归一化进度，以Charged规则`hitRadiusRefPx`和当前架势画笔样式绘制青白环；环满只表示就绪，仍须继续划动至少配置的100参考像素并抬手才会识别Charged。
- 蓄力环复用Prefab已有Renderer，不新增资源、对象、Inspector或玩法数值，也不拥有手势、命中或伤害真相。

## 验证

- StrokeSampling EditMode：11/11通过。
- StrokeTrail PlayMode：6/6通过。
- T660生产入口PlayMode：5/5通过。
- T699A投射物PlayMode：2/2通过。
- 隔离工程全量EditMode：216/216通过。
- 隔离工程全量PlayMode：61/61通过。
- 主工程已有Unity实例占用，未关闭或干扰；测试副本包含当时完整Assets、Packages、ProjectSettings及仓库合同文件。
- 全量EditMode首轮213/216的3项失败仅因快速隔离副本缺少外层合同文件，补齐后216/216。全量PlayMode首轮57/61中3项为旧`0.6.8`日志快照，1项为640宽窗口下既有测试端点越界；同步当前`0.6.9`快照并把测试笔迹夹在屏幕内后61/61。

## 范围结论

- 未修改工作簿、配置数据、Schema、DTO、Animator、动画资源、Scene、Prefab、Registry、ProjectSettings或Packages。
- T700保持独立READY，本任务未提前实施其回归矩阵。
