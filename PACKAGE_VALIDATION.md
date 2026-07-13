# 开发包与当前基线验证报告

更新日期：2026-07-13

## 任务计划

- 原子任务：49
- 计划人日：84.0
- 依赖引用错误：0
- 依赖图：PASS（无环）
- Bootstrap/Harness任务：T000、T010、T020、T030、T040（DONE）
- 标准Web G1：T100（PASS WITH KNOWN ISSUES）
- 配置契约：T200（DONE）
- 当前任务：T220（READY；T120/T130按用户决定延期）

## 配置契约

- 版本：schema `1` / content `0.1.1-sample`
- 工作表：29（含README）
- 数据Sheet：28
- FieldDictionary字段条目：248；按约定不递归描述自身
- 示例JSON：31个顶层属性
- 表头/Schema/样例结构对照：PASS
- 样例JSON Schema校验：PASS（T200项目内只读契约审计；生产校验器属于T220）
- 主键/组合键重复：0
- 普通、分组、通配符和conditional外键缺失：0
- 组内order/关卡-Boss跨表语义错误：0
- 示例内容哈希：`16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`

## 工作簿

- 正式源：`Design/Config/GameConfig.xlsx`
- 示例镜像：`config/一笔镇妖_游戏配置表模板.xlsx`
- 两份工作簿SHA-256：`aa215a1fd5c798da97e5d21a7ab9b71b3ee1dd3f2326c424e17f023ef80ca52a`（字节一致）
- 工作簿大小：87,238字节
- 配置对象：玩家、架势、手势、公式、护甲、弱点、移动、敌人、攻击、弹幕、Buff、技能、效果、关卡、波次、出生、Boss、奖励、教程、文本、音频、VFX和资源。
- README公式：14个，仅做记录数摘要，引用带Sheet单引号且限定范围；公式错误0条，不进入JSON。
- 29个Sheet均由artifact-tool完成最终结构检查与渲染；4张全表总览拼图人工复核无明显裁切、错位或破损。

## T200结论

- 按玩法权威文档统一关卡ID `lv_001_tutorial`、`lv_002_cave`、`lv_003_boss` 与Boss ID `boss_tomb_king`，并同步全部依赖引用。
- 冻结数据所有权、稳定ID、独立命名空间、空值、枚举、主键/组合键、分组ID、通配符、conditional外键、稳定排序和contentHash算法。
- 工作簿、FieldDictionary、Schema和样例JSON对照审计为PASS；证据位于 `artifacts/evals/T200/`。
- T200未实现.NET导出器、生产校验器或Runtime加载，也未生成 `Assets/_Game/Config/Generated/gameplay_config.json`。

## T210导出器基线

- 独立 `net8.0` CLI位于 `Tools/ConfigExporter`，使用精确固定并锁定的 `DocumentFormat.OpenXml 3.5.1`；依赖未进入 `Assets/` 或Unity程序集。
- `validate/export --input ... --schema ... --strict` 已读取29个Sheet、导出28张表共645条记录，版本为schema `1` / content `0.1.1-sample`，规范化内容hash保持 `16b64a6f...b4b1c`。
- 同一输入两次输出均为168,071字节，文件SHA-256均为 `91d2c312cd2caead5243ef76ee12b54dc53702dc0ba23d4d34b0726c111a066a`；自动测试同时覆盖反转源行、区域设置、表头漂移、CLI错误码和原子写保护。
- T210不执行必填/范围/枚举/唯一性/外键/跨表生产校验，不生成受管Runtime JSON；这些仍分别属于T220、T230/T250。
- T210结论：DONE；锁定还原、0 warning/0 error编译、专项.NET测试8/8及真实CLI双导出全部PASS，证据位于 `artifacts/evals/T210/`。

## 平台说明

T100标准Web构建和浏览器核心冒烟已通过并记录已知问题。T110已固定官方WXSDK v0.1.33 commit `ed4ad28f...`，embedded单点补丁后全工程编译与测试通过。T120已完成G2微信转换，结论为 `PASS WITH KNOWN ISSUES`；因本机缺少微信开发者工具和可用手机，G3/G4仍阻塞。用户已要求先推进主要游戏内容，因此T120保持 `BLOCKED`、T130保持 `BACKLOG`；该延期不构成微信平台PASS，也不删除发布前四级验收。
