# TASKS：一笔镇妖原子任务清单

> 状态仅允许 `BACKLOG / READY / IN_PROGRESS / REVIEW / DONE / BLOCKED`。原则上任何时刻只能有一个任务为 `IN_PROGRESS`。

## 总览

| ID | 阶段 | 状态 | 依赖 | 估算人日 | 目标 |
|---|---|---|---|---:|---|
| T000 | P0 合同与Harness | DONE | — | 0.5 | 统一玩法、MVP范围、技术基线、配置唯一真相源和完成定义。 |
| T010 | P0 合同与Harness | DONE | T000 | 1.0 | 验收并纳管仓库根目录现有Unity 6000.5.1f1 2D工程基线。 |
| T020 | P0 合同与Harness | DONE | T010 | 1.0 | 固定URP 2D、Input System、TMP、Test Framework和质量档。 |
| T030 | P0 合同与Harness | DONE | T020 | 1.0 | 建立目录、asmdef和Bootstrap/MainMenu/Battle三场景骨架。 |
| T040 | P0 合同与Harness | DONE | T030 | 1.0 | 建立构建、测试、证据和一任务一提交工作流。 |
| T100 | P1 微信平台Spike | DONE | T040 | 1.0 | 先验证标准Unity Web构建，不接微信转换。 |
| T110 | P1 微信平台Spike | DONE | T100 | 2.0 | 确认当前官方微信Unity转换方案并做Unity/SDK兼容矩阵。 |
| T120 | P1 微信平台Spike | BLOCKED | T110 | 2.0 | 完成微信转换、开发者工具和至少一台真机的分级冒烟。 |
| T130 | P1 微信平台Spike | BACKLOG | T110 | 1.0 | 建立Editor/Web/WeChat平台服务抽象。 |
| T200 | P2 配置系统 | DONE | T040 | 1.0 | 确认Excel工作簿、字段字典、ID规则和数据所有权。 |
| T210 | P2 配置系统 | DONE | T200 | 2.0 | 实现独立.NET配置导出器：xlsx到稳定JSON。 |
| T220 | P2 配置系统 | DONE | T210 | 2.0 | 实现结构、范围、枚举、唯一性、外键和跨表语义校验。 |
| T230 | P2 配置系统 | DONE | T220 | 2.0 | 实现Unity Runtime配置加载、版本检查和只读索引。 |
| T240 | P2 配置系统 | DONE | T230 | 1.0 | 建立assetKey到Unity对象的AssetRegistry，且不保存平衡值。 |
| T250 | P2 配置系统 | DONE | T240 | 1.0 | 把导出、校验、JSON diff和Unity配置测试接入一条命令。 |
| T300 | P3 手势战斗核心 | DONE | T250, T030 | 1.0 | 实现统一指针输入、UI阻挡、Safe Area和参考像素坐标。 |
| T310 | P3 手势战斗核心 | DONE | T300 | 1.0 | 实现笔迹采样、最小距离、最大点数和长度裁剪。 |
| T320 | P3 手势战斗核心 | DONE | T310 | 2.0 | 实现RDP简化、重采样、长度、包围盒、面积、闭合和曲率。 |
| T330 | P3 手势战斗核心 | DONE | T320 | 2.0 | 配置驱动识别横、竖、斜、弧、圆和蓄力笔势。 |
| T340 | P3 手势战斗核心 | DONE | T310 | 1.5 | 实现低分配笔迹视觉、淡出和池化。 |
| T350 | P3 手势战斗核心 | DONE | T320, T340 | 2.0 | 实现分段胶囊命中、顺序排序、同笔去重和弱点命中。 |
| T360 | P3 手势战斗核心 | DONE | T350, T230 | 1.5 | 实现伤害公式、方向奖励、连斩、评分和能量。 |
| T370 | P3 手势战斗核心 | DONE | T350 | 1.5 | 实现可切断、不可切断和可反弹的敌方投射物。 |
| T400 | P4 玩家敌人技能 | DONE | T360 | 1.5 | 实现玩家HP、能量、刀/符架势、切换冷却和战斗事件。 |
| T410 | P4 玩家敌人技能 | DONE | T400, T230 | 3.0 | 实现数据驱动Skill到EffectGroup到有序Effect执行链。 |
| T420 | P4 玩家敌人技能 | DONE | T360 | 2.0 | 实现通用敌人状态机、Damageable和Weakpoint。 |
| T430 | P4 玩家敌人技能 | DONE | T420, T370 | 3.0 | 实现可组合移动、攻击、防御和支援策略注册表。 |
| T440 | P4 玩家敌人技能 | DONE | T420 | 1.5 | 建立敌人、投射物、VFX和伤害数字对象池及完整重置。 |
| T450 | P4 玩家敌人技能 | DONE | T430, T440 | 2.0 | 只用配置组合5种普通怪和1种精英怪。 |
| T460 | P4 玩家敌人技能 | DONE | T410, T430 | 3.0 | 实现配置驱动Boss阶段、阈值、技能序列和切换。 |
| T500 | P5 关卡完整单局 | DONE | T450 | 2.0 | 实现Level/Wave/Spawn时间轴和条件结束。 |
| T510 | P5 关卡完整单局 | DONE | T500, T400 | 1.5 | 实现Countdown/Playing/UltimateDrawing/Paused/Victory/Defeat状态机。 |
| T520 | P5 关卡完整单局 | DONE | T510 | 2.0 | 完成幽菌古道教学关：普通斩、连斩、切弹、架势和终极。 |
| T530 | P5 关卡完整单局 | DONE | T520 | 2.0 | 完成混合怪物普通关，验证战术组合和难度曲线。 |
| T540 | P5 关卡完整单局 | DONE | T460, T530 | 2.5 | 完成Boss关和镇墓玄甲王三阶段战斗。 |
| T550 | P5 关卡完整单局 | DONE | T540 | 2.0 | 实现结算、星级/评分、解锁、重开和最小进度保存。 |
| T600 | P6 表现与资源 | DONE | T510 | 2.0 | 实现生命、能量、连斩、评分、架势、终极、暂停和结算UI。 |
| T610 | P6 表现与资源 | DONE | T600 | 1.0 | 建立中文TMP字体、fallback和字符覆盖检查。 |
| T620 | P6 表现与资源 | DONE | T360, T440 | 2.0 | 实现受击停顿、闪白、震屏、伤害数字、音效、震动和慢动作。 |
| T630 | P6 表现与资源 | DONE | T450, T600 | 2.0 | 接入PSD解析出的背景、主角、怪物、UI和特效作为原型资源。 |
| T640 | P6 表现与资源 | BACKLOG | T600, T120 | 1.5 | 适配横屏比例、刘海/圆角、安全区和触控遮挡。 |
| T650 | P6 表现与资源 | DONE | T520, T600 | 1.5 | 完成事件驱动教程遮罩、手势示意和跳过/回看。 |
| T660 | P6 表现与资源 | DONE | T540, T550, T630, T650 | 3.0 | 建立生产可玩入口与Battle组合根。 |
| T670 | P6 代码可读性 | DONE | T660 | 0.5 | 为Core与Platform脚本补齐中文注释。 |
| T671 | P6 代码可读性 | DONE | T670 | 1.0 | 为Config手写运行时脚本补齐中文注释。 |
| T672 | P6 代码可读性 | DONE | T671 | 1.0 | 为Input脚本补齐中文注释。 |
| T673 | P6 代码可读性 | DONE | T672 | 1.0 | 为Combat脚本补齐中文注释。 |
| T674 | P6 代码可读性 | DONE | T673 | 1.5 | 为Actors脚本补齐中文注释。 |
| T675 | P6 代码可读性 | DONE | T674 | 1.0 | 为Skills脚本补齐中文注释。 |
| T676 | P6 代码可读性 | DONE | T675 | 1.5 | 为Levels脚本补齐中文注释。 |
| T677 | P6 代码可读性 | DONE | T676 | 1.5 | 为Presentation脚本补齐中文注释。 |
| T678 | P6 代码可读性 | DONE | T677 | 1.0 | 为Bootstrap脚本补齐中文注释。 |
| T679 | P6 代码可读性 | DONE | T678 | 1.0 | 为Editor脚本补齐中文注释。 |
| T680 | P6 代码可读性 | DONE | T679 | 0.5 | 通过导出器为ConfigIds生成中文注释。 |
| T681 | P6 代码可读性 | DONE | T680 | 0.5 | 审计Scripts全量中文注释覆盖。 |
| T690 | P6 表现与资源 | DONE | T630, T660 | 0.5 | 将火鱼静态原型替换为用户提供的九帧循环动画Prefab。 |
| T691 | P6 工程卫生 | DONE | T690 | 0.1 | 仅放开Assets目录下Unity必需的meta跟踪。 |
| T692 | P6 表现与资源 | DONE | T690 | 0.2 | 修复Lit角色在Actors层缺少2D全局光而显示全黑。 |
| T693 | P6 工程修复 | DONE | T660 | 0.2 | 固定Android包只允许横屏方向。 |
| T694 | P6 表现与资源 | DONE | T630, T660, T692 | 0.8 | 将主角静态原型替换为用户提供的待机与攻击动画Prefab。 |
| T695 | P6 表现与资源 | DONE | T440, T620, T630, T660, T692 | 0.8 | 将用户提供的十一帧爆炸动画接入怪物死亡特效。 |
| T696 | P6 工程治理 | DONE | T695 | 0.1 | 明确增量、按风险升级的快速收尾合同。 |
| T697 | P6 工程修复 | DONE | T695 | 0.2 | 修复生产参考空间中怪物死亡特效被错误缩小的问题。 |
| T698 | P6 表现与资源 | DONE | T340, T630, T660 | 1.0 | 实现方案C青白闪电画笔特效并打通配置、Prefab、池化渲染和生产入口。 |
| T699 | P6 工程修复 | DONE | T420, T500, T660 | 0.5 | 修复生产战斗接触伤害与右侧出生推进。 |
| T699A | P6 工程修复 | DONE | T370, T440, T660, T699 | 0.8 | 接入生产投射物命中、可见移动与画笔击落。 |
| T700 | P7 质量发布 | READY | T540, T660, T681, T699A | 2.0 | 补齐纯规则EditMode回归矩阵。 |
| T710 | P7 质量发布 | BACKLOG | T550, T650 | 3.0 | 补齐Unity集成、完整单局、暂停、重开和生命周期PlayMode测试。 |
| T720 | P7 质量发布 | BACKLOG | T710, T250 | 1.0 | 审计所有玩法数值、内容和文案是否来自配置表。 |
| T730 | P7 质量发布 | BACKLOG | T710, T630 | 3.0 | 在目标低端机收敛CPU、GC、内存、DrawCall、纹理和包体。 |
| T740 | P7 质量发布 | BACKLOG | T730 | 2.0 | 自动化配置验证、Unity测试、Web构建和证据归档。 |
| T750 | P7 质量发布 | BACKLOG | T740, T120 | 2.0 | 生成微信小游戏发布候选并完成四级平台验收。 |
| T760 | P7 质量发布 | BACKLOG | T750 | 1.0 | 完成发布资料、版本冻结、回滚方案和最终证据索引。 |

