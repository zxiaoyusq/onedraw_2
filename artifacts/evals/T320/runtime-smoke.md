# T320 Runtime Smoke

- Unity：6000.5.1f1，实例`onedraw_2@272e911286835fad`。
- 自动玩家路径：Input System Mouse按下→两次转向移动→抬起，经T300统一输入与T310采样进入`StrokeGeometry.Process`，得到1个保留`strokeId=1`、正长度、正包围盒和正总曲率的不可变处理结果；PlayMode 1/1 PASS。
- Bootstrap真实运行路径：Bootstrap加载28表645条配置和76键Registry，从配置初始化1920×1080 Mouse/Touch统一输入后进入MainMenu。
- 关键日志：`CONFIG_RUNTIME_READY`、`ASSET_REGISTRY_READY`、`POINTER_INPUT_READY source=InputSystem modes=Mouse,Touch reference=1920x1080 safeArea=dynamic uiBeginBlock=true maxActivePointers=1`。
- 清空测试期预期负例日志后执行上述路径，Console新增Error 0、Warning 0。
