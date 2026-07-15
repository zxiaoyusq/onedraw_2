# T660 触控板与Console评审诊断（2026-07-15）

## 基线与保护

- 基线提交：`9d2f35e06ee56b5835e76ece774594d3873f0f9b`。
- 初始工作树仅有受保护的未跟踪`artifacts/evals/T700/**`，本轮未编辑或暂存。
- 允许范围见`change-whitelist.md`的2026-07-15增补。

## 现场证据

- 用户确认主Unity已能进入生产游戏，但Mac触控板拖动没有可见划线反馈。
- 主Editor初始化日志：`POINTER_INPUT_READY source=InputSystem modes=Mouse,Touch ...`；Mac触控板在Editor内走Mouse路径，必须按下并保持后拖动，只移动手指不产生`leftButton.wasPressedThisFrame`。
- 生产`ProductionBattleSession`已收集并结算笔迹，但未装配T340的`StrokeTrailPool`，因此即使命中也没有线条反馈。
- 切换架势捕获：`ARREG009 [source=AssetRegistry:asset_registry, context=sfx_switch]: Unknown asset key 'sfx_switch'`，堆栈进入`ProductionBattleWorld.PlayAudio`。`SkillEffects.audioKey`是协议ID，实际资源键由`AudioCues`映射为`audio_sfx_switch`，不能直接访问Registry。
- 另有`currentFileSystemTime.ticks != 0 using check file Temp/FSTimeGet-*` Assert，无产品脚本堆栈，保留为Unity 6.5资源管线临时文件噪声。

## 红→绿修复

- 红测`test-results/playmode-review-red.xml`：断言生产笔迹必须渲染，修复前失败。
- 红测`test-results/playmode-review-audio-red.xml`：切换架势触发上述`ARREG009`，修复前失败。
- `BattleCompositionRoot`现在以配置的`vfx_slash`池设置和当前架势样式显示处理后几何，并在会话释放时清池和销毁运行时材质。
- `ProductionBattleWorld.PlayAudio`现在执行`AudioCues.audioKey -> assetKey -> AssetRegistry`映射，不新增旁路表或吞掉未知配置。
- 最终专项EditMode 2/2、PlayMode 4/4；全量EditMode 197/197、PlayMode 50/50。最终XML/日志位于同目录的`test-results/`与`logs/`。

## 人工门

- 首轮主Unity MCP已连接并能检查MainMenu、Battle和Console；当时Computer Use可读到Battle窗口，但坐标点击/拖拽返回`noWindowsAvailable`，因此没有用自动化InputSystem Mouse测试冒充实体触控板复测。
- 用户随后亲自确认触控板已经能够画线，并继续报告中央白板和闪线观感；该后续视觉问题的诊断与修复见下节。T660当前等待的是修复后视觉确认，不再等待输入是否可用的确认。

## 轨迹视觉复评

- 用户随后确认触控板已经能够画线，但报告游戏中间持续出现白板，轨迹只能看到粗白光一闪，无法辨认绘制路径。
- 无轨迹注入的无遮挡现场仍出现相同白板；Renderer枚举锁定唯一8×4中央MeshRenderer为场景开发对象`BattleGraybox`。运行时将它临时设为inactive后白板立即消失，证明白板与轨迹是两个独立问题。随后通过Unity Editor只把该对象设为inactive并保存Battle场景，YAML diff仅`m_IsActive: 1 -> 0`。
- 轨迹方面，主Editor测得生产参考根`lossyScale=(0.009835,0.009249,1)`；旧LineRenderer在该非等比缩放层级内渲染，且只在`StrokeCompleted`后显示配置的0.3秒残留，没有拖动中预览。
- 红测`playmode-review-visual-red.xml`在释放前查不到生产轨迹，以`3 / 4 / 1`按预期失败。修复后`StrokeInputCollector`在首点及每个满足采样阈值的点发布预览事件；池复用同一View实时追加点，完成时切换到处理后几何和既有配置淡出。
- 白板回归`playmode-review-whiteboard-red.xml`在旧场景以“开发灰盒仍active”按预期失败（3/4/1），Editor保存场景后`playmode-review-whiteboard-green.xml`通过（4/4）。LineRenderer GameObject现挂在生产会话单位变换根，`referenceRoot`只作为坐标与参考像素宽度的转换空间；主Editor冻结复查返回`scale=(1,1,1)`、`referencePixelWorldScale=0.009835`、配置18参考像素对应`width=0.177025`、4点、`useWorldSpace=true`、`preview=true`，现场截图无白板且连续折线可见。
- 最终StrokeSampling EditMode 10/10、StrokeTrail PlayMode 5/5、T660 PlayMode 4/4、全量EditMode 198/198、全量PlayMode 50/50；配置只读漂移门和.NET 58/58通过。T660继续保持REVIEW，仅等待用户确认修复后的实际拖动观感。
