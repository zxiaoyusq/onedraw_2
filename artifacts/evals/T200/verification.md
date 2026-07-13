# T200 Verification

## 追溯

- 日期：2026-07-13。
- 任务与范围：冻结正式配置工作簿、FieldDictionary、Enums、ID/空值/外键/排序/hash规则与数据所有权，使T210可实现无歧义的确定性导出器。
- 明确不做：不实现T210导出器、T220生产校验器或T230 Runtime加载；不生成 `Assets/_Game/Config/Generated/gameplay_config.json`；不改Gameplay、Unity场景/Prefab、SDK或平台设置；不恢复T120/T130。
- 分支/提交：`main`；任务基线 `130569421166a20eb1c2345bab4c313fb03d8ad1`（T120阻塞检查点）；本任务以 `T200: freeze configuration contract` 形成单一可回滚提交。
- 任务开始Git基线：见 `baseline-commit.txt` 与 `baseline-status.txt`；工作树干净，无需覆盖的用户已有改动。
- Unity精确版本：`6000.5.1f1`，沿用已核验工程基线；本任务未修改 `Assets/`、`Packages/` 或 `ProjectSettings/`。
- 配置：schema `1` / content `0.1.1-sample` / contentHash `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`。

## 改动审查

- 预计白名单：见 `change-whitelist.md`；在发现非权威开发计划仍含旧Boss玩法ID后，先补充该文档到白名单再同步。
- 实际改动：通过artifact-tool更新正式工作簿及字节一致镜像；同步样例JSON和Schema；重写配置契约/管线说明；新增D-012；同步PACKAGE_VALIDATION、TASKS、PROGRESS、project-index及T200证据。
- 用户已有改动保护：任务开始无用户改动；未接触白名单外Unity资产、源码或平台文件。
- 正式源/镜像边界：两份xlsx均为87,238字节，SHA-256同为 `aa215a1fd5c798da97e5d21a7ab9b71b3ee1dd3f2326c424e17f023ef80ca52a`；镜像不能独立维护。
- 语义差异：`semantic-changes.md` 所列变更机械应用到基线样例并按冻结键排序后，与最终样例逐值相等；`SEMANTIC_DIFF_EXPECTED_ONLY=PASS`。
- `git diff --check`：PASS；JSON与YAML可解析；两份xlsx通过ZIP完整性检查。
- 白名单审查：PASS；所有受管改动均属于 `change-whitelist.md`，`Tools/ConfigExporter`、`Assets/`、`Packages/`、`ProjectSettings/`和Runtime生成JSON无差异。

## 配置与表格验证

- T200项目内只读契约审计：27/27项PASS；该证据脚本位于忽略目录，仅用于T200，不是T210/T220生产实现。
- 工作簿结构：29/29个Sheet，固定名称/顺序；28个数据Sheet；FieldDictionary 248/248条且按Sheet/表头顺序、非递归描述自身。
- Schema/样例：全部表头与 `$defs/*Row.properties/required` 一致；31个顶层属性顺序正确；样例通过当前JSON Schema；工作簿规范化数据与稳定排序后的样例逐值一致。
- 类型/语义：必填、类型、范围、Trim、Global判别联合、89个枚举、主键/组合键、稳定ID、唯一通配符、普通/分组/conditional外键、组内连续order、星级、Level→Wave→Spawn和Boss阈值全部PASS。
- 确定性：样例28个数组符合冻结的Ordinal排序键；contentHash按排除自身后的递归Ordinal键序、稳定数组序、UTF-8紧凑JSON重算一致。
- 公式：README共14个受限 `COUNTA` 公式，Sheet引用带单引号、范围为 `A5:A1000`；公式错误0；公式与结果不导出。
- 文件完整性：样例JSON SHA-256 `39f05301c3f6d4417dc43389daf38e34822eb623fbbd2024cc2a59cd4f0c3259`；Schema SHA-256 `721c2081e69c320cc16f71d4e1292c03642cfb67745ac47c2c171a333da86bd7`。

## Unity与玩家路径

- Unity Refresh/编译：NOT RUN。本任务只改Unity工程外的xlsx/JSON/Markdown/YAML且未创建Runtime产物，不会进入Unity Asset Database或C#编译；以路径边界和Git差异审查代替无关Editor运行。
- EditMode/PlayMode：NOT RUN；没有C#、asmdef、场景、Prefab或Unity资源改动，不重复T120的13/13 EditMode与2/2 PlayMode历史结论。
- Console Error/Warning：NOT RUN；本任务没有Unity导入范围内改动。
- 真实玩家路径：NOT RUN；T200验收是配置契约人工审查，不是玩法接入。不得把样例JSON或只读审计外推为游戏已加载配置。
- 标准Web/微信转换/DevTools/真机：NOT RUN新验证；平台状态沿用T120，G2为PASS WITH KNOWN ISSUES，G3/G4仍BLOCKED。

## 视觉证据

- artifact-tool对最终工作簿29/29个Sheet逐表渲染成功。
- `workbook-preview-1.png` 至 `workbook-preview-4.png` 覆盖全部Sheet；以original detail人工复核，无明显裁切、错位、空白或格式破损。
- 详细分组与审查结论见 `visual-review.md`；结构化字段和长表内容由 `contract-audit.json` 覆盖。

## 结论

- 已知边界：T210尚未实现真实xlsx读取、稳定JSON落盘与双导出字节测试；T220尚未实现带Sheet/行/字段定位的生产级坏配置拒绝；T230尚未接入Unity Runtime。T120平台阻塞保持不变。
- 结论：PASS。T200验收完成，T210成为唯一READY任务；本任务没有提前开始T210或把平台阻塞写成PASS。
