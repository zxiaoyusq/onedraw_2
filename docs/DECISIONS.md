# DECISIONS

## D-001 · Unity版本策略

- 状态：ACCEPTED
- 决定：采用现有工程已经固定的Unity `6000.5.1f1`，不得在未完成兼容性Spike和新决策前升级或降级。
- 理由：用户已用该版本初始化工程，`ProjectSettings/ProjectVersion.txt` 与本机Editor安装均可核验；T000不为追随原计划建议版本而迁移现有工程。
- 替代：若T110证明官方微信方案不兼容，比较更换补丁、切换已验证版本或最小embedded补丁；必须另写决策。

## D-002 · 配置唯一真相源

- 状态：ACCEPTED
- 决定：Excel为内容源，稳定JSON为构建快照，Runtime不读xlsx。
- 理由：可审查、可验证、适合Web和微信，避免Inspector双主库。

## D-003 · Unity对象引用

- 状态：ACCEPTED
- 决定：AssetRegistrySO只映射assetKey到Prefab、Sprite、Audio和VFX，不保存平衡数值。

## D-004 · 敌人架构

- 状态：ACCEPTED
- 决定：通用EnemyController、状态机和策略注册表，不为每个怪物建立空壳子类。

## D-005 · MVP平台能力

- 状态：ACCEPTED
- 决定：MVP只接存储、震动、生命周期和日志；广告、支付、登录、分享和排行榜不在范围内。

## D-006 · 横屏与参考坐标

- 状态：ACCEPTED
- 决定：横屏，1920×1080参考坐标；输入阈值按Safe Area缩放后的参考像素计算，不依赖Screen.dpi。

## D-007 · Unity工程目录

- 状态：ACCEPTED
- 决定：仓库根目录同时作为唯一Git根和Unity工程根，`Assets/`、`Packages/`、`ProjectSettings/` 不再放入 `game/` 子目录。
- 理由：当前目录已经是初始化完成的Unity 2D工程；避免移动资产产生额外GUID、路径和工具链风险。

## D-008 · T020 Unity包与渲染基线

- 状态：ACCEPTED
- 决定：Unity 6000.5.1f1使用URP 17.5.0、Input System 1.19.0、uGUI/TMP 2.5.0和Test Framework 1.7.0；Unity MCP固定commit `11836003a5e2ffcb7715ecec7e1fbb9d9cdb5bb8`。
- 决定：Graphics默认管线与Low/High质量档统一引用 `Assets/Settings/UniversalRP.asset`，其默认Renderer为 `Renderer2D.asset`。
- 理由：消除Graphics空管线与Git浮动依赖，确保Editor、测试和后续构建使用同一可复现基线。
- 细节：完整直接依赖、质量和输入测试入口见 `docs/PACKAGE_BASELINE.md`。

## D-009 · T110 微信转换 SDK 固定与 Unity 6000.5 补丁

