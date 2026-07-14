# T650 Player Path

- 执行：Unity `6000.5.1f1` PlayMode，从Bootstrap/MainMenu读取正式配置并创建实际BattleHUD、`TutorialOverlayView`与`TutorialDirector`。
- 首步：遮罩显示配置中文“划过妖怪即可攻击”，手势为`Any`，高亮解析到`BattleArea`，跳过按钮显示配置文案“继续战斗”；所有活动TMP文本无overflow/truncate。
- 首局：点击跳过后只产生`TutorialSkipped`/`TutorialCompleted`，`StepCompleted`计数不增加；战斗不会立即结束。时间线实际出生15个敌人，全部击败后才放行最终`PlayerConfirmed`门并结算`Victory`。
- 重开：新会话从进度v2读到已完成教程ID并自动跳过教程展示；回看可显示最近配置提示，关闭回看前后序列均为Completed。新会话仍实际出生并击败15个敌人后才结算第二次`Victory`。
- 持久化：两局结束后`completedTutorialIds`包含当前配置教程ID，`IProgressSaveStore.WriteCount = 1`。
- 图形证据：`tutorial-overlay-1920x1080.png`，1920×1080 RGB/Metal，SHA-256 `86587e44620e6064e197cb4bb7b3fd1ad159b33a0af6b8d3a4531c6f8603599f`。
- 结论：PASS。
