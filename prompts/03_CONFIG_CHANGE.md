# 03_CONFIG_CHANGE：配置契约变更

完整读取 `AGENTS.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md` 和当前任务。

## 先判定

- 这是仅修改数据行，还是修改字段/结构？
- 是否会改变Schema版本或内容版本？
- 影响哪些外键、索引、资源键、公式、测试和关卡？
- 是否存在Excel与Inspector/C#双真相？

## 数据行变更必须同步

1. `Design/Config/GameConfig.xlsx`
2. 导出的稳定JSON
3. 配置校验结果
4. JSON diff
5. 受影响的专项测试
6. 证据和内容版本

## 字段/结构变更必须同步

1. Excel表头和模板
2. `FieldDictionary`
3. 导出器DTO/解析
4. JSON Schema
5. 版本化JSON快照
6. Unity DTO
7. 结构、范围、枚举、外键和跨表校验
8. 运行时只读索引
9. 配置文档
10. EditMode/PlayMode回归

## 禁止

- 运行时解析xlsx。
- 只改Excel不改JSON。
- 只改DTO不改字段字典。
- 坏配置半应用。
- 在Inspector添加同义数值兜底而不定义覆盖规则。
- 手工修改生成JSON后不更新源Excel。

完成后输出配置版本、哈希、校验摘要和影响清单。
