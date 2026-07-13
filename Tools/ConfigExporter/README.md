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
```

`validate` 只读取、建模、序列化并在内存自校验，不写 JSON。`export` 先写同目录 `<output>.tmp`，自校验通过后再原子替换目标文件。成功返回 `0`，参数错误返回 `2`，配置错误返回 `3`，未预期错误返回 `4`。

## 稳定输出契约

- xlsx 需按冻结顺序包含 `README` 和 28 张数据表，数据表第 4 行是精确匹配的表头。
- 字段类型来自 `FieldDictionary`，字符串 trim，整数、小数和布尔始终用 `InvariantCulture` 解析。
- 根属性、表、行和行字段按冻结契约排序；输出 UTF-8 无 BOM、2 空格缩进、LF 结尾，不包含时间戳。
- `contentHash` 是不包含自身字段的规范化 JSON 的 SHA-256。
- 每份输出在替换前重新解析，检查顶层顺序、元数据、记录数和哈希。

## T210 校验边界

当前 `--strict` 对任何已实现的错误都返回非零值。T210 只负责可读取性、Sheet/表头、基础类型、Schema/表头对齐、排序/哈希确定性和输出自检。主键、必填、范围、枚举、外键和跨表语义由 `T220` 实现。

## 测试

```bash
dotnet test Tools/ConfigExporter/Tests/ConfigExporter.Tests.csproj
```

测试覆盖双导出字节一致、冻结哈希/样例语义一致、表头漂移、区域设置无关性、CLI 非零错误码，以及自校验失败时保护旧输出。
