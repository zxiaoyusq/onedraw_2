# 配置模板说明

- `一笔镇妖_游戏配置表模板.xlsx`：正式工作簿的同步示例镜像，共29个工作表（含README），不得作为第二内容源独立修改。
- `schema/gameplay.schema.json`：生成JSON的结构Schema。
- `examples/gameplay_config.sample.json`：与工作簿示例数据对应的版本化JSON。
- 正式工程中的唯一配置源应为 `Design/Config/GameConfig.xlsx`。
- 运行时仅读取构建期生成的 `gameplay_config.json`。
- 当前冻结契约为 schema `1` / content `0.1.1-sample`；ID、空值、外键、排序和hash算法见 `docs/CONFIG_SCHEMA.md`。

本模板当前示例内容：

- 1名玩家、2种架势。
- 7类敌人（含精英和Boss）。
- 10种敌人攻击、5种弹幕、5种Buff。
- 3个技能及有序效果链。
- 3个关卡、9个波次、13个出生条目、3个Boss阶段。
- 事件驱动教程、文本、音频、VFX和资源清单。

示例值仅用于首个灰盒与配置管线验证。正式平衡必须通过试玩和真机数据迭代，不要把模板数值视为最终数值。

镜像同步规则：先修改并审查 `Design/Config/GameConfig.xlsx`，再用受控表格工具覆盖本模板并验证两文件SHA-256一致。禁止从模板反向覆盖正式源。
