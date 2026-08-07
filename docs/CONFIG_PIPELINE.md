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
  gameplay_config.json                       # T250受管Runtime快照
  gameplay_config.hash                       # T250受管contentHash旁车
Assets/_Game/Scripts/Config/Generated/
  ConfigIds.g.cs                              # T250受管ID常量，属于OneStrokeDemon.Config
```

Runtime不解析xlsx；生成目录只由工具写入。T210/T220实现独立.NET 8确定性导出和生产级内容校验；T230接入Runtime一次性加载；T240绑定Unity对象；T250从同一模型生成受管JSON、hash和ID常量，并建立一键漂移/Unity回归门。

## 当前受管快照

- schema `7` / content `0.7.0-sample` / content hash `e0b0dcecdcea50ad079c8b7880d0f7a7a0df6771d671fecf13bf57845dbe5448`。
- 权威工作簿与模板镜像均为126,182字节、SHA-256 `8b77e6054281e7a9bd471a7900c9606e0bec3927f11ea4c7355d0c1c8465d7e2`，字节完全一致。
- 受管JSON为264,312字节、文件SHA-256 `7ac24ca94012ea99df3e05854673684b04cc6f9d61842f568751f4ed657d4a32`，包含30个数据表、772条记录；`ConfigIds.g.cs`为29组385个常量。
- T699H在普通战斗的`Any/Charged`基础上新增`Triangle`纯形状识别，并通过`StrokeRules.onMatchSkillId -> Skills -> SkillEffects`配置链触发全体怪物减速；FieldDictionary为284条、Enums为99条，Registry仍为78键。默认只读生成/漂移门与ConfigExporter 64项测试通过，Unity全量EditMode 223项、PlayMode 62项通过。

## 已实现命令

仓库根目录的默认完整门：

```bash
Tools/CI/verify-config.sh
```

该命令依次构建导出器、在临时目录生成三份预期文件、只读比较受管生成物、执行ConfigExporter全套测试，再以`ConfigPipeline`分类执行Unity EditMode和PlayMode。任何配置错误、缺失/漂移生成物、空测试集或测试失败均返回非零；只有四层全绿才输出`CONFIG_PIPELINE_PASS`。

策划修改正式工作簿后显式更新并复核：

```bash
Tools/CI/verify-config.sh --update
```

默认命令永不改受管文件。`--update`先用同一`generate`入口重生成，再执行完整门；仍须审查Git diff并提交全部受影响生成物。Editor已打开时可临时使用`--skip-unity`完成.NET与字节门，但只输出`CONFIG_PIPELINE_PARTIAL_PASS`，不能作为T250完整结论。

底层CLI：

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

当前工程没有全局`dotnet`时，`verify-config.sh`会按`ProjectVersion.txt`解析并使用Unity `6000.5.1f1`随附的.NET 8 SDK；也可通过`--dotnet`或`DOTNET`显式指定。脚本只负责编排，读取、校验、排序、hash和生成逻辑仍只有ConfigExporter一份。

`validate`和`export`执行同一生产合同；错误返回非零值，并以稳定错误码及Sheet/Excel行/字段定位。校验先于序列化和写入，坏配置不会替换已有JSON。`--schema`存在时还会拒绝FieldDictionary/Enums与Schema类型、可空性、范围或枚举的漂移。

## 导出阶段

1. 固定库版本读取工作簿；验证31个Sheet名称/顺序和第4行表头。
2. 依据FieldDictionary解析类型、Trim字符串、应用明确空值语义；不读取README公式结果。
3. 根据 `docs/CONFIG_SCHEMA.md` 的固定表序与排序键构造完整内存模型。
4. 对完整模型执行主键、必填、范围、枚举、普通/分组/通配符/conditional外键、策略登记和跨表语义校验；Schema镜像漂移也在此阻断。
5. 从 `Global.config_schema_version` 和 `Global.content_version` 生成根版本字段；README公式不进入JSON。
6. 对排除 `contentHash` 后的规范化完整对象计算SHA-256：递归Ordinal键序、稳定数组序、UTF-8无BOM、紧凑JSON、无区域格式。
7. 写入 `contentHash` 后以固定缩进和UTF-8无BOM序列化；生成时间只写日志。
8. 先写目标同目录 `<output>.tmp`，落盘后重新读取并完成属性顺序、版本、记录数与hash自校验，再原子替换正式JSON；失败时保留旧输出并清理临时文件。
9. T250从同一个`PreparedExport`生成JSON、64位hash+LF旁车，以及位于Config asmdef内的29组/当前385项`ConfigIds.g.cs`；禁止分别实现另一套排序、解析或数值真相。
10. 同一输入连续两次导出并比较全部生成文件，必须字节完全相同；测试还会反转源数据行，证明稳定排序不依赖Excel行号。

## T250生成物与漂移门

1. `generate`在完整生产校验通过后才构造三份字节；JSON继续执行`CFG012`结构/hash自检，hash和C#执行精确字节自检，三个输出路径必须互不相同。
2. `verify`只读重建预期字节并逐文件比较，不更新时间戳或修正文件；缺失、任意字节漂移、C#标识符冲突或输出路径冲突均以`CFG013`失败。
3. `ConfigIds.g.cs`只包含稳定ID/Key和schema/content/hash元数据，不包含HP、CD、伤害或Unity对象引用；Runtime内容仍由JSON加载。
4. 一键脚本会在临时目录再次生成并执行`cmp`；漂移时输出受管文件与预期文件的unified diff，验证通过后运行.NET 64项及Unity分类测试。
5. Unity T250测试断言hash旁车、生成元数据、JSON Runtime hash一致，C#实际编入`OneStrokeDemon.Config.dll`，29组385常量均为稳定ID，并用代表性常量完成类型化配置/Registry查询。

T220坏配置清单位于 `Tools/ConfigExporter/Tests/Fixtures/invalid-config-cases.json`。测试只克隆并修改内存中的原始单元格，不生成或提交派生坏xlsx；每个用例都断言稳定错误码、Sheet、Excel数据行和字段。

## Runtime加载阶段

1. Bootstrap只把受管JSON作为 `TextAsset` 资源引用交给 `GameplayConfigRuntime`，Runtime不读取文件系统，也不解析xlsx。
2. `GameplayConfigService`每个实例只允许一次加载；使用显式30表DTO严格拒绝注释、未知、缺失、重复和非法null属性，并在局部候选对象上完成全部检查。
3. 兼容合同固定为schema `7`和content `0.7.x`；根版本必须与Global对应行一致，`contentHash`必须与导出器相同的规范化SHA-256算法吻合。
4. 所有检查通过后才原子发布只读主键字典和分组列表；失败状态不发布部分索引，也不允许同一服务实例重试。
5. 业务层只依赖 `IConfigProvider` 的显式O(1)查询，不在热路径反序列化，不遍历可变根数组，也不通过反射选择战斗行为。
6. 启动日志固定输出来源、schema、content、hash、表数、记录数和索引数；不兼容或损坏配置留在Bootstrap并阻断进入MainMenu/Battle。

## AssetRegistry绑定阶段

1. T240由`IConfigProvider.GetAssetManifest()`暴露加载后只读清单；`AssetRegistryService`按Ordinal键精确核对Canonical `Assets/_Game/Config/Registry/AssetRegistry.asset`，全部通过后才发布只读对象索引。
2. Registry条目只保存`assetKey`和Unity对象引用；Prefab、Sprite、AudioClip和Scene分别做类型检查，Scene使用`AssetSceneReference`包装明确场景引用。SO、Inspector和代码不得保存玩法平衡值。
3. 运行时和Editor作者工具不读取AssetManifest的`addressOrPath`，不通过GUID或路径完成Prefab/Sprite/Audio绑定；资源替换只改Registry引用，稳定配置ID不变。
4. `One Stroke Demon/Config/Validate Asset Registry`检查78键覆盖、持久化资产、Prefab和启用场景；同一校验由`IPreprocessBuildWithReport`在构建前执行，缺失、重复、额外、空或错型键均阻断构建。
5. 当前正式资源未接入的键按类型共享受管占位Sprite、AudioClip和Prefab，`scene_battle`引用Battle场景；作者工具保留合法的既有引用，允许后续逐键替换。占位不代表表现验收完成。
6. Bootstrap先通过Runtime配置检查，再初始化Registry；两者的摘要都成功输出后才进入MainMenu，任一失败均不发布可用Registry或继续场景流。

## 配置改动完成定义

- [ ] 只修改 `Design/Config/GameConfig.xlsx`，模板镜像由受控同步步骤更新。
- [ ] FieldDictionary与Enums需要时已同步。
- [ ] Schema、导出器、DTO、校验、文档和测试按影响范围同步。
- [ ] JSON、hash和ConfigIds由`verify-config.sh --update`同源生成，无人工编辑痕迹。
- [ ] `verify-config.sh`默认只读门通过，受管三文件无字节漂移。
- [ ] Unity编译、ConfigPipeline分类EditMode/PlayMode和真实启动版本/hash通过。
- [ ] 没有在C#、Inspector或ScriptableObject新增同一平衡值。
- [ ] `git diff --check`、生成文件确定性比较和改动白名单通过。
