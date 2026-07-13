# T460 Boss Player Path

PlayMode真实读取受管配置，生成`boss_tomb_king`及玩家`player_moyan`，然后由`BossPhaseController`运行以下路径：

1. 满血1200进入phase1：护甲120、速度20、无弱点，执行`fx_boss_phase1_enter`和`atk_boss_rockfall`投射物各一次。
2. 受到516来伤：护甲吸收120、HP减少396到804，比例精确0.67；进入phase2时HP保持804，护甲按新规则重置60、速度32、封印弱点可在表内窗口打开，执行`fx_boss_phase2_enter`和`atk_boss_seal_wave`投射物各一次。
3. 再受456来伤：护甲吸收60、HP减少396到408，比例精确0.34；进入phase3时HP保持408，护甲0、速度48，执行`fx_boss_phase3_enter`和`atk_boss_charge`冲撞各一次。
4. 最终阶段事件3次、`EnemyController`阶段事件3次、进入VFX 3次、攻击VFX 3次、攻击动作3次；重复观察HP不产生额外阶段事件。

内存配置变体把phase1/2边界改为0.6、phase2速度倍率改为1.05、防御和弱点改为none；重算hash并重新加载后直接得到0.6边界、速度42、护甲0、无弱点，产品C#无分支变化。
