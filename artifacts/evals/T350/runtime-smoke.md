# T350 Runtime Smoke

- Unity：6000.5.1f1，实例`onedraw_2@272e911286835fad`，Bootstrap→MainMenu。
- 玩家输入：Input System真实Mouse从屏幕15%横划到85%，经T300统一输入、T310采样、T320几何和T330分类得到Horizontal。
- 共享路径：T340轨迹View的`SourcePoints`与T320几何`Points`为同一引用，LineRenderer点数一致且位于Camera视锥；T350解析同一几何。
- 多目标：路径依次输出target 101和202，归一化路径参数严格递增；偏离路径的303不输出。
- 弱点/去重：101同时拥有主体BoxCollider2D和弱点Trigger CircleCollider2D，只输出一条记录且`IsWeakpoint=true`；202仅主体且为false。
- 追溯：输出strokeId等于几何strokeId，Gesture为同一分类结果，Timestamp等于几何结束时间；抬笔后指针不活动。
- 热路径：真实Collider2D + CircleCast + Resolver预热后连续128次，当前线程托管分配增量0 B。
