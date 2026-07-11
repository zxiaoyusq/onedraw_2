# Design/Config

`GameConfig.xlsx` 是正式项目的策划内容唯一真相源。

禁止：
- 在Unity运行时解析xlsx。
- 手工修改生成JSON而不修改本工作簿。
- 在Inspector或C#复制相同的平衡数值。
- 改字段名但不更新FieldDictionary、导出器、Schema、DTO、校验和测试。

使用 `Tools/ConfigExporter` 执行validate/export。
