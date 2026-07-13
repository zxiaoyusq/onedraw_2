# T460 Workbook Validation

- 正式源：`Design/Config/GameConfig.xlsx`；镜像：`config/一笔镇妖_游戏配置表模板.xlsx`。
- 两文件字节比较：PASS；SHA-256均为`6c9313231d37d981128ba464f34d8a0f329a0535ccc01fb29db3af23c7b016b1`。
- 使用artifact-tool精确修改Global content版本、MovePatterns二/三阶段行和BossPhases引用；未增加Sheet、列或FieldDictionary字段。
- 对最终工作簿29个Sheet全部重新渲染并检查；重点`MovePatterns`、`BossPhases`截图分别归档为`workbook-move-patterns.png`、`workbook-boss-phases.png`，表头、冻结样式、数据验证和列宽可读。
- 公式错误扫描：0个`#REF!/#DIV/0!/#VALUE!/#NAME?/#N/A`匹配。
- 重点值：phase1/2/3移动倍率`0.5/0.8/1.2`；BossPhases引用`move_boss_ground/move_boss_phase2/move_boss_phase3`；阈值保持`1→0.67→0.34→0`。