## P0 合同与Harness

### T000 · 统一玩法、MVP范围、技术基线、配置唯一真相源和完成定义。

- **状态：** `DONE`
- **依赖：** 无
- **估算：** 0.5 人日
- **产出：** 处理文档中的待确认项；更新project-index；锁定下一任务。
- **明确不做：** 不写业务C#；不导入正式美术；不安装微信SDK。
- **验收：** 权威文档之间无冲突；所有TBD被确认或明确BLOCKED；T010改为READY。
- **验证：** 文档交叉检查；git status基线。
- **证据：** `artifacts/evals/T000/`
- **提交：** `T000: <imperative summary>`

### T010 · 验收并纳管仓库根目录现有Unity 6000.5.1f1 2D工程基线。

- **状态：** `DONE`
- **依赖：** T000
- **估算：** 1.0 人日
- **产出：** 根目录Unity工程；根.gitignore；确认精确ProjectVersion.txt；空场景。
- **明确不做：** 不接微信SDK；不写战斗代码；不创建嵌套.git。
- **验收：** Unity 6000.5.1f1可打开并进入Play Mode；Console无Error；仓库只有一个.git。
- **验证：** Unity空场景冒烟；git status审查。
- **证据：** `artifacts/evals/T010/`
- **提交：** `T010: <imperative summary>`

### T020 · 固定URP 2D、Input System、TMP、Test Framework和质量档。

- **状态：** `DONE`
- **依赖：** T010
- **估算：** 1.0 人日
- **产出：** URP Asset与2D Renderer；Input Actions；Low/High质量档；包版本清单。
- **明确不做：** 不引入非必要框架；不实现玩法。
- **验收：** Graphics及Quality均指向正确URP；鼠标/触摸统一输入可读；测试程序集可发现。
- **验证：** RenderPipelineBaselineTests；InputBaselinePlayModeTests。
- **证据：** `artifacts/evals/T020/`
- **提交：** `T020: <imperative summary>`

### T030 · 建立目录、asmdef和Bootstrap/MainMenu/Battle三场景骨架。

- **状态：** `DONE`
- **依赖：** T020
- **估算：** 1.0 人日
- **产出：** 模块目录；Runtime/Editor/EditMode/PlayMode asmdef；Bootstrap场景；场景流接口。
- **明确不做：** 不实现具体战斗；不手改Unity YAML。
- **验收：** 无程序集循环依赖；Bootstrap能加载主菜单和战斗灰盒；场景由Unity保存。
- **验证：** AssemblyDependencyTests；SceneFlowSmokePlayModeTests。
- **证据：** `artifacts/evals/T030/`
- **提交：** `T030: <imperative summary>`

### T040 · 建立构建、测试、证据和一任务一提交工作流。

