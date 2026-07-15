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

- 主Unity MCP已连接并能检查MainMenu、Battle和Console；Computer Use可读到Battle窗口，但坐标点击/拖拽返回`noWindowsAvailable`。
- 因此本轮不把自动化InputSystem Mouse测试冒充用户的实体触控板复测。T660保持REVIEW，等待用户在非UI区域“按住触控板并拖过敌人，松开”确认短暂轨迹、扣血和评分变化。