- 状态：ACCEPTED
- 决定：采用微信官方 `wechat-miniprogram/minigame-tuanjie-transform-sdk` 的 `v0.1.33` 发布线，固定 commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228`，不使用浮动分支或已禁用的旧仓库。
- 决定：SDK由 Unity Package Manager embedded 到 `Packages/com.qq.weixin.minigame`。只允许 `WXRuntimeExtDef.cs` 在 `UNITY_6000_5_OR_NEWER` 使用 `GetEntityId` 的单点补丁；较早 Unity 保持上游实现。
- 理由：未修改上游在 Unity 6000.5.1f1 因 `GetInstanceID()` 的 CS0619 无法编译；替代 API 已通过 Unity 反射核验，补丁后全工程编译与回归通过。embedded 使补丁和完整上游快照可复现。
- 许可证：SDK根许可证为 MIT；随包保留 Brotli MIT-style 与 Binaryen 103.0.0 Apache-2.0 许可证。
- 移除条件：官方不可变版本修复该调用，并在 Unity 6000.5.1f1 通过 T110 同等编译与测试矩阵后，删除 embedded 包并恢复纯 Git 依赖。
- 限制：该决定不确认 G2转换、G3 DevTools、G4真机，也不授权迁移 Unity。

## D-010 · T120 可重复微信转换入口与Brotli策略

- 状态：ACCEPTED
- 决定：G2统一通过项目自有 `WechatBuildEntry` 与 `Tools/CI/build-wechat.sh` 调用固定SDK的 `WXConvertCore.DoExport`；输出限定在忽略目录 `Builds/WeChat/**`，Spike配置使用空AppID、横屏、256MB、关闭渲染线程和性能分析。
- 决定：macOS Unity `6000.5.1f1` 使用SDK公开的 `brotliMT=true` 路径。默认单线程路径因错误定位 `Unity.app/PlaybackEngines` 无法运行；这不是新增SDK源码差异。
- 决定：构建包装器在运行前备份、退出后恢复ProjectSettings、embedded SDK配置/元数据、URP构建期字段和SDK临时Assets，避免平台Spike污染可审查基线。
- 理由：第一次完整转换证明默认Brotli路径阻断G2；启用随SDK提供的压缩实现后，Builder、Converter、JSON、产物清单和 `.br` 均通过，并且仓库无平台设置残留。
- 限制：G2为 `PASS WITH KNOWN ISSUES`，不替代G3/G4；93条未匹配替换规则保持BUG-0006，只有实际DevTools和真机可以缩小风险。
- 移除条件：官方固定版本修复macOS路径并在同一Unity版本通过完整G2～G4后，可恢复默认压缩路径并删除兼容策略。

## D-011 · 平台阻塞期间优先推进主内容链

- 状态：ACCEPTED
- 决定：按用户明确指示，T120保持`BLOCKED`并保留现有G2证据，T130保持`BACKLOG`；暂不处理微信开发者工具、真机和打包问题，依赖T040且可独立执行的T200成为唯一`READY`任务。
- 理由：G3/G4需要本机之外的登录工具与设备条件，而P2配置系统到大部分玩法主链不依赖这些运行门；继续主内容可以产生有效进展，同时不伪造平台结论。
- 限制：这是执行顺序延期，不是范围裁剪。`MVP_SCOPE`中的微信四级验证仍必须完成，T120不得改为DONE，G3/G4不得改为PASS。
- 恢复条件：具备已登录微信开发者工具和至少一台可用手机后可恢复T120；无论设备何时到位，平台任务最迟在T640或T750开始前恢复并满足其依赖。

## D-012 · T200配置契约冻结

- 状态：ACCEPTED
- 决定：`Design/Config/GameConfig.xlsx` 为唯一内容源，`config/一笔镇妖_游戏配置表模板.xlsx` 只做字节一致的同步镜像；当前冻结 schema `1` / content `0.1.1-sample`。
- 决定：稳定ID匹配 `^[a-z][a-z0-9_]*$`；玩法权威ID采用 `lv_001_tutorial`、`lv_002_cave`、`lv_003_boss` 与 `boss_tomb_king`。文案键、奖励/教程分组键和资源键是独立命名空间，不跟随玩法ID自动改名。
- 决定：Schema `required` 表示JSON属性存在，FieldDictionary `required` 表示Excel单元格非空；可空字符串导出为空字符串，可空数值/布尔导出为null。Global四个值列组成由valueType判别且恰好一项非空的联合。
- 决定：普通外键使用 `Sheet.field`；分组外键校验目标组存在；`SpawnPoints.levelId="*"` 是唯一通配符；`Rewards.rewardId=conditional` 按UnlockLevel/UnlockFeature/ScoreToken分别校验关卡ID、`feature_`和`token_`命名空间。
- 决定：contentHash对排除自身后的完整配置对象计算SHA-256，采用递归Ordinal对象键序、固定数组排序、UTF-8无BOM和紧凑JSON；生成时间不进入内容。
- 理由：T210导出器和T220校验器需要无歧义的输入、空值、外键、排序及hash合同；先修正与GAME_DESIGN冲突的ID和字段字典必填错误，避免把初始样例缺陷固化到Runtime。
- 限制：T200的项目内审计脚本只用于冻结证据，不是T210导出器或T220生产校验器；本决定不授权Runtime读取xlsx或Inspector保存数值。

## D-013 · T210独立配置导出器与确定性写入

- 状态：ACCEPTED
- 决定：配置导出采用独立 `net8.0` 控制台工具 `Tools/ConfigExporter`；xlsx读取固定为 `DocumentFormat.OpenXml 3.5.1`，直接依赖使用精确版本约束并提交NuGet锁文件。Open XML及测试依赖仅存在于Tools，不进入Unity Runtime。
- 决定：输出顺序和`contentHash`严格执行D-012；所有表显式稳定排序，FieldDictionary按固定Sheet和对应表头序，重复排序键以整行字段作Ordinal/数值兜底，禁止依赖Excel行号、当前区域设置或字典枚举顺序。
- 决定：`export`先在目标同目录写入`.tmp`并强制落盘，重新读取验证顶层顺序、版本、记录数和hash后才原子替换旧文件；`validate`执行同一读取、建模、序列化与内存自检但不写输出。
- 理由：相同工作簿在不同机器和重复运行中必须得到字节一致快照，且导出器异常或自检失败不能破坏最后一份有效JSON。
- 许可证：运行时工具依赖均为MIT；测试依赖为MIT或Apache-2.0，完整版本和上游记录见 `Tools/ConfigExporter/THIRD_PARTY_NOTICES.md`。
- 限制：T210只证明可导出性、契约对齐与确定性，不执行T220的必填、范围、枚举、唯一性、外键或跨表生产校验，不创建T230/T250负责的Unity Runtime资产。

## D-014 · T220生产校验与整包拒绝

- 状态：ACCEPTED
- 决定：`ConfigValidator`在完整工作簿建模后、序列化和原子写入前执行；任一错误以稳定`CFG`错误码和Sheet/Excel行/字段定位并拒绝整包，不半应用、不静默修正。
- 决定：生产合同覆盖必填、类型/范围、稳定ID、主键/组合键、枚举、普通/分组/通配符/conditional外键、连续order、Global联合、Level→Wave→Spawn、星级和Boss全覆盖语义。提供Schema时，FieldDictionary/Enums的类型、可空性、min/max和枚举集合还必须与Schema精确一致。
- 决定：`MovePatternType`与`AttackTriggerType`属于代码拥有的算法合同，当前由导出器登记精确集合；T430实现Runtime策略注册表时必须复用或同步该合同，不能把策略选择变成第二套玩法数值库。
- 测试策略：坏配置样例采用可审查JSON变更清单，只修改正式工作簿读取后的内存副本；37类反例逐一断言错误码、Sheet、Excel数据行和字段，正式xlsx及其镜像、Schema、样例JSON保持只读。
- 限制：T220不生成Unity Runtime快照、DTO、加载服务或AssetRegistry；这些仍分别属于T230、T240和T250。