- **状态：** `DONE`
- **依赖：** T030
- **估算：** 1.0 人日
- **产出：** 批处理测试命令；Web构建入口；verification模板；改动白名单流程。
- **明确不做：** 不宣称微信真机通过。
- **验收：** EditMode/PlayMode可独立输出XML；失败返回非零；证据模板可复用。
- **验证：** 空测试流水线冒烟。
- **证据：** `artifacts/evals/T040/`
- **提交：** `T040: <imperative summary>`


## P1 微信平台Spike

### T100 · 先验证标准Unity Web构建，不接微信转换。

- **状态：** `DONE`
- **依赖：** T040
- **估算：** 1.0 人日
- **产出：** 最小Web构建；输入/音频/中文/存储冒烟；构建日志与体积。
- **明确不做：** 不接业务内容；不把浏览器成功等同微信成功。
- **验收：** 标准Web构建完成；本地HTTP可运行；核心冒烟有记录。
- **验证：** WebBuildSmoke；浏览器人工冒烟。
- **证据：** `artifacts/evals/T100/`
- **提交：** `T100: <imperative summary>`

### T110 · 确认当前官方微信Unity转换方案并做Unity/SDK兼容矩阵。

- **状态：** `DONE`
- **依赖：** T100
- **估算：** 2.0 人日
- **产出：** SDK来源/版本/commit/许可证；兼容矩阵；UPSTREAM.md；补丁决策。
- **明确不做：** 不使用浮动分支；不把旧失效仓库当安装源；未经决策不换Unity版本。
- **验收：** 依赖可复现；导入后全工程编译；失败保留原始错误并正确标记。
- **验证：** SDKImportCompile；版本与许可证审查。
- **证据：** `artifacts/evals/T110/`
- **提交：** `T110: <imperative summary>`

### T120 · 完成微信转换、开发者工具和至少一台真机的分级冒烟。

- **状态：** `BLOCKED`
- **依赖：** T110
- **估算：** 2.0 人日
- **产出：** 转换输出；DevTools日志/截图；真机触摸/音频/前后台/存储结果。
- **明确不做：** 缺工具或真机时不得写PASS。
- **验收：** 四级结果独立记录：Web/转换/DevTools/真机；失败可定位到层级。
- **验证：** 人工平台矩阵。
- **证据：** `artifacts/evals/T120/`
- **提交：** `T120: <imperative summary>`

### T130 · 建立Editor/Web/WeChat平台服务抽象。

- **状态：** `BACKLOG`
- **依赖：** T110
- **估算：** 1.0 人日
- **产出：** IPlatformService；存储/震动/生命周期/日志接口；Editor/Web Stub；微信实现壳。
- **明确不做：** 不接广告支付排行榜；Gameplay不直接调用WX静态API。
- **验收：** Gameplay程序集不依赖微信SDK；无SDK时Editor仍可运行；平台实现可替换。
- **验证：** PlatformServiceContractTests。
- **证据：** `artifacts/evals/T130/`
- **提交：** `T130: <imperative summary>`


## P2 配置系统

### T200 · 确认Excel工作簿、字段字典、ID规则和数据所有权。

- **状态：** `DONE`
- **依赖：** T040
- **估算：** 1.0 人日
- **产出：** GameConfig.xlsx；CONFIG_SCHEMA；枚举/字段字典；样例数据。
- **明确不做：** 不让Inspector成为第二数值库；运行时不读xlsx。
- **验收：** 玩家/手势/敌人/技能/关卡/Boss/文本/VFX均有表；外键关系明确。
- **验证：** Schema人工审查。
- **证据：** `artifacts/evals/T200/`
- **提交：** `T200: <imperative summary>`

### T210 · 实现独立.NET配置导出器：xlsx到稳定JSON。

- **状态：** `DONE`
- **依赖：** T200
- **估算：** 2.0 人日
- **产出：** Tools/ConfigExporter；export/validate命令；稳定排序和hash；原子写入。
- **明确不做：** Excel库不进入Unity Runtime；输出不含无意义随机时间戳。
- **验收：** 同一输入重复导出字节一致；错误返回非零；输出自校验后替换。
- **验证：** ExporterDeterminismTests；ExporterHeaderTests。
- **证据：** `artifacts/evals/T210/`
- **提交：** `T210: <imperative summary>`

### T220 · 实现结构、范围、枚举、唯一性、外键和跨表语义校验。

- **状态：** `DONE`
- **依赖：** T210
- **估算：** 2.0 人日
- **产出：** ConfigValidator；Sheet/Row/Field定位；错误码；坏配置样例。
- **明确不做：** 不允许半应用坏配置；不静默修正策划数据。
- **验收：** 重复ID/缺外键/负时间/Boss阈值乱序等全部失败；错误可定位单元格。
- **验证：** ConfigValidationTests。
- **证据：** `artifacts/evals/T220/`
- **提交：** `T220: <imperative summary>`

### T230 · 实现Unity Runtime配置加载、版本检查和只读索引。

- **状态：** `DONE`
- **依赖：** T220
- **估算：** 2.0 人日
- **产出：** GameplayConfigDocument DTO；GameplayConfigService；ID索引；启动摘要。
- **明确不做：** 热路径不反序列化；不使用反射驱动战斗。
- **验收：** 启动一次解析并建立只读字典；版本不兼容阻止进战斗；日志有来源/hash/数量。
- **验证：** RuntimeConfigLoadTests；InvalidConfigTests。
- **证据：** `artifacts/evals/T230/`
- **提交：** `T230: <imperative summary>`

### T240 · 建立assetKey到Unity对象的AssetRegistry，且不保存平衡值。

- **状态：** `DONE`
- **依赖：** T230
- **估算：** 1.0 人日
- **产出：** AssetRegistrySO；Prefab/Sprite/Audio/VFX映射；Editor校验器。
- **明确不做：** SO不复制HP/CD/伤害；配置不写GUID或路径。
- **验收：** 每个assetKey可校验；缺失/重复键构建失败；资源替换不改配置ID。
- **验证：** AssetRegistryValidationTests。
- **证据：** `artifacts/evals/T240/`
- **提交：** `T240: <imperative summary>`

### T250 · 把导出、校验、JSON diff和Unity配置测试接入一条命令。

- **状态：** `DONE`
- **依赖：** T240
- **估算：** 1.0 人日
- **产出：** verify-config脚本；生成JSON快照；CI说明。
- **明确不做：** 不允许只改Excel不提交JSON；不允许手改生成JSON。
- **验收：** 一键导出验证；生成物漂移可检测；全绿后才进入玩法任务。
- **验证：** ConfigPipelineE2E。
- **证据：** `artifacts/evals/T250/`
- **提交：** `T250: <imperative summary>`


## P3 手势战斗核心

### T300 · 实现统一指针输入、UI阻挡、Safe Area和参考像素坐标。

- **状态：** `DONE`
- **依赖：** T250, T030
- **估算：** 1.0 人日
- **产出：** IPointerInput；InputSystemPointerAdapter；ReferencePixelConverter；失焦取消。
- **明确不做：** 不依赖Screen.dpi；MVP不支持多指战斗。
- **验收：** 鼠标与触摸同接口；UI上起笔不攻击；前后台会取消当前笔迹。
- **验证：** PointerInputTests；PointerCancelPlayModeTests。
- **证据：** `artifacts/evals/T300/`
- **提交：** `T300: <imperative summary>`

