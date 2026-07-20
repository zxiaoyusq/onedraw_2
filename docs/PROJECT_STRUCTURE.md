# PROJECT_STRUCTURE：工程目录与模块说明

本文说明《一笔镇妖》当前仓库的实际结构、文件归属、程序集依赖和主要运行链路。它是面向开发者的导航文档，不替代各领域的权威合同：玩法以 `GAME_DESIGN_MVP.md` 为准，范围以 `MVP_SCOPE.md` 为准，技术约束以 `TECH_SPEC.md` 为准，配置契约以 `CONFIG_SCHEMA.md` 为准，任务状态以 `TASKS.md` 为准。

## 1. 工程基线

- 仓库根目录同时是 Unity 工程根目录，不存在额外的 `game/` 包装层。
- Unity 版本固定为 `6000.5.1f1`，机器真相位于 `ProjectSettings/ProjectVersion.txt`。
- 项目使用 URP 2D、Input System、uGUI、TextMeshPro 和 Unity Test Framework。
- 游戏为横屏，参考分辨率为 1920×1080；手势与界面布局使用参考像素和 Safe Area，不依赖 `Screen.dpi`。
- 自有游戏内容统一放在 `Assets/_Game/`，避免与第三方包和 Unity 工程设置混杂。

## 2. 仓库根目录

```text
repo/
├─ AGENTS.md                    # Codex/Claude 执行合同
├─ CLAUDE.md                    # Claude 入口说明
├─ project-index.yaml           # 当前工程状态、版本、哈希与证据索引
├─ Assets/                      # Unity 资产；自有内容集中在 Assets/_Game
├─ Design/Config/               # 正式策划配置工作簿
├─ config/                      # Schema、样例和工作簿镜像
├─ Tools/                       # 配置导出、美术处理和 CI 脚本
├─ docs/                        # 设计、技术、流程、测试与发布文档
├─ artifacts/evals/             # 按任务归档的验收证据
├─ Packages/                    # Unity Package Manager 清单与嵌入包
├─ ProjectSettings/             # Unity 工程、场景和平台设置
├─ prompts/                     # 项目启动与执行提示
├─ tasks/                       # 任务辅助材料
├─ templates/                   # 验证、白名单等工作流模板
├─ reference/                   # 外部参考材料
├─ outputs/                     # 工具输出或交付辅助材料
└─ Builds/、Library/、Logs/等    # 本地生成目录，不作为源码维护
```

### 根目录职责

| 路径 | 职责 | 维护规则 |
|---|---|---|
| `project-index.yaml` | 汇总 Unity 版本、当前任务、配置哈希、子系统状态和证据路径 | 完成原子任务后同步更新，不作为玩法或配置字段的第二真相源 |
| `Design/Config/GameConfig.xlsx` | 正式策划内容与玩法数值的唯一真相源 | 配置修改必须经过完整配置闭环 |
| `config/schema/gameplay.schema.json` | 配置 JSON 结构合同 | 与字段字典、导出器、DTO、校验和测试同步维护 |
| `config/examples/` | 配置结构样例 | 仅用于合同和工具验证，不作为 Runtime 数据源 |
| `config/一笔镇妖_游戏配置表模板.xlsx` | 正式工作簿的同步镜像 | 不允许独立编辑形成第二份配置 |
| `Tools/ConfigExporter/` | 独立 .NET 8 配置读取、校验和稳定导出工具 | xlsx 解析依赖只能存在于此，不进入 Unity Runtime |
| `Tools/CI/` | 配置验证、Unity 测试、Web/微信构建和证据初始化入口 | 脚本失败必须返回非零；部分验证不得标记为完整 PASS |
| `artifacts/evals/TASK-ID/` | 单个原子任务的白名单、基线、测试结果和验收结论 | 原始敏感日志放在忽略目录，只提交过滤后的证据 |
| `Packages/` | Unity 包版本锁定及 embedded 微信小游戏 SDK | SDK 必须固定版本或 commit，不得静默升级 |
| `ProjectSettings/` | Unity 版本、Build Settings、URP、输入和平台设置 | 通过 Unity Editor 修改，避免手工编辑 Unity YAML |

## 3. Unity 游戏目录

