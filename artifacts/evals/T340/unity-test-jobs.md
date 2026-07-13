# T340 Unity Test Evidence

- 脚本Refresh、Unity 6000.5.1f1编译：Console Error 0 / Warning 0。
- 最终StrokeTrail PlayMode：5/5 PASS，job `f8a4ab189acb4eb8b13e3d8f75c73a95`；包含每项详细输出和真实Bootstrap日志。
- 状态同步前全量EditMode：72/72 PASS，job `fb6b89fbece84ac4a68eac954ae7b2bc`。
- 状态同步前全量PlayMode：20/20 PASS，job `21ecd2d56abc4b5ca30e5aa339156496`。
- 状态同步后最终全量EditMode：72/72 PASS，job `75fd8e4f09474f72952af754983ab879`。
- 状态同步后最终全量PlayMode：20/20 PASS，job `f7b4dd1aa2ec4c67a3a8944751abbe9f`。

专项覆盖真实配置刀/符切换、真实VFX Sorting Layer、同一几何点引用和逐点坐标、线性淡出、到期回收、完整重置、最旧复用、三条活动上限、唯一共享材质、预热后0 B托管分配，以及真实Mouse玩家路径。
