# T360 Workbook Validation

- 工具：Spreadsheets技能要求的`@oai/artifact-tool`；未使用openpyxl或手工编辑xlsx内部XML。
- 正式源：`Design/Config/GameConfig.xlsx`
- 中文镜像：`config/一笔镇妖_游戏配置表模板.xlsx`
- 临时验证副本：`outputs/T360-config/GameConfig.xlsx`（完成字节比较后删除，不作为第二内容源或提交文件）
- 正式源、中文镜像和临时验证副本的SHA-256均为`c1c04c57a62681ecef4c912bb34cf1b075eb6a297751029cfb0b622846aedb8f`，比较时字节一致。
- `README`与`Global`均为schema 2/content 0.2.0-sample。
- `Stances`新增必填`damageFormulaId`：刀→`damage_player_default`，符→`damage_talisman_default`。
- `DamageFormulas`新增非负`scorePerDamage`，三条样例均为1.00。
- `FieldDictionary`新增上述两条字段记录，总数250；架势公式外键为`DamageFormulas.formulaId`。
- 导出后29张Sheet全部重新渲染并组成总览；重点原尺寸复核README、Global、Stances、DamageFormulas和FieldDictionary，标题、说明、表头、交替行色、数字格式、列宽及合并范围正常。
- 公式错误扫描`#REF!/#DIV/0!/#VALUE!/#NAME?/#N/A`命中0项。
