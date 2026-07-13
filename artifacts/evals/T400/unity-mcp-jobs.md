# T400 Unity MCP Jobs

- Unity实例：`onedraw_2@272e911286835fad`
- Unity版本：`6000.5.1f1`
- 脚本完整Refresh/编译：PASS；新增脚本与测试目录由Unity导入并生成`.meta`。
- 专项EditMode `PlayerCombat`：job `ce8620dd05d74579b31e8666d42ce0aa`，8/8 PASS。
- 专项PlayMode `StanceSwitch`：job `16c685c7bd274716aad8280822d814a1`，2/2 PASS。
- 实现完成后全量EditMode：job `fed7d77049254b67876b1693e91d90a7`，106/106 PASS。
- 实现完成后全量PlayMode：job `8b785d96143b4be98438a367d0b44ab7`，27/27 PASS。
- 状态/证据同步后的最终全量EditMode：job `f911c6e7f7fe498a8da19756c5c4dbb8`，106/106 PASS。
- 状态/证据同步后的最终全量PlayMode：job `8920fcb0b9a840d4b599b4f52946a628`，27/27 PASS。
- 最终清空Console并执行脚本Refresh/编译后：Error 0 / Warning 0。

## 玩家路径

PlayMode从`Bootstrap`加载受管配置和AssetRegistry并进入`MainMenu`，运行时创建无Inspector数值的`PlayerCombatController`：

1. 默认刀架势生成18参考像素轨迹宽度、`damage_player_default`规则；`proj_seal_bolt`得到`RequiredStanceMismatch`。
2. 一次按钮意图等价的`TrySwitchStance(stance_talisman)`立即更新唯一状态，发布`fx_switch_to_talisman`意图；同调用后的轨迹为28参考像素、公式为`damage_talisman_default`，弹体结果为`Reflected`，冷却内回切被拒绝且无第二事件。
3. T360伤害结果把表内能量收益加入当前能量；符术从`Skills.skill_talisman_bind.energyCost`扣能。
4. 同一时间戳两次致死攻击只让第一次发布`Died`。
