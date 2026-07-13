# T200 Workbook Visual Review

- 工具：artifact-tool逐Sheet渲染，`autoCrop=all`、`scale=1`、PNG。
- 范围：29/29个Sheet；最终工作簿SHA-256见 `workbook-hashes.txt`。
- `workbook-preview-1.png`：README、Global、Players、Stances、StrokeRules、DamageFormulas、DefenseRules、WeakpointRules。
- `workbook-preview-2.png`：MovePatterns、Enemies、EnemyAttacks、Projectiles、Buffs、Skills、SkillEffects、Levels。
- `workbook-preview-3.png`：Waves、SpawnPoints、EnemyModifiers、Spawns、BossPhases、Rewards、Tutorials、Texts。
- `workbook-preview-4.png`：AudioCues、VfxCues、AssetManifest、Enums、FieldDictionary。
- 复核结果：标题、说明、表头、斑马纹、布尔复选框、下拉显示、关键ID和README所有权说明均正常；未发现明显裁切、错位、空白渲染、公式错误或格式破损。
- 长表以全表缩略图验证结构完整性；字段值/公式/记录数由 `contract-audit.json` 的结构化审计覆盖，不依赖肉眼读取缩略文字。
