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
- 当前任务：T240（READY；T120/T130按用户决定延期）

## 配置契约

- 版本：schema `1` / content `0.1.1-sample`
- 工作表：29（含README）
- 数据Sheet：28
- FieldDictionary字段条目：248；按约定不递归描述自身
- 示例JSON：31个顶层属性
- 表头/Schema/样例结构对照：PASS
- 样例JSON Schema校验：PASS；T220生产校验还会拒绝FieldDictionary/Enums与Schema约束漂移
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
- T210的确定性输出边界由T220生产校验链补全；T230已提交初始受管Runtime JSON，T250再补齐一键生成与漂移检查。
- T210结论：DONE；锁定还原、0 warning/0 error编译、专项.NET测试8/8及真实CLI双导出全部PASS，证据位于 `artifacts/evals/T210/`。

## T220生产校验基线

- `ConfigValidator`在序列化/写入前执行必填、类型、范围、枚举、稳定ID、唯一性、普通/分组/通配符/conditional外键和跨表语义校验；坏配置拒绝整包，不覆盖旧输出。
- 连续order、Global联合、星级阈值、Level→Wave→Spawn、出生点作用域及Boss从1到0无缝覆盖均有明确规则；代码策略枚举还要与登记集合精确一致。
- 37类内存坏配置覆盖重复ID、缺外键、负时间、Boss阈值乱序等路径，并逐项断言稳定错误码、Sheet、Excel数据行和字段；正式xlsx、镜像、Schema和样例未修改。
- Release锁定还原和编译0 warning/0 error，格式检查通过，全套.NET测试46/46通过。正式CLI校验28表645条记录通过，两次导出均为168,071字节且文件SHA-256均为`91d2c312...1a066a`。
- T220结论：DONE；证据位于 `artifacts/evals/T220/`。本任务未生成Runtime JSON，也未运行无改动关联的Unity场景/平台门。

## T230 Runtime配置基线

- 初始受管快照位于 `Assets/_Game/Config/Generated/gameplay_config.json`：28表、645条、168,071字节；contentHash为`16b64a6f...b4b1c`，文件SHA-256为`91d2c312...1a066a`，与正式工作簿重新导出结果字节一致。
- 28张表DTO与冻结Schema的248字段通过反射测试精确对齐；严格解析、schema `1` / content `0.1.x`兼容、根/Global版本一致、规范化hash、主键/组合键和只读索引均在发布前检查，失败不进入MainMenu。
- Runtime直接固定Unity官方 `com.unity.nuget.newtonsoft-json 3.2.2`（Newtonsoft.Json `13.0.2`）；Unity Companion License与包内第三方MIT边界已记录。
- 专项EditMode 12/12、专项PlayMode 2/2、全量EditMode 25/25、全量PlayMode 4/4及ConfigExporter .NET 46/46通过。真实Bootstrap路径记录28表/645条并进入MainMenu，Console新增Error/Warning为0。
- T230结论：DONE；证据位于 `artifacts/evals/T230/`。本任务未实现T240 AssetRegistry、T250 hash旁车/ConfigIds/一键漂移检查，也未恢复延期的平台门。

## 平台说明

T100标准Web构建和浏览器核心冒烟已通过并记录已知问题。T110已固定官方WXSDK v0.1.33 commit `ed4ad28f...`，embedded单点补丁后全工程编译与测试通过。T120已完成G2微信转换，结论为 `PASS WITH KNOWN ISSUES`；因本机缺少微信开发者工具和可用手机，G3/G4仍阻塞。用户已要求先推进主要游戏内容，因此T120保持 `BLOCKED`、T130保持 `BACKLOG`；该延期不构成微信平台PASS，也不删除发布前四级验收。
