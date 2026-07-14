# ConfigExporter

独立 `.NET 8` 控制台工具，将 `Design/Config/GameConfig.xlsx` 导出为稳定 JSON。xlsx 读取依赖只存在于 `Tools/ConfigExporter`，不会进入 Unity Runtime。

## 命令

```bash
dotnet run --project Tools/ConfigExporter -- \
  validate \
  --input Design/Config/GameConfig.xlsx \
  --schema config/schema/gameplay.schema.json \
  --strict

dotnet run --project Tools/ConfigExporter -- \
  export \
  --input Design/Config/GameConfig.xlsx \
  --output Assets/_Game/Config/Generated/gameplay_config.json \
  --schema config/schema/gameplay.schema.json \
  --strict

dotnet run --project Tools/ConfigExporter -- \
  generate \
  --input Design/Config/GameConfig.xlsx \
  --output Assets/_Game/Config/Generated/gameplay_config.json \
  --hash-output Assets/_Game/Config/Generated/gameplay_config.hash \
  --ids-output Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs \
  --schema config/schema/gameplay.schema.json \
  --strict

dotnet run --project Tools/ConfigExporter -- \
  verify \
  --input Design/Config/GameConfig.xlsx \
  --output Assets/_Game/Config/Generated/gameplay_config.json \
  --hash-output Assets/_Game/Config/Generated/gameplay_config.hash \
  --ids-output Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs \
  --schema config/schema/gameplay.schema.json \
  --strict
```

`validate`只读取、建模、序列化并在内存自校验，不写文件；`export`保留单JSON兼容入口。`generate`从同一个已校验模型生成JSON、hash旁车和`ConfigIds.g.cs`，每个文件先写同目录`<output>.tmp`并自检后替换；`verify`只读重建预期字节并拒绝任一受管文件漂移。成功返回`0`，参数错误返回`2`，配置/生成物漂移返回`3`，未预期错误返回`4`。

仓库闭环入口：

```bash
Tools/CI/verify-config.sh           # 只读验证，默认还运行ConfigPipeline Unity EditMode/PlayMode
Tools/CI/verify-config.sh --update  # 显式重生成受管文件后执行同一完整验证
```

`--skip-unity`只用于Editor已打开时的局部反馈，会输出`CONFIG_PIPELINE_PARTIAL_PASS unity=NOT_RUN`，绝不输出完整PASS。

## 稳定输出契约

- xlsx 需按冻结顺序包含 `README` 和 29 张数据表，数据表第 4 行是精确匹配的表头。
- 字段类型来自 `FieldDictionary`，字符串 trim，整数、小数和布尔始终用 `InvariantCulture` 解析。
- 根属性、表、行和行字段按冻结契约排序；输出 UTF-8 无 BOM、2 空格缩进、LF 结尾，不包含时间戳。
- `contentHash` 是不包含自身字段的规范化 JSON 的 SHA-256。
- 每份输出在替换前重新解析，检查顶层顺序、元数据、记录数和哈希。
- `gameplay_config.hash`只包含64位小写`contentHash`及一个LF；当前为65字节。
- `ConfigIds.g.cs`位于`OneStrokeDemon.Config` asmdef作用域，按28组稳定键生成当前372个Ordinal排序常量，并嵌入schema/content/hash；UTF-8无BOM、LF结尾、无时间戳。
- C#标识符由稳定ID确定性转换；同组不同ID若产生同名标识符，生成以`CFG013`失败，不静默改名。

## 生产校验合同

`validate` 与 `export` 使用同一完整校验链。任何配置错误都在序列化和原子写入前阻断整包，不半应用、不静默修正。当前生产校验覆盖：

- 30个Sheet及表头、FieldDictionary覆盖、字段类型/空值、`contentVersion`和Schema合同。
- 必填、稳定ID、主键/组合键唯一、min/max、时间/归一化范围、大小写敏感枚举。
- FieldDictionary、Enums与JSON Schema中的类型、可空性、范围和枚举镜像一致性。
- 普通、分组、唯一通配符及conditional外键，资源、文案、音频和VFX引用。
- 六类分组连续order、Global联合、星级阈值、Level→Wave→Spawn完整性和出生点作用域。
- Boss阶段从1连续覆盖到0、阈值严格下降且无缝相接，Boss关卡结束条件与实际出生一致。
- `MovePatternType`、`AttackTriggerType`、`EffectType`和`TargetType`必须与代码登记的策略/执行器/选择器集合精确一致。

配置诊断格式为 `CODE [sheet=..., row=..., field=...]: message`。稳定错误码按类别划分：`CFG001/CFG002`结构合同、`CFG003`必填、`CFG004`类型、`CFG005`唯一性、`CFG006`枚举/策略、`CFG007`范围、`CFG008`外键、`CFG009`行内语义、`CFG010`跨表/Boss、`CFG011`版本、`CFG012`JSON输出自检、`CFG013`多生成物/路径/字节漂移。

## 测试

```bash
dotnet test Tools/ConfigExporter/Tests/ConfigExporter.Tests.csproj
```

当前58项测试覆盖三生成物双生成字节一致、受管文件精确匹配、JSON/hash/C#三类漂移、冻结哈希/样例语义一致、表头漂移、区域设置无关性、CLI非零错误码、自校验失败保护旧输出，以及41类只修改内存副本的坏配置。坏配置断言精确错误码、Sheet、Excel行和字段；正式xlsx在测试中保持只读。