```text
Assets/_Game/
├─ Art/
│  ├─ Audio/                    # 音频资产；当前仍有受管静音占位
│  ├─ Backgrounds/              # 普通关与 Boss 关背景
│  ├─ Characters/Moyan/         # 玩家玄狸·墨砚视觉资产
│  ├─ Enemies/                  # 六种非 Boss 敌人与 Boss 图片
│  ├─ Sprites/                  # 技能图标、投射物等通用 Sprite
│  ├─ SpriteAtlases/            # Backgrounds/Characters/Enemies/UI/VFX 图集
│  ├─ UI/                       # HUD、按钮、架势和终极图标、TMP 字体
│  └─ VFX/                      # VFX Sprite 与可池化 VFX Prefab
├─ Config/
│  ├─ Generated/                # gameplay_config.json 与 hash 旁车
│  └─ Registry/                 # AssetRegistry.asset 和受管占位资源
├─ Prefabs/
│  ├─ Actors/                   # 当前落盘的精英与 Boss 原型 Prefab
│  ├─ Projectiles/              # 投射物 Prefab 归属目录
│  ├─ UI/                       # 可复用 UI Prefab 归属目录
│  └─ VFX/                      # 通用 VFX Prefab 归属目录
├─ Scenes/
│  ├─ Bootstrap.unity           # build index 0，初始化运行时真相源
│  ├─ MainMenu.unity            # 生产主菜单和关卡选择入口
│  └─ Battle.unity              # 生产战斗组合根
├─ Scripts/
│  ├─ Core/                     # 无业务方向依赖的核心接口、场景名和对象池
│  ├─ Config/                   # Runtime DTO、解析、只读索引与资源 Registry
│  ├─ Input/                    # 指针、采样、几何处理和笔势识别
│  ├─ Combat/                   # 命中、伤害、投射物、连斩、评分规则
│  ├─ Actors/                   # 玩家、敌人、弱点、防御与行为策略
│  ├─ Skills/                   # Skill → EffectGroup → Effect 执行链与 Boss 阶段
│  ├─ Levels/                   # 波次、关卡、教程、流程、结算和进度
│  ├─ Presentation/             # 轨迹、HUD、教程遮罩和战斗反馈
│  ├─ Platform/                 # 平台相关实现与平台冒烟探针
│  ├─ Bootstrap/                # 场景流、生产组合根和战斗会话装配
│  └─ Editor/                   # 资源生成/校验、字体、场景和构建工具
└─ Tests/
   ├─ EditMode/Txxx/            # 纯规则、配置合同与 Editor 结构测试
   └─ PlayMode/Txxx/            # Unity 接线、场景、输入和完整单局测试
```

所有 Unity 资产都必须保留对应 `.meta` 文件。场景、Prefab、ScriptableObject、Importer 和 ProjectSettings 应通过 Unity Editor 或受控 Editor 工具生成、修改并保存，不手工编辑其 YAML。

## 4. Runtime 程序集边界

`Assets/_Game/Scripts` 下的目录同时定义 Runtime 程序集边界。依赖方向如下：

```text
OneStrokeDemon.Core
├─ OneStrokeDemon.Config
├─ OneStrokeDemon.Input
│  └─ OneStrokeDemon.Combat ── Config
│     └─ OneStrokeDemon.Actors ── Config
│        └─ OneStrokeDemon.Skills ── Config, Combat
│           └─ OneStrokeDemon.Levels ── Config, Actors
├─ OneStrokeDemon.Platform
└─ OneStrokeDemon.Presentation ── Config, Combat, Actors, Levels

OneStrokeDemon.Bootstrap ── 所有 Runtime 模块
```

上图用于表达分层方向；精确引用以各目录内 `.asmdef` 为准。当前主要边界为：

| 程序集 | 主要职责 | 允许引用的项目程序集 |
|---|---|---|
| `OneStrokeDemon.Core` | 稳定接口、场景常量、通用对象池合同 | 无 |
| `OneStrokeDemon.Config` | 配置解析、版本/哈希校验、只读索引、AssetRegistry | Core |
| `OneStrokeDemon.Input` | 输入统一、Safe Area 映射、采样、几何和识别 | Core |
| `OneStrokeDemon.Combat` | 笔迹命中、伤害、评分、连斩和投射物 | Core、Config、Input |
| `OneStrokeDemon.Actors` | 玩家和通用敌人运行时 | Core、Config、Combat |
| `OneStrokeDemon.Skills` | 技能效果链和 Boss 阶段控制 | Core、Config、Combat、Actors |
| `OneStrokeDemon.Levels` | 关卡、波次、流程、教程、结算和存档模型 | Core、Config、Actors、Skills |
| `OneStrokeDemon.Presentation` | HUD、轨迹、教程和战斗反馈 | Core、Config、Combat、Actors、Levels |
| `OneStrokeDemon.Platform` | 平台实现边界和平台探针 | Core |
| `OneStrokeDemon.Bootstrap` | 唯一生产装配层，连接所有 Runtime 模块 | 所有 Runtime 模块 |

补充约束：

