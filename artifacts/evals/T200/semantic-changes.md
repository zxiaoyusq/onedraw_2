# T200 Semantic Change Review

相对任务基线 `130569421166a20eb1c2345bab4c313fb03d8ad1`，样例内容只包含以下受控语义变化；将旧样例应用这些变化并按冻结排序键重排后，与新样例逐值相等（`SEMANTIC_DIFF_EXPECTED_ONLY=PASS`）。

- 内容版本 `0.1.0-sample` → `0.1.1-sample`，并按冻结算法重算contentHash。
- 关卡玩法ID：`level_001/002/003` → `lv_001_tutorial/lv_002_cave/lv_003_boss`。
- Boss玩法ID：`boss_tomb_armor_king` → `boss_tomb_king`；旧字符串仅作为独立的文案键/资源键保留。
- 同步Levels的nextLevelId/bossEnemyId、9条Waves.levelId、1条SpawnPoints.levelId、1条Spawns.enemyId、3条BossPhases.enemyId和2条UnlockLevel奖励ID。
- Global四个互斥值列的FieldDictionary `required` 从true改为false。
- 10个不可空主键/条件字段的FieldDictionary `required` 从false改为true：DefenseRules.defenseRuleId、WeakpointRules.weakpointRuleId、Projectiles.projectileId、Buffs.buffId、SkillEffects.effectGroupId、Waves.endCondition、Rewards.conditionType/conditionValue、AudioCues.audioKey、VfxCues.vfxKey。
- 样例数组按 `docs/CONFIG_SCHEMA.md` 的Ordinal稳定导出键排序；工作簿仍保留策划易读的人工行序，T210导出时必须规范化。
- 未改任何平衡数值、文案内容、枚举值、Unity资源键、场景、Prefab、Runtime代码或SDK。
