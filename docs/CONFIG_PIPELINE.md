# CONFIG_PIPELINE：Excel到Unity闭环

## 目录与边界

```text
Design/Config/GameConfig.xlsx                 # 唯一内容源
config/一笔镇妖_游戏配置表模板.xlsx           # 同步示例镜像，不是第二内容源
config/schema/gameplay.schema.json            # JSON结构契约
config/examples/gameplay_config.sample.json   # 与工作簿同步的审查样例
Tools/ConfigExporter/                         # T210/T220实现
Build/Config/gameplay_config.json.tmp         # 临时输出
Assets/_Game/Config/Generated/
  gameplay_config.json
  gameplay_config.hash
  ConfigIds.g.cs
```

Runtime不解析xlsx；生成目录只由工具写入。T200只冻结输入、Schema、样例和算法契约，不提前创建Runtime JSON或实现T210/T220。

## 计划命令

```bash
dotnet run --project Tools/ConfigExporter -- \
  validate \
  --input Design/Config/GameConfig.xlsx \
  --strict

dotnet run --project Tools/ConfigExporter -- \
  export \
  --input Design/Config/GameConfig.xlsx \
  --output Assets/_Game/Config/Generated/gameplay_config.json \
  --schema config/schema/gameplay.schema.json \
  --strict
```

Windows脚本与Shell脚本只封装同一命令，不复制业务逻辑。

## 导出阶段

1. 固定库版本读取工作簿；验证29个Sheet名称/顺序和第4行表头。
2. 依据FieldDictionary解析类型、Trim字符串、应用明确空值语义；不读取README公式结果。
3. 执行主键、范围、枚举、普通/分组/通配符/conditional外键和跨表语义校验。
4. 根据 `docs/CONFIG_SCHEMA.md` 的固定表序与排序键构造完整内存模型。
5. 从 `Global.config_schema_version` 和 `Global.content_version` 生成根版本字段并核对README。
6. 对排除 `contentHash` 后的规范化完整对象计算SHA-256：递归Ordinal键序、稳定数组序、UTF-8无BOM、紧凑JSON、无区域格式。
7. 写入 `contentHash` 后以固定缩进和UTF-8无BOM序列化；生成时间只写日志。
8. 先写同文件系统临时文件，重新读取并完成Schema/内容/hash自校验后原子替换正式JSON。
9. 由同一模型生成hash文件和ID常量；禁止分别实现另一套排序或解析逻辑。
10. 同一输入连续两次导出并比较全部生成文件，必须字节完全相同。

## 配置改动完成定义

- [ ] 只修改 `Design/Config/GameConfig.xlsx`，模板镜像由受控同步步骤更新。
- [ ] FieldDictionary与Enums需要时已同步。
- [ ] Schema、导出器、DTO、校验、文档和测试按影响范围同步。
- [ ] JSON快照由工具重新生成且hash自校验通过，无人工编辑痕迹。
- [ ] Unity编译、专项配置测试和真实启动版本/hash通过。
- [ ] 没有在C#、Inspector或ScriptableObject新增同一平衡值。
- [ ] `git diff --check`、生成文件确定性比较和改动白名单通过。