### T310 · 实现笔迹采样、最小距离、最大点数和长度裁剪。

- **状态：** `DONE`
- **依赖：** T300
- **估算：** 1.0 人日
- **产出：** StrokeSampler纯C#；不可变StrokeData；配置阈值。
- **明确不做：** 不为每点创建GameObject；Update热路径不分配列表。
- **验收：** 短抖动过滤；超长笔迹精确截断；点数上限稳定。
- **验证：** StrokeSamplerBoundaryTests。
- **证据：** `artifacts/evals/T310/`
- **提交：** `T310: <imperative summary>`

### T320 · 实现RDP简化、重采样、长度、包围盒、面积、闭合和曲率。

- **状态：** `DONE`
- **依赖：** T310
- **估算：** 2.0 人日
- **产出：** StrokeGeometry纯C#；容差说明；退化输入处理。
- **明确不做：** 算法不藏在MonoBehaviour。
- **验收：** 直线/折线/弧/圆稳定；重复点和极短输入不崩溃；结果可回放。
- **验证：** StrokeGeometryTests。
- **证据：** `artifacts/evals/T320/`
- **提交：** `T320: <imperative summary>`

### T330 · 配置驱动识别横、竖、斜、弧、圆和蓄力笔势。

- **状态：** `DONE`
- **依赖：** T320
- **估算：** 2.0 人日
- **产出：** GestureClassifier；GestureMatchResult及置信度；规则表映射。
- **明确不做：** MVP不做机器学习；不要求像素级书法精度。
- **验收：** 角度/闭合/曲率均来自表；输入相同结果相同；误识别样例有回归。
- **验证：** GestureClassifierTests。
- **证据：** `artifacts/evals/T330/`
- **提交：** `T330: <imperative summary>`

### T340 · 实现低分配笔迹视觉、淡出和池化。

- **状态：** `DONE`
- **依赖：** T310
- **估算：** 1.5 人日
- **产出：** StrokeTrailView；LineRenderer或程序化Mesh；轨迹对象池。
- **明确不做：** 视觉组件不决定命中；不为每段实例化材质。
- **验收：** 快速连续划动无明显GC尖峰；刀/符轨迹可配置切换。
- **验证：** StrokeTrailPoolPlayModeTests。
- **证据：** `artifacts/evals/T340/`
- **提交：** `T340: <imperative summary>`

### T350 · 实现分段胶囊命中、顺序排序、同笔去重和弱点命中。

- **状态：** `DONE`
- **依赖：** T320, T340
- **估算：** 2.0 人日
- **产出：** StrokeHitResolver；IHittable；HitRecord；NonAlloc查询缓存。
- **明确不做：** 不动态生成大量Collider；默认同一目标同笔只伤一次。
- **验收：** 一笔多目标顺序正确；同目标不重复伤害；弱点与主体可区分。
- **验证：** StrokeHitResolverTests；MultiTargetHitPlayModeTests。
- **证据：** `artifacts/evals/T350/`
- **提交：** `T350: <imperative summary>`

### T360 · 实现伤害公式、方向奖励、连斩、评分和能量。

- **状态：** `DONE`
- **依赖：** T350, T230
- **估算：** 1.5 人日
- **产出：** DamageCalculator纯C#；ComboService；ScoreService；DamageContext。
- **明确不做：** 不在敌人脚本散落公式；不硬编码倍率。
- **验收：** 所有倍率来自表；弱点/方向/连斩可独立断言；浮点取整规则明确。
- **验证：** DamageFormulaTests；ComboScoreTests。
- **证据：** `artifacts/evals/T360/`
- **提交：** `T360: <imperative summary>`

### T370 · 实现可切断、不可切断和可反弹的敌方投射物。

- **状态：** `DONE`
- **依赖：** T350
- **估算：** 1.5 人日
- **产出：** ProjectileController；ProjectileHitTarget；切断/反弹规则。
- **明确不做：** 不依赖不可控物理力。
- **验收：** 三类规则均可配；反弹归属和伤害来源正确；回收状态完整。
- **验证：** ProjectileCutTests；ProjectileReflectPlayModeTests。
- **证据：** `artifacts/evals/T370/`
- **提交：** `T370: <imperative summary>`


## P4 玩家敌人技能

### T400 · 实现玩家HP、能量、刀/符架势、切换冷却和战斗事件。

- **状态：** `DONE`
- **依赖：** T360
- **估算：** 1.5 人日
- **产出：** PlayerCombatModel；PlayerCombatController；StanceService。
- **明确不做：** MVP不做自由移动；数值不放Inspector。
- **验收：** 架势影响轨迹/伤害/切弹；能量获取消耗可配；死亡只触发一次。
- **验证：** PlayerCombatTests；StanceSwitchPlayModeTests。
- **证据：** `artifacts/evals/T400/`
- **提交：** `T400: <imperative summary>`

### T410 · 实现数据驱动Skill到EffectGroup到有序Effect执行链。

- **状态：** `DONE`
- **依赖：** T400, T230
- **估算：** 3.0 人日
- **产出：** SkillService；IEffectExecutor；伤害/治疗/Buff/击退/清弹/慢动作等。
- **明确不做：** 不为每个技能写独立MonoBehaviour；不反射查执行器。
- **验收：** 新增由现有效果组成的技能只改表；顺序稳定；目标过滤可配。
- **验证：** SkillEffectPipelineTests；UltimateSkillPlayModeTests。
- **证据：** `artifacts/evals/T410/`
- **提交：** `T410: <imperative summary>`

### T420 · 实现通用敌人状态机、Damageable和Weakpoint。

- **状态：** `DONE`
- **依赖：** T360
- **估算：** 2.0 人日
- **产出：** EnemyController；EnemyStateMachine；Damageable；WeakpointController。
- **明确不做：** 不创建每怪空壳子类。
- **验收：** Spawn/Move/Windup/Attack/Recovery/Stun/Dead清晰；死亡/打断/回收幂等。
- **验证：** EnemyStateMachineTests。
- **证据：** `artifacts/evals/T420/`
- **提交：** `T420: <imperative summary>`

### T430 · 实现可组合移动、攻击、防御和支援策略注册表。

- **状态：** `DONE`
- **依赖：** T420, T370
- **估算：** 3.0 人日
- **产出：** MovementStrategyRegistry；AttackStrategyRegistry；DefenseRuleService；Telegraph。
- **明确不做：** 不引入NavMesh/A*；不把动画事件当唯一伤害真相。
- **验收：** 靠近/悬浮/俯冲/冲撞/投射物/护盾可复用；未知策略ID校验失败。
- **验证：** EnemyStrategyTests；AttackTelegraphPlayModeTests。
- **证据：** `artifacts/evals/T430/`
- **提交：** `T430: implement configured enemy strategies`

### T440 · 建立敌人、投射物、VFX和伤害数字对象池及完整重置。

