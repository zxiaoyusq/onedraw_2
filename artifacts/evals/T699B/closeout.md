# T699B Closeout

## 起始状态

- 任务开始时间：2026-08-01（Asia/Shanghai）。
- 起始提交：`b838add0 T699A: integrate production projectiles`。
- 起始Git状态仅有用户保留改动：` D Design/Config/~$GameConfig.xlsx`；本任务不恢复、不删除、不暂存该路径。
- 当前缺口：31个Sheet已有中文用途概要，FieldDictionary覆盖29个业务数据Sheet的280个字段且说明非空，但165条仍是“X表的Y字段”占位文案；全部280个第4行API字段名均为英文，工作表内没有就近中文字段名或单元格批注。

## 预计改动白名单

- 权威配置与同步镜像：`Design/Config/GameConfig.xlsx`、`config/一笔镇妖_游戏配置表模板.xlsx`。
- 配置生成物：`Assets/_Game/Config/Generated/gameplay_config.json`、`gameplay_config.hash`、`Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`、`config/examples/gameplay_config.sample.json`。
- 配置冻结测试：`Tools/ConfigExporter/Tests/ExporterDeterminismTests.cs`、`ConfigPipelineE2ETests.cs`；`Assets/_Game/Tests/EditMode/T230/RuntimeConfigLoadTests.cs`；`Assets/_Game/Tests/PlayMode/T230/`、`T240/`、`T600/`、`T610/`、`T650/`内版本/hash快照。
- T699B覆盖测试：按最小职责新增到`Tools/ConfigExporter/Tests/`或`Assets/_Game/Tests/EditMode/T699B/`，以及Unity `.meta`。
- 文档与索引：`docs/TASKS.md`、`docs/PROGRESS.md`、`docs/CONFIG_SCHEMA.md`、`docs/CONFIG_PIPELINE.md`、`project-index.yaml`和本文件；仅保留必要视觉证据。

## 验证摘要

- 工作簿覆盖：31/31个Sheet的A2包含中文用途说明；30个数据Sheet的第3行共290/290个非空英文API表头均有同列中文名称；FieldDictionary的280/280条description均包含中文、以前缀中文名称加全角冒号开头，并与目标Sheet第3行一致。
- 说明质量：原165条“X表的Y字段”占位文案已归零；说明覆盖稳定ID、单位/时间口径、范围、枚举、外键、留空语义、池化/运行时用途等适用信息。FieldDictionary继续不递归描述自身，但自身10个列名已在第3行中文化。
- 数据边界：结构审计确认除30个Sheet第3行、README/Global content版本和FieldDictionary.description外没有单元格值变化；第4行API字段、字段结构、玩法数值、ID、枚举、外键、表数和记录数均未改变；公式错误扫描0。
- 视觉：使用artifact-tool对修改前后各31个Sheet全部渲染检查；中文字段名采用浅蓝说明行，与深蓝英文API表头形成明确层级，FieldDictionary长说明列完整显示，无明显截断、错位或空白Sheet。
- 配置：schema 6 / content `0.6.8-sample` / content hash `6ab856f9e53dc3726c684340df8851d88b0447833872a21a01b737ed49847fdb`；30表/763条、29组381个ID常量。
- 文件：双工作簿均110,303字节、SHA-256 `7ccb004ea4bc5cfe3ff3d179c268d2085c65e9cfa65be71677581b430b529567`且`cmp`一致；受管JSON为216,081字节、文件SHA-256 `b523b205d13ed750687474c40527e15199423779943a836ac71ae8506aab61c0`，审查样例与其字节一致。
- 自动化：新增工作簿中文覆盖测试2/2；ConfigExporter 62/62，受管JSON/hash/ConfigIds只读verify与临时生成cmp通过；最终全量EditMode 215/215（15.39秒）、PlayMode 59/59（55.44秒）。
- 环境噪声：首次EditMode初始化在域刷新后保持0项，清理MCP孤儿任务后有效回归215/215；首次PlayMode因`editor_is_focused=false`触发配置自动暂停，导致4项入口/弹体测试停止推进，Game视图聚焦后受影响项4/4及全量59/59通过。
- 范围：未修改Schema、DTO、Scene、Prefab、Registry、ProjectSettings、Packages或玩法数值；T700未提前实施。

## 用户改动保护

- `Design/Config/~$GameConfig.xlsx`的起始删除状态持续保留并排除在提交外。
