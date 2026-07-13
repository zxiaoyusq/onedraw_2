# T510 Player Path

- 环境：Unity `6000.5.1f1` / WebGL目标；两条PlayMode均从真实Bootstrap加载受管配置并到达MainMenu，日志确认schema 4、content 0.5.1-sample、hash `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`、Registry 76键和Mouse/Touch统一输入就绪。
- 生命周期路径：配置2秒Countdown完成后进入Playing，再进入UltimateDrawing并推进1秒；FocusLost立即请求一次活动笔迹取消并进入Paused，随后ApplicationPaused叠加。暂停推进30秒时流程delta、战斗delta和关卡delta均为0；先恢复Focus仍保持Paused，最后解除ApplicationPaused才回到Playing，终极绘制不恢复。
- 终极路径：满100能量进入UltimateDrawing；只把配置2.5秒输入窗走到包含边界时仍保持等待、能量100、效果0。再推进0.000001秒只产生InputWindowExpired取消，成功事件0、能量仍100。第二次先通过单调gestureEventId门，再收到T410有效Circle结果后恰好一次成功，能量100→0，并执行TimeScale与ClearProjectiles世界步骤；配置0.25倍/0.8秒让下一0.8秒未缩放推进只产生0.2秒战斗delta。EditMode另验证已消费事件ID不能跨绘制重放。
- 事件与结算：EditMode同时提交玩家死亡、关卡完成和时限到达，稳定得到Defeat；随后重复提交和推进100秒都不再发布第二次Settled。仅LevelCompleted事实得到Victory。
- 边界：本任务验证流程、时间和事件门，不包含T520具体教学编排、T540完整Boss关、T550结算存档、T600 HUD或平台构建。