- **状态：** `DONE`
- **依赖：** T420
- **估算：** 1.5 人日
- **产出：** ObjectPoolService；IPoolable；预热配置；泄漏检测。
- **明确不做：** 不只重置activeSelf；不让事件订阅跨回收残留。
- **验收：** 连续生成/击杀/清场/重开3次无旧状态；池不足策略可配。
- **验证：** PoolResetTests；RestartThreeTimesPlayModeTests。
- **证据：** `artifacts/evals/T440/`
- **提交：** `T440: implement configured object pools`

### T450 · 只用配置组合5种普通怪和1种精英怪。

- **状态：** `DONE`
- **依赖：** T430, T440
- **估算：** 2.0 人日
- **产出：** 符火鱼妖；轮车僵妖；石甲龟妖；骷髅幽魂；飞行符蝠；摄魂道傀。
- **明确不做：** 不为内容复制业务代码；不先追求正式动画。
- **验收：** 每怪有独立教学点与清晰前摇；改HP/速度/攻击无需改C#。
- **验证：** EnemyArchetypeConfigTests；EnemyGalleryPlayModeTests。
- **证据：** `artifacts/evals/T450/`
- **提交：** `T450: assemble configured enemy archetypes`

### T460 · 实现配置驱动Boss阶段、阈值、技能序列和切换。

- **状态：** `DONE`
- **依赖：** T410, T430
- **估算：** 3.0 人日
- **产出：** BossPhaseController；阶段条件/进入动作；镇墓玄甲王三阶段。
- **明确不做：** 不在Boss脚本硬编码百分比；切换不依赖动画长度猜测。
- **验收：** 阈值顺序校验；阶段可换攻击/速度/护甲/弱点；事件只触发一次。
- **验证：** BossPhaseTests；BossBattlePlayModeTests。
- **证据：** `artifacts/evals/T460/`
- **提交：** `T460: implement configured boss phases`


## P5 关卡完整单局

### T500 · 实现Level/Wave/Spawn时间轴和条件结束。

- **状态：** `DONE`
- **依赖：** T450
- **估算：** 2.0 人日
- **产出：** LevelRunner；WaveRunner；SpawnScheduler；归一化出生区域。
- **明确不做：** 不在场景手摆每波敌人；不让计时器跨过玩家必须完成的动作门。
- **验收：** 波次和刷怪完全来自表；AllDefeated/TimeElapsed/BossDefeated可用；暂停不推进。
- **验证：** SpawnTimelineTests；WaveRunnerPlayModeTests。
- **证据：** `artifacts/evals/T500/`
- **提交：** `T500: implement configured level timelines`

### T510 · 实现Countdown/Playing/UltimateDrawing/Paused/Victory/Defeat状态机。

- **状态：** `DONE`
- **依赖：** T500, T400
- **估算：** 1.5 人日
- **产出：** BattleFlowStateMachine；事件门；统一时间源。
- **明确不做：** 不以计时器替代玩家确认；不允许重复结算。
- **验收：** 终极必须收到有效笔势事件；暂停/失焦恢复一致；胜负互斥。
- **验证：** BattleFlowTests；NoAdvanceBeforePlayerActionTests。
- **证据：** `artifacts/evals/T510/`
- **提交：** `T510: implement deterministic battle flow`

### T520 · 完成幽菌古道教学关：普通斩、连斩、切弹、架势和终极。

- **状态：** `DONE`
- **依赖：** T510
- **估算：** 2.0 人日
- **产出：** lv_001_tutorial配置；约6波；教程步骤。
- **明确不做：** 不做复杂剧情；不强制精确书法。
- **验收：** 新玩家约3分钟理解核心；教程由事件触发而非固定秒数。
- **验证：** TutorialLevelE2EPlayModeTests；人工无提示试玩。
- **证据：** `artifacts/evals/T520/`
- **提交：** `T520: complete event-driven tutorial level`

### T530 · 完成混合怪物普通关，验证战术组合和难度曲线。

- **状态：** `DONE`
- **依赖：** T520
- **估算：** 2.0 人日
- **产出：** lv_002_cave配置；约8波；精英与危险目标组合。
- **明确不做：** 不新增代码型敌人。
- **验收：** 节奏只改表即可；核心机制都有发挥；不存在不可解组合。
- **验证：** NormalLevelE2EPlayModeTests。
- **证据：** `artifacts/evals/T530/`
- **提交：** `T530: complete configured mixed-enemy level`

### T540 · 完成Boss关和镇墓玄甲王三阶段战斗。

- **状态：** `DONE`
- **依赖：** T460, T530
- **估算：** 2.5 人日
- **产出：** lv_003_boss配置；阶段与处决流程；胜败回路。
- **明确不做：** 不加入第二Boss；不依赖正式过场。
- **验收：** 约4分钟可完成；阶段提示清晰；失败重试和胜利均稳定。
- **验证：** BossLevelE2EPlayModeTests。
- **证据：** `artifacts/evals/T540/`
- **提交：** `T540: complete configured boss level`

### T550 · 实现结算、星级/评分、解锁、重开和最小进度保存。

- **状态：** `DONE`
- **依赖：** T540
- **估算：** 2.0 人日
- **产出：** ResultService；ProgressSave v1；Restart/NextLevel；迁移接口。
- **明确不做：** 不做云存档；不做付费货币。
- **验收：** 重复结算幂等；坏存档可回退；连续重开3次无泄漏。
- **验证：** ResultTests；SaveMigrationTests；RestartThreeTimesPlayModeTests。
- **证据：** `artifacts/evals/T550/`
- **提交：** `T550: complete result and progress loop`


## P6 表现与资源

### T600 · 实现生命、能量、连斩、评分、架势、终极、暂停和结算UI。

- **状态：** `DONE`
- **依赖：** T510
- **估算：** 2.0 人日
- **产出：** BattleHUD；UI Presenter；按钮状态/冷却；无业务逻辑View。
- **明确不做：** UI不直接改战斗Model；UI文字不硬编码。
- **验收：** UI只订阅状态/事件；按钮状态完整；Safe Area内可用。
- **验证：** HudBindingPlayModeTests。
- **证据：** `artifacts/evals/T600/`
- **提交：** `T600: implement battle HUD and result UI`

### T610 · 建立中文TMP字体、fallback和字符覆盖检查。

- **状态：** `DONE`
- **依赖：** T600
- **估算：** 1.0 人日
- **产出：** 中文字体Asset；fallback设置；常用字符集清单。
- **明确不做：** 不打包不必要超大Atlas；不在交付包外发字体源文件。
- **验收：** 配置文本无方框/缺字/裁切；动态伤害数字正常。
- **验证：** LocalizationGlyphTests；PlayMode截图。
- **证据：** `artifacts/evals/T610/`
- **提交：** `T610: add Chinese TMP fallback coverage`

### T620 · 实现受击停顿、闪白、震屏、伤害数字、音效、震动和慢动作。

- **状态：** `DONE`
- **依赖：** T360, T440
- **估算：** 2.0 人日
- **产出：** CombatFeedbackService；VFX/Audio cue映射；反馈强度设置。
- **明确不做：** 反馈不改变伤害真相；不阻塞加载音频。
- **验收：** 不看Console也能理解命中/弱点/破甲/弹反；对象池化；震动可关闭。
- **验证：** FeedbackEventTests；人工感知验收。
- **证据：** `artifacts/evals/T620/`
- **提交：** `T620: <imperative summary>`

