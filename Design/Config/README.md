# Design/Config

`GameConfig.xlsx` 是正式项目的策划内容唯一真相源，当前契约为 schema `1` / content `0.1.1-sample`。

数据所有权：表头、ID、枚举和外键契约由程序与策划共同审批；玩法内容、数值、关卡、教程和文案由策划维护；`assetKey`由策划与美术共同维护。JSON与ID常量只由构建工具生成。

禁止：
- 在Unity运行时解析xlsx。
- 手工修改生成JSON而不修改本工作簿。
- 在Inspector或C#复制相同的平衡数值。
- 改字段名但不更新FieldDictionary、导出器、Schema、DTO、校验和测试。

ID、空值、分组、通配符、conditional外键、稳定排序和contentHash算法以 `docs/CONFIG_SCHEMA.md` 为准。`config/一笔镇妖_游戏配置表模板.xlsx` 只是同步镜像，不能独立维护。

T210完成后使用 `Tools/ConfigExporter` 执行validate/export；当前目录中尚无可用导出器实现。
