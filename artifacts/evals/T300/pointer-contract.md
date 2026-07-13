# T300 Pointer Contract

- 唯一入口：`IPointerInput.PointerChanged`，Mouse和Touch均发布同一不可变`PointerInputEvent`。
- 坐标：事件同时保留原始屏幕坐标和Safe Area内的参考像素坐标；参考宽高来自配置Global，运行时代码没有1920/1080兜底，也不读取`Screen.dpi`。
- 起笔：必须在当前动态Safe Area内且当前uGUI Raycast无命中；被UI阻挡的按压即使移出UI也不会补发Began。
- 活动：只锁定首个Mouse或物理TouchControl；第二根手指/另一设备不会接管或延长。合法笔迹移出Safe Area时参考坐标夹紧到边界，确保End仍可发布。
- 终止：正常抬起为Ended；失焦、应用暂停、禁用、系统取消、设备断开与Runtime重置为带明确原因的Canceled；同一活动指针至多一个终止事件。
- 生命周期：Bootstrap在配置与AssetRegistry成功后读取`reference_width/reference_height`并初始化`PointerInputRuntime`；初始化失败阻断MainMenu，没有Inspector数值副本或场景组件副本。
- 明确边界：没有采样最小距离、点数上限、长度裁剪、识别、轨迹或命中逻辑，这些从T310开始。
