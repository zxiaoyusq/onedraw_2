# ConfigExporter 计划规格

此目录是独立 `.NET 8` 控制台工具的目标位置。由任务 `T210/T220/T250` 实现。不要把xlsx解析库带入Unity运行时。

## 目标命令

```bash
dotnet run --project Tools/ConfigExporter -- \
  validate --input Design/Config/GameConfig.xlsx --strict

dotnet run --project Tools/ConfigExporter -- \
  export \
  --input Design/Config/GameConfig.xlsx \
  --output Assets/_Game/Config/Generated/gameplay_config.json \
  --schema config/schema/gameplay.schema.json \
  --strict
```

Windows与Shell脚本只包装相同命令，不复制业务逻辑。

## 建议项目结构

```text
Tools/ConfigExporter/
├─ ConfigExporter.csproj
├─ Program.cs
├─ Commands/
├─ Excel/
├─ Model/
├─ Validation/
├─ Serialization/
└─ Tests/
```

## 强制规则

1. 使用固定版本的xlsx读取库，并记录许可证。
2. 不依赖安装Excel。
3. 所有数值使用InvariantCulture。
4. Trim字符串；统一布尔、枚举和空值语义。
5. 表头必须与字段契约精确匹配。
6. 校验唯一主键、范围、枚举、外键、Boss阈值、关卡星级递增等跨表语义。
7. 输出按固定表顺序、主键和order稳定排序。
8. UTF-8、固定缩进、稳定小数格式。
9. `contentHash`基于规范化内容，不包含生成时间。
10. 先写`.tmp`，完成自校验后原子替换。
11. 同一输入连续导出两次必须字节完全相同。
12. 错误包含sheet、行号、字段、错误码和可读说明。
13. 严格模式任何错误返回非零退出码。
14. 生成JSON禁止人工编辑。

## 错误码建议

- `CFG001` 缺少Sheet
- `CFG002` 表头不匹配
- `CFG003` 必填为空
- `CFG004` 类型解析失败
- `CFG005` 主键重复
- `CFG006` 枚举非法
- `CFG007` 数值越界
- `CFG008` 外键不存在
- `CFG009` 跨字段关系非法
- `CFG010` 跨表语义非法
- `CFG011` Schema版本不兼容
- `CFG012` 输出不确定

## 必须测试

- 同输入双导出字节相同。
- 重复ID、缺外键、非法枚举、负时间、Boss阈值乱序。
- 星级门槛不递增。
- 空白行与前后空格规范化。
- 中文与小数区域设置。
- 临时文件失败时不破坏旧JSON。