- 禁止循环依赖；底层规则程序集不能反向引用 Bootstrap 或 Presentation。
- `OneStrokeDemon.Editor` 只在 Editor 中编译，可引用全部 Runtime 模块和构建依赖。
- `OneStrokeDemon.Tests.EditMode` 与 `OneStrokeDemon.Tests.PlayMode` 是独立测试程序集。
- Gameplay 模块不得直接调用微信 SDK 静态 API；正式平台抽象和注入仍属于 `T130`。
- 新增纯规则优先放入无 `MonoBehaviour` 依赖的层，并先添加 EditMode 测试。

## 5. 生产启动与场景生命周期

当前生产玩家路径已经接入三个真实场景：

```text
Bootstrap.unity
  └─ BootstrapController
     ├─ 解析并发布 GameplayConfigRuntime
     ├─ 校验并发布 AssetRegistryRuntime
     ├─ 初始化 PointerInputRuntime
     └─ SceneFlowService.LoadMainMenu()
          ↓
MainMenu.unity
  └─ MainMenuCompositionRoot
     ├─ 从 Levels、Texts 和 ProgressSnapshot 创建菜单
     ├─ 点击“开始”后显示关卡选择
     ├─ 只允许选择已解锁的配置关卡
     └─ BattleLaunchContext.Select(levelId) → LoadBattle()
          ↓
Battle.unity
  └─ BattleCompositionRoot
     ├─ 读取已选择或首个已解锁关卡
     ├─ 创建 ProductionBattleSession
     ├─ 组合玩家、敌人、波次、输入、技能和流程
     ├─ 组合 HUD、教程、反馈、结算与进度
     └─ 支持 Restart、NextLevel、ReturnToMainMenu
```

关键生命周期规则：

- `Bootstrap` 必须是 Build Settings 的 build index 0。
- 配置、Registry 或统一输入初始化失败时，不允许继续进入主菜单。
- `MainMenuCompositionRoot` 和 `BattleCompositionRoot` 是生产场景的唯一装配所有者，业务规则不应复制到场景或 Inspector。
- `BattleLaunchContext` 只传递经过配置目录验证的 `levelId`，不保存关卡数值。
- 战斗重开必须释放旧 `ProductionBattleSession` 后创建新会话；返回菜单时清除关卡选择。
- 当前最小进度保存由 `PlayerPrefsProgressSaveStore` 提供；替换成平台存储时仍应通过进度存储接口，不让 Levels 直接依赖微信 SDK。

## 6. 配置数据链路

```text
Design/Config/GameConfig.xlsx
  ↓ Tools/ConfigExporter：读取、建模、完整校验、稳定序列化
config/schema/gameplay.schema.json
  ↓
Assets/_Game/Config/Generated/gameplay_config.json
Assets/_Game/Config/Generated/gameplay_config.hash
Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs
  ↓ BootstrapController
GameplayConfigRuntime / IConfigProvider
  ↓
Combat、Actors、Skills、Levels、Presentation、Bootstrap
```

维护规则：

- 游戏运行时不解析 xlsx。
- `gameplay_config.json`、`gameplay_config.hash` 和 `ConfigIds.g.cs` 是同源受管生成物，禁止手工修改。
- 配置变更必须同步工作簿、字段字典、Schema、导出器、DTO、校验、文档和测试中受影响的部分。
- Inspector 和 ScriptableObject 只能保存 Unity 对象引用、场景引用和明确调试兜底，不得保存重复的 HP、伤害、CD、波次或文案。
- 默认只读验证入口为 `Tools/CI/verify-config.sh`；只有审查过工作簿修改后才使用 `Tools/CI/verify-config.sh --update` 更新生成物。

## 7. 资源引用链路

配置表使用稳定 `assetKey`，Unity 对象由 Registry 解析：

```text
配置表中的 assetKey
  ↓
Assets/_Game/Config/Registry/AssetRegistry.asset
  ↓ AssetRegistryService 完整校验后发布
IAssetRegistry
  ↓
Sprite / Prefab / AudioClip / Battle Scene Reference
```

`AssetRegistry.asset` 只负责 `assetKey → UnityEngine.Object` 映射，不保存平衡数值。资源替换应保持配置 ID 稳定，通过 Unity Editor 更新对象引用。当前原型美术已覆盖主要 Sprite 和 VFX/Actor Prefab；受管音频键仍含静音占位，不能据此宣称发布音频完成。

## 8. 测试与证据结构

```text
Assets/_Game/Tests/EditMode/Txxx/     # 纯规则、配置和结构回归
Assets/_Game/Tests/PlayMode/Txxx/     # Unity 生命周期和玩家路径回归
Tools/ConfigExporter/Tests/           # 导出器与配置错误用例
Tools/CI/                              # 统一测试、配置和构建入口
artifacts/evals/Txxx/                  # 每个任务的可追溯证据
artifacts/tmp/                         # 本机原始日志，忽略且不提交
```

