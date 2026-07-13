# CONFIG_PIPELINE：Excel到Unity闭环

## 目录与边界

```text
Design/Config/GameConfig.xlsx                 # 唯一内容源
config/一笔镇妖_游戏配置表模板.xlsx           # 同步示例镜像，不是第二内容源
config/schema/gameplay.schema.json            # JSON结构契约
config/examples/gameplay_config.sample.json   # 与工作簿同步的审查样例
Tools/ConfigExporter/                         # T210导出器与T220生产校验器
Build/Config/gameplay_config.json.tmp         # 临时输出
Assets/_Game/Config/Generated/
  gameplay_config.json                       # T230已提交的初始Runtime快照
  gameplay_config.hash                       # T250生成
  ConfigIds.g.cs                              # T250生成
```

Runtime不解析xlsx；生成目录只由工具写入。T210/T220实现独立.NET 8确定性导出和生产级内容校验；T230提交初始受管JSON快照并接入Runtime一次性加载；T250再建立一键生成、hash旁车、ID常量和漂移检查。

## 已实现命令

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

当前工程没有全局 `dotnet` 时，可使用 Unity `6000.5.1f1` 随附的 `.NET 8 SDK` 执行相同命令。Windows脚本与Shell脚本后续只封装相同入口，不复制业务逻辑。

`validate`和`export`执行同一生产合同；错误返回非零值，并以稳定错误码及Sheet/Excel行/字段定位。校验先于序列化和写入，坏配置不会替换已有JSON。`--schema`存在时还会拒绝FieldDictionary/Enums与Schema类型、可空性、范围或枚举的漂移。

## 导出阶段

1. 固定库版本读取工作簿；验证29个Sheet名称/顺序和第4行表头。
2. 依据FieldDictionary解析类型、Trim字符串、应用明确空值语义；不读取README公式结果。
3. 根据 `docs/CONFIG_SCHEMA.md` 的固定表序与排序键构造完整内存模型。
4. 对完整模型执行主键、必填、范围、枚举、普通/分组/通配符/conditional外键、策略登记和跨表语义校验；Schema镜像漂移也在此阻断。
5. 从 `Global.config_schema_version` 和 `Global.content_version` 生成根版本字段；README公式不进入JSON。
6. 对排除 `contentHash` 后的规范化完整对象计算SHA-256：递归Ordinal键序、稳定数组序、UTF-8无BOM、紧凑JSON、无区域格式。
7. 写入 `contentHash` 后以固定缩进和UTF-8无BOM序列化；生成时间只写日志。
8. 先写目标同目录 `<output>.tmp`，落盘后重新读取并完成属性顺序、版本、记录数与hash自校验，再原子替换正式JSON；失败时保留旧输出并清理临时文件。
9. T250由同一模型/输出生成hash文件和ID常量；禁止分别实现另一套排序或解析逻辑。
10. 同一输入连续两次导出并比较全部生成文件，必须字节完全相同；测试还会反转源数据行，证明稳定排序不依赖Excel行号。

T220坏配置清单位于 `Tools/ConfigExporter/Tests/Fixtures/invalid-config-cases.json`。测试只克隆并修改内存中的原始单元格，不生成或提交派生坏xlsx；每个用例都断言稳定错误码、Sheet、Excel数据行和字段。

## Runtime加载阶段

1. Bootstrap只把受管JSON作为 `TextAsset` 资源引用交给 `GameplayConfigRuntime`，Runtime不读取文件系统，也不解析xlsx。
2. `GameplayConfigService`每个实例只允许一次加载；使用显式28表DTO严格拒绝注释、未知、缺失、重复和非法null属性，并在局部候选对象上完成全部检查。
3. 兼容合同固定为schema `1`和content `0.1.x`；根版本必须与Global对应行一致，`contentHash`必须与导出器相同的规范化SHA-256算法吻合。
4. 所有检查通过后才原子发布只读主键字典和分组列表；失败状态不发布部分索引，也不允许同一服务实例重试。
5. 业务层只依赖 `IConfigProvider` 的显式O(1)查询，不在热路径反序列化，不遍历可变根数组，也不通过反射选择战斗行为。
6. 启动日志固定输出来源、schema、content、hash、表数、记录数和索引数；不兼容或损坏配置留在Bootstrap并阻断进入MainMenu/Battle。

## 配置改动完成定义

- [ ] 只修改 `Design/Config/GameConfig.xlsx`，模板镜像由受控同步步骤更新。
- [ ] FieldDictionary与Enums需要时已同步。
- [ ] Schema、导出器、DTO、校验、文档和测试按影响范围同步。
- [ ] JSON快照由工具重新生成且hash自校验通过，无人工编辑痕迹。
- [ ] Unity编译、专项配置测试和真实启动版本/hash通过。
- [ ] 没有在C#、Inspector或ScriptableObject新增同一平衡值。
- [ ] `git diff --check`、生成文件确定性比较和改动白名单通过。
