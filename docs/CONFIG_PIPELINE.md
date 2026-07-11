# CONFIG_PIPELINE：Excel到Unity闭环

## 目录与产物

```text
Design/Config/GameConfig.xlsx
Tools/ConfigExporter/
Build/Config/gameplay_config.json.tmp
Assets/_Game/Config/Generated/
  gameplay_config.json
  gameplay_config.hash
  ConfigIds.g.cs
```

## 计划命令

```bash
dotnet run --project Tools/ConfigExporter --   export   --input Design/Config/GameConfig.xlsx   --output Assets/_Game/Config/Generated/gameplay_config.json   --schema config/schema/gameplay.schema.json   --strict

dotnet run --project Tools/ConfigExporter --   validate --input Design/Config/GameConfig.xlsx --strict
```

Windows脚本与Shell脚本只封装同一命令，不复制逻辑。

## 导出要求

1. 表头与schema精确比对。
2. 使用InvariantCulture解析数值。
3. Trim字符串，规范布尔和枚举大小写。
4. 执行结构、范围、唯一性、外键和跨表语义校验。
5. 按表固定顺序、按主键稳定排序。
6. JSON使用固定缩进、UTF-8和稳定小数格式。
7. `contentHash`基于规范化内容生成；生成时间写日志，不写入制造diff的内容。
8. 先写临时文件，通过自校验后原子替换正式JSON。
9. 同一输入连续两次导出必须字节完全相同。

## 配置改动完成定义

- [ ] Excel已更新。
- [ ] FieldDictionary与Enums需要时已更新。
- [ ] 导出器、DTO、schema需要时已更新。
- [ ] JSON快照已重新生成。
- [ ] 校验和专项测试通过。
- [ ] Unity启动版本与hash正确。
- [ ] 没有在C#或Inspector新增同一数值。
- [ ] Git diff可解释，生成JSON没有人工编辑痕迹。