验证层次为：静态结构 → EditMode → PlayMode → 真实玩家路径 → 稳定性 → Web/微信平台。缺少 Editor、微信开发者工具或真机时必须记录 `NOT RUN` 或 `BLOCKED`，不能用较低层结果代替。

常用入口：

```bash
# 配置闭环，只读
Tools/CI/verify-config.sh

# Unity 测试，EditMode 与 PlayMode 分开执行
Tools/CI/run-unity-tests.sh --mode EditMode --results <xml> --log <log>
Tools/CI/run-unity-tests.sh --mode PlayMode --results <xml> --log <log>

# 标准 WebGL 与微信转换构建
Tools/CI/build-web.sh --output Builds/WebGL
Tools/CI/build-wechat.sh

# 初始化单任务证据，不覆盖已有目录
Tools/CI/new-task-evidence.sh TASK-ID
```

## 9. 新文件放置规则

| 新内容 | 应放位置 | 同步要求 |
|---|---|---|
| 纯算法、稳定接口或通用池合同 | `Scripts/Core` 或最接近的领域程序集 | EditMode 测试；不得制造反向依赖 |
| 指针、采样、几何、笔势识别 | `Scripts/Input` | 阈值从配置边界映射，不在 Input 内硬编码玩法数值 |
| 命中、伤害、投射物、评分 | `Scripts/Combat` | 纯规则优先，补 EditMode 测试 |
| 玩家或敌人状态与策略 | `Scripts/Actors` | 内容组合来自配置，不为每种敌人新增继承树 |
| 技能或效果执行器 | `Scripts/Skills` | 同步策略登记、配置校验和测试 |
| 关卡、波次、教程、结算 | `Scripts/Levels` | 关卡内容、时间轴、条件和文案来自配置 |
| HUD、轨迹、VFX、教程表现 | `Scripts/Presentation` | 只消费状态和事件，不反写战斗真相 |
| 场景组合或跨场景装配 | `Scripts/Bootstrap` | 只接线，不复制子系统业务规则 |
| Unity 作者工具或构建工具 | `Scripts/Editor` | 确保不进入 Runtime 程序集 |
| 游戏 Sprite、字体、音频、VFX | `Art` 对应分类 | 配置引用时同步 AssetRegistry |
| 可复用 Unity 对象 | `Prefabs` 对应分类 | 通过 Unity Editor 保存；数值仍来自配置 |
| 场景 | `Scenes` | 通过 Unity Editor 创建，必要时同步 Build Settings 与 Registry |
| 纯规则测试 | `Tests/EditMode/TASK-ID` | 与任务证据和 `TEST_PLAN.md` 对齐 |
| 集成或玩家路径测试 | `Tests/PlayMode/TASK-ID` | 走真实配置、场景和生命周期 |

## 10. 维护边界与当前状态

- 本文描述的是当前工程结构，不是未来目录愿望；目录或装配职责变化时应同步更新。
- 当前生产路径为 `Bootstrap → MainMenu → Battle`，三个 MVP 关卡通过配置和进度选择进入生产战斗会话。
- 微信标准 Web 构建与转换已有分层证据，但开发者工具和真机验证仍受外部环境约束，不能把转换成功等同于发布可用。
- `T700` 是当前排期中的 READY 任务，负责补齐纯规则 EditMode 回归矩阵；本文档创建不代表 `T700` 已开始或完成。

## 11. 相关权威文档

- `docs/GAME_DESIGN_MVP.md`：玩法真相与 MVP 核心循环。
- `docs/MVP_SCOPE.md`：必须完成、明确不做和完成标准。
- `docs/TECH_SPEC.md`：技术约束、程序集边界和关键实现原则。
- `docs/CONFIG_SCHEMA.md`：配置字段、校验和 Runtime 语义。
- `docs/CONFIG_PIPELINE.md`：配置生产和验证流程。
- `docs/ASSET_INTEGRATION.md`、`docs/ART_PIPELINE.md`：资源接入与美术处理。
- `docs/TEST_PLAN.md`：测试金字塔、专项覆盖和证据要求。
- `docs/WORKFLOW.md`：原子任务、白名单、验证和提交规范。
- `docs/PLATFORM_WECHAT.md`：微信小游戏平台边界和分级验收。
- `docs/TASKS.md`、`docs/PROGRESS.md`：任务状态与当前进度。
- `project-index.yaml`：机器可读的工程状态和证据索引。