### T630 · 接入PSD解析出的背景、主角、怪物、UI和特效作为原型资源。

- **状态：** `DONE`
- **依赖：** T450, T600
- **估算：** 2.0 人日
- **产出：** Sprite导入规范；SpriteAtlas；Pivot/PPU/SortingLayer；来源记录。
- **明确不做：** 不把125MB PSD放进Runtime Assets；不把单张角色当骨骼拆件。
- **验收：** 只导出透明PNG；尺寸/压缩/命名统一；生成图素材完成授权核对。
- **验证：** AssetImportValidationTests；确定性1920×1080原型资产画廊；真机视觉冒烟因平台任务延期明确NOT RUN。
- **证据：** `artifacts/evals/T630/`
- **提交：** `T630: <imperative summary>`

### T640 · 适配横屏比例、刘海/圆角、安全区和触控遮挡。

- **状态：** `BACKLOG`
- **依赖：** T600, T120
- **估算：** 1.5 人日
- **产出：** SafeAreaFitter；背景裁切策略；左右手按钮镜像。
- **明确不做：** 不按单一设备绝对坐标布局。
- **验收：** 16:9/19.5:9/平板不遮挡关键内容；UI区与划动区冲突可测。
- **验证：** ResolutionLayoutPlayModeTests；多设备截图。
- **证据：** `artifacts/evals/T640/`
- **提交：** `T640: <imperative summary>`

### T650 · 完成事件驱动教程遮罩、手势示意和跳过/回看。

- **状态：** `DONE`
- **依赖：** T520, T600
- **估算：** 1.5 人日
- **产出：** TutorialDirector；配置化步骤；一次性标记。
- **明确不做：** 不靠固定延时推进关键步骤。
- **验收：** 完成目标动作才推进；重开/跳过不锁死；文案来自Texts表。
- **验证：** TutorialGateTests；TutorialSkipPlayModeTests。
- **证据：** `artifacts/evals/T650/`
- **提交：** `T650: add event-driven tutorial overlay`

### T660 · 建立生产可玩入口与Battle组合根。

- **状态：** `DONE`
- **依赖：** T540, T550, T630, T650
- **估算：** 3.0 人日
- **产出：** 主菜单开始按钮与配置关卡选择；跨场景启动意图；玩家、敌人、波次、战斗输入、HUD、教程、结算和导航的生产组合根。
- **明确不做：** 不在场景/Inspector复制玩法数值或文案；不把测试夹具当生产运行时；不实现T640多设备适配、T700回归矩阵或微信平台验收。
- **验收：** 从Bootstrap点击Play进入MainMenu；点击开始后可选择普通关或Boss关；选择后进入Battle并能通过真实划线输入造成伤害、推进波次、显示HUD/教程与结算；Restart创建全新会话，Main Menu返回正式入口。
- **验证：** T660 EditMode；Bootstrap→MainMenu→Battle生产路径PlayMode；普通关与Boss玩家路径；Unity Editor手动点击冒烟与1920×1080截图；全量EditMode/PlayMode。
- **当前评审：** 中央白板与松开后闪线问题已修复；专项StrokeSampling 10/10、StrokeTrail 5/5、T660 PlayMode 4/4及全量EditMode 198/198、PlayMode 50/50通过。2026-07-17用户确认视觉复测通过，T660转为DONE。
- **证据：** `artifacts/evals/T660/`
- **提交：** `T660: add production playable battle entry`


## P6 代码可读性

> 用户要求为`Assets/_Game/Scripts`全量脚本补充中文注释。为遵守“一次一个原子任务”，手写代码按模块顺序处理，生成文件只经导出器修改。所有批次只允许增加注释，不改变运行语义。

### T670 · 为Core与Platform脚本补齐中文注释。

- **状态：** `DONE`
- **依赖：** T660
- **产出：** Core/Platform中所有手写C#的类型、方法与主要逻辑中文注释。
- **明确不做：** 不修改运行语义、测试、配置或Unity资源。
- **验收：** 六个脚本的公开/内部类型和每个方法均有易懂中文注释，非显而易见分支与池租约生命周期有逻辑说明。
- **验证：** 静态注释覆盖审查；Core/ObjectPool相关EditMode；全量EditMode/PlayMode。
- **证据：** `artifacts/evals/T670/`
- **提交：** `T670: document core and platform scripts in Chinese`

### T671–T681 · 分模块补齐其余中文注释并审计。

- **状态：** T671–T681全部`DONE`
- **依赖：** 按T671→T681顺序串行，首任务依赖T670。
- **产出：** Config手写Runtime、Input、Combat、Actors、Skills、Levels、Presentation、Bootstrap、Editor、ConfigIds生成器/生成物，以及最终全量审计。
- **明确不做：** 不把注释批次变成功能重构；不手改受管生成文件。
- **验收：** 每个批次独立证据、回归和可回滚提交；T681最终审计覆盖`Assets/_Game/Scripts`全量C#。
- **证据：** `artifacts/evals/T671/`至`artifacts/evals/T681/`

| 任务 | 状态 | 依赖 | 原子范围 |
|---|---|---|---|
| T671 | DONE | T670 | Config手写Runtime，排除`Generated/ConfigIds.g.cs` |
| T672 | DONE | T671 | Input |
| T673 | DONE | T672 | Combat |
| T674 | DONE | T673 | Actors |
| T675 | DONE | T674 | Skills |
| T676 | DONE | T675 | Levels |
| T677 | DONE | T676 | Presentation |
| T678 | DONE | T677 | Bootstrap |
| T679 | DONE | T678 | Editor |
| T680 | DONE | T679 | ConfigExporter注释生成逻辑与重新生成`ConfigIds.g.cs` |
| T681 | DONE | T680 | `Assets/_Game/Scripts`全量覆盖审计与最终回归 |

### T690 · 将火鱼静态原型替换为九帧循环动画Prefab。

- **状态：** `DONE`
- **依赖：** T630, T660
- **估算：** 0.5 人日
- **产出：** 用户提供的3×3鱼妖图集、九帧循环AnimationClip、AnimatorController、`EnemyFireFish` Prefab，以及保持`enemy_fire_fish`稳定键的配置与Registry绑定。
- **明确不做：** 不改火鱼玩法数值、攻击判定或状态机；不接入主角或其他敌人动画；不手工编辑Unity YAML和生成配置。
- **验收：** 九帧按源JSON顺序循环；对象池实例使用Prefab并保留现有敌人组件补齐与回收语义；配置与Registry严格校验通过；无新增编译警告或错误。
- **验证：** 配置完整门；动画资源EditMode；火鱼对象池PlayMode；全量EditMode/PlayMode；Unity Editor玩家路径目视确认。
- **证据：** `artifacts/evals/T690/`
- **提交：** `T690: animate fire fish prototype`

### T691 · 仅放开Assets目录下Unity必需的meta跟踪。

- **状态：** `DONE`
- **依赖：** T690
- **估算：** 0.1 人日
- **产出：** `.gitignore`保留全局meta兜底，仅反向放开`Assets/**/*.meta`；补齐对应已跟踪资产遗漏的meta。
- **明确不做：** 不放开Library、Temp、工具缓存或其他目录的meta；不修改产品逻辑或Unity资源内容。
- **验收：** Assets下meta不再被忽略；Assets外meta仍被忽略；无成批历史遗漏或无主meta进入提交。
- **验证：** `git check-ignore`边界检查；未跟踪meta审计；暂存白名单检查。
- **证据：** `artifacts/evals/T691/`
- **提交：** `T691: track Unity asset metadata`

### T692 · 修复Lit角色在Actors层缺少2D全局光而显示全黑。

- **状态：** `DONE`
- **依赖：** T690
- **估算：** 0.2 人日
- **产出：** 三个运行场景的Global Light 2D覆盖全部项目Sorting Layer；可重复执行的场景修复工具与回归测试。
- **明确不做：** 不改火鱼图集、动画、Prefab、Registry、玩法配置或材质类型；不以切回Default层或Unlit材质掩盖场景灯光配置缺口。
- **验收：** `Actors`层被Bootstrap、MainMenu、Battle的Global Light 2D覆盖；火鱼继续使用`Actors`层和`Sprite-Lit-Default`材质；运行时不再呈现全黑剪影。
- **验证：** T692 EditMode；T692 PlayMode；全量EditMode/PlayMode；Unity场景运行目视确认。
- **证据：** `artifacts/evals/T692/`
- **提交：** `T692: fix 2D light sorting layer coverage`

### T693 · 固定Android包只允许横屏方向。

- **状态：** `DONE`
- **依赖：** T660
- **估算：** 0.2 人日
- **产出：** Android Player Settings仅允许左右横屏；方向回归测试。
- **明确不做：** 不修改场景、Prefab、Build Profile、微信方向配置或T640多比例安全区适配。
- **验收：** Android构建不再以竖屏启动；设备可在左右横屏之间自动旋转；已有用户ProjectSettings改动完整保留。
- **验证：** T693 EditMode；ProjectSettings静态差异审计。
- **证据：** `artifacts/evals/T693/`
- **提交：** `T693: force Android landscape orientation`

### T694 · 将主角静态原型替换为待机与攻击动画Prefab。

- **状态：** `DONE`
- **依赖：** T630, T660, T692
- **估算：** 0.8 人日
- **产出：** 用户提供的九帧待机与十二帧攻击图集、两个AnimationClip、共享AnimatorController、`PlayerMoyan` Prefab，以及保持`char_moyan_idle`稳定键的配置与Registry绑定。
- **明确不做：** 不改玩家HP、伤害、手势判定、技能、关卡或文案；动画事件不承担伤害真相；不接入其他角色动画；不手工编辑Unity YAML和生成配置。
- **验收：** 两组帧按源JSON自然顺序播放；待机循环、攻击单次播放后回待机；生产战斗入口实例化配置Prefab，有效普通笔势触发攻击表现；配置、Registry、图集和2D光照合同保持有效。
- **验证：** 素材批次预检；配置完整门；T694动画资源EditMode；生产入口攻击动画PlayMode；全量EditMode/PlayMode；真实相机目视确认。
- **证据：** `artifacts/evals/T694/`
- **提交：** `T694: animate player prototype`

### T695 · 将十一帧爆炸动画接入怪物死亡特效。

- **状态：** `DONE`
- **依赖：** T440, T620, T630, T660, T692
- **估算：** 0.8 人日
- **产出：** 用户提供的十一帧爆炸图集、非循环AnimationClip、AnimatorController、池化`vfx_enemy_death` Prefab，以及`VfxCues`、`FeedbackCues`、AssetManifest和Registry绑定。
- **明确不做：** 不改敌人HP、死亡判定、掉落、计分、波次或Boss结算；动画完成事件不承担死亡真相；不替换其他VFX；不手工编辑Unity YAML和生成配置。
- **验收：** 十一帧按源JSON自然顺序播放且末帧获得完整帧时长；生产战斗入口只在已接受的敌人死亡事件上播放一次；敌人本体回收后特效仍在死亡位置完成播放；池化复用从首帧重新开始；配置、Registry、图集和2D光照合同保持有效。
- **验证：** 素材批次预检；配置完整门；T695动画资源EditMode；死亡反馈池化PlayMode；全量EditMode/PlayMode；真实相机目视确认。
- **证据：** `artifacts/evals/T695/`
- **提交：** `T695: animate enemy death effect`

### T696 · 明确增量、按风险升级的快速收尾合同。

- **状态：** `DONE`
- **依赖：** T695
- **估算：** 0.1 人日
- **产出：** `AGENTS.md`快速收尾规则，明确一次验证边界、条件文档、合并证据、路径优先审计和显式暂存。
- **明确不做：** 不降低原子提交、用户改动保护、配置唯一真相源和Unity YAML作者工具约束；不修改产品代码、配置或Unity资源。
- **验收：** 常规任务收尾限定为一次文档补丁、一次批量审计、一次显式暂存和一次提交；只有异常路径、漂移、ProjectSettings变化或验证边界后再改产品产物时升级深审。
- **验证：** 文档差异与白名单路径审计；纯文档变更不运行Unity测试。
- **提交：** `T696: streamline task closeout contract`

### T697 · 修复生产参考空间中怪物死亡特效被错误缩小的问题。

- **状态：** `DONE`
- **依赖：** T695
- **估算：** 0.2 人日
- **产出：** `VfxPoolItem`按Sprite二维XY边界计算配置缩放；生产Battle相机死亡特效像素尺寸回归断言。
- **明确不做：** 不修改死亡判定、敌人回收、死亡动画资源、Prefab、Registry、配置表或其他反馈参数。
- **验收：** 怪物死亡时特效在真实Battle参考空间中保持配置尺寸，不再因父节点Z缩放缩小；既有死亡位置快照与池化复用语义保持不变。
- **验证：** T695 PlayMode；全量PlayMode；真实Battle相机目视确认。
- **证据：** `artifacts/evals/T697/`
- **提交：** `T697: fix production VFX scaling`

### T698 · 实现方案C青白闪电画笔特效并打通完整流程。

- **状态：** `DONE`
- **依赖：** T340, T630, T660
- **估算：** 1.0 人日
- **产出：** 权威配置中的画笔样式表与架势绑定；分层青白主轨迹、确定性稀疏电弧、池化复用；由Unity作者工具生成的`vfx_slash` Prefab；生产入口集成与自动化、目视证据。
- **明确不做：** 不修改笔迹采样、命中判定、伤害、技能、关卡或其他VFX；不把Prefab或Inspector变成第二数值库；不手工编辑Unity YAML。
- **验收：** 刀/符架势均从配置解析到方案C；外辉光、青白主体、白色核心与稀疏电弧共用同一命中路径并按配置宽度缩放；电弧生成可重复；完成后整体淡出并可安全复用；生产Battle入口从`VfxCues`和AssetRegistry取得Prefab；配置、Unity测试及真实相机目视检查通过。
- **验证：** 配置导出器测试与完整门；T698 EditMode/PlayMode；全量EditMode/PlayMode；真实Battle相机截图。
- **证据：** `artifacts/evals/T698/`
- **提交：** `T698: add lightning stroke trail`

### T699 · 修复生产战斗接触伤害与右侧出生推进。

- **状态：** `DONE`
- **依赖：** T420, T500, T660
- **估算：** 0.5 人日
- **产出：** 玩家和敌人身体碰撞体；配置驱动的接触伤害；按各自出生时刻推进的敌人移动时钟；生产入口回归测试。
- **明确不做：** 不修改敌人、攻击、关卡、波次或出生点数值；不实现投射物命中；不提前实施T700。
- **验收：** 敌人在屏幕右半区出生并持续向主角方向推进；晚出生敌人不会按关卡累计时间跳到路径中段；敌人未接触主角时攻击执行不扣血，身体接触后按`Enemies.contactDamage`扣血并沿用玩家受击无敌帧。
- **验证：** T699 EditMode/PlayMode；全量EditMode/PlayMode；真实Battle场景目视检查。
- **证据：** `artifacts/evals/T699/`
- **提交：** `T699: fix enemy contact damage and approach`

### T699A · 接入生产投射物命中、可见移动与画笔击落。

- **状态：** `DONE`
- **依赖：** T370, T440, T660, T699
- **估算：** 0.8 人日
- **产出：** 配置驱动的生产投射物生成、可见渲染、较慢弹道、碰撞扣血、画笔切断/反弹与池化回收。
- **明确不做：** 不修改敌人本体接触伤害、攻击配置/策略定义、关卡/波次/出生点、投射物美术绑定、场景、Prefab、Registry或ProjectSettings；不提前实施T700。
- **验收：** 远程攻击在攻击者位置生成可见弹体并向玩家飞行；速度与寿命来自权威配置且弹道可读、能覆盖右侧出生点到玩家的距离；敌方弹体只有命中玩家时才按`Projectiles.damage`扣血并沿用受击无敌帧；真实画笔路径可命中弹体并按既有配置切断或反弹，回收后不残留归属、碰撞或可见状态。
- **验证：** 配置导出器与漂移门；T699A EditMode/PlayMode；全量EditMode/PlayMode；真实Battle场景目视检查。
- **证据：** `artifacts/evals/T699A/`
- **提交：** `T699A: integrate production projectiles`


## P7 质量发布

### T700 · 补齐纯规则EditMode回归矩阵。

- **状态：** `READY`
- **依赖：** T540, T660, T681, T699A
- **估算：** 2.0 人日
- **产出：** 手势/伤害/配置/技能/状态机/Boss测试。
- **明确不做：** 纯算法不使用场景测试。
- **验收：** 边界、无效输入、重复事件覆盖；测试无顺序依赖。
- **验证：** 全量EditMode。
- **证据：** `artifacts/evals/T700/`
- **提交：** `T700: <imperative summary>`

### T710 · 补齐Unity集成、完整单局、暂停、重开和生命周期PlayMode测试。

- **状态：** `BACKLOG`
- **依赖：** T550, T650
- **估算：** 3.0 人日
- **产出：** 专项与E2E测试；按玩家意图命名的测试helper。
- **明确不做：** 不直接篡改内部状态伪造玩家动作。
- **验收：** 三关核心路径可自动跑完；动作前不会错误推进；重开3次通过。
- **验证：** 全量PlayMode。
- **证据：** `artifacts/evals/T710/`
- **提交：** `T710: <imperative summary>`

### T720 · 审计所有玩法数值、内容和文案是否来自配置表。

- **状态：** `BACKLOG`
- **依赖：** T710, T250
- **估算：** 1.0 人日
- **产出：** ConfigCoverageReport；硬编码扫描规则；例外白名单。
- **明确不做：** 不把引擎常量误判策划数值；不保留Inspector双真相。
- **验收：** 敌人/技能/关卡改动无需改C#；所有例外有Decision记录。
- **验证：** rg静态扫描；配置覆盖测试。
- **证据：** `artifacts/evals/T720/`
- **提交：** `T720: <imperative summary>`

### T730 · 在目标低端机收敛CPU、GC、内存、DrawCall、纹理和包体。

- **状态：** `BACKLOG`
- **依赖：** T710, T630
- **估算：** 3.0 人日
- **产出：** Profiler记录；质量档；图集/音频/纹理优化；预算报告。
- **明确不做：** 不只看Editor Profiler；不牺牲到不可读画质。
- **验收：** 目标60fps且不持续低于30fps；热路径GC约0；内存与包体在项目预算。
- **验证：** 设备性能场景；10分钟压力。
- **证据：** `artifacts/evals/T730/`
- **提交：** `T730: <imperative summary>`

### T740 · 自动化配置验证、Unity测试、Web构建和证据归档。

- **状态：** `BACKLOG`
- **依赖：** T730
- **估算：** 2.0 人日
- **产出：** verify-all脚本；测试XML/构建日志/体积摘要；版本号注入。
- **明确不做：** 日志不泄露AppSecret；长构建不放在最小反馈环前。
- **验收：** 一条命令产出可审查结果；失败层级清晰；构建可复现。
- **验证：** 本地/CI流水线冒烟。
- **证据：** `artifacts/evals/T740/`
- **提交：** `T740: <imperative summary>`

### T750 · 生成微信小游戏发布候选并完成四级平台验收。

- **状态：** `BACKLOG`
- **依赖：** T740, T120
- **估算：** 2.0 人日
- **产出：** RC构建；转换日志；DevTools结果；真机矩阵；已知问题。
- **明确不做：** 任一未执行层不得PASS；不使用未固定SDK。
- **验收：** 三关/前后台/音频/存档/触摸/异常重启真机通过；阻断问题为0。
- **验证：** 人工RC清单。
- **证据：** `artifacts/evals/T750/`
- **提交：** `T750: <imperative summary>`

### T760 · 完成发布资料、版本冻结、回滚方案和最终证据索引。

- **状态：** `BACKLOG`
- **依赖：** T750
- **估算：** 1.0 人日
- **产出：** RELEASE_CHECKLIST；代码/配置/SDK/Unity追溯；回滚说明；风险接受记录。
- **明确不做：** 冻结后不顺手加功能。
- **验收：** 发布包可完整追溯；所有KNOWN ISSUE有明确处理决定。
- **验证：** 独立验收。
- **证据：** `artifacts/evals/T760/`
- **提交：** `T760: <imperative summary>`

## 状态推进规则

1. 只选择依赖全部为 `DONE` 的第一个 `READY` 任务。
2. 开始前记录 `git status --short --branch` 并写预计改动白名单。
3. 完成后依次执行专项测试、真实玩家路径、Console检查和diff审查。
4. 通过后写 `verification.md`、提交当前任务文件、把下一任务改为READY，然后停止。
5. 缺Unity Editor、微信开发者工具或真机时，相关结论必须是 `BLOCKED` 或 `KNOWN ISSUE`。
