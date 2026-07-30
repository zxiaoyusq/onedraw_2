# 《一笔镇妖》画笔、主角与敌人内容制作手册

> 面向策划、美术和需要在 Unity 中落地资源的开发者。本文说明当前工程真实可用的创建、导入、绑定和验收流程。
> 适用工程版本：Unity `6000.5.1f1`；最后核对日期：2026-07-30。

## 1. 先看这一页：你要做哪一类改动

| 目标 | 最短安全路径 | 是否改工作簿 | 是否需要 Unity |
|---|---|---:|---:|
| 只调整现有闪电画笔颜色、粗细、电弧密度 | 改`StrokeTrailStyles`，重新导出配置 | 是 | 最终目视需要 |
| 新增一个仍使用“三层主线+电弧”拓扑的画笔样式 | 新增`StrokeTrailStyles`行，并让`Stances`引用它 | 是 | 最终目视需要 |
| 改变画笔组件结构，例如新增粒子、拖尾或贴图层 | 新增/修改幂等 Unity 作者工具，再生成 Prefab | 视资源键是否变化 | 是 |
| 替换现有主角或敌人的单张立绘 | 导入同类型 Sprite 或 Prefab，改绑同一个 Registry 键 | 通常否 | 是 |
| 把现有静态角色升级为图集动画 | PNG+帧JSON预检，生成 Clip/Controller/Prefab/Atlas，改绑 Registry | `Sprite`变`Prefab`时要改 | 是 |
| 新增一个玩法敌人 | 先补全敌人配置链，再制作和绑定资源 | 是 | 是 |
| 替换怪物死亡动画 | PNG+帧JSON预检，生成非循环 VFX Prefab，绑定`vfx_enemy_death` | 路径/类型/表现参数变化时要改 | 是 |

无论哪一种，完整链路都是：

```text
来源与授权
→ 素材/配置预检
→ 权威工作簿（需要时）
→ 配置导出
→ Unity导入或作者工具生成
→ SpriteAtlas
→ AssetRegistry绑定
→ 自动化测试
→ 真实Battle相机目视验收
```

### 最重要的执行顺序

如果同一批次既要运行通用 T630 工具，又要运行专用动画或画笔工具，固定按下面顺序：

```text
T630通用原型资源
→ T690火鱼
→ T694主角
→ T695怪物死亡VFX
→ T698闪电画笔（最后）
→ Validate Asset Registry
```

原因：

- T630 会重建全部非`/Animated/`的通用 VFX Prefab；
- T690、T694、T695维护各自的动画切片、Clip、Controller、Prefab和Atlas；
- `vfx_slash.prefab`不在`/Animated/`目录，T630会把它恢复成通用单图 VFX，所以必须最后再运行 T698；
- 重跑工具后看到日志中的`*_AUTHORING_PASS`或`T698_STROKE_TRAIL_PREFAB_PASS`，才表示作者流程完成。

## 2. 三条不可破坏的边界

### 2.1 数值和资源引用分开

- HP、伤害、速度、CD、帧表现寿命、池预热、画笔颜色和宽度等内容值来自`Design/Config/GameConfig.xlsx`。
- Prefab、Sprite、AudioClip和Scene的 Unity 对象引用保存在`Assets/_Game/Config/Registry/AssetRegistry.asset`。
- Prefab和 Inspector 只保存组件结构与资源引用，不保存工作簿里已有的玩法或表现数值。

### 2.2 只编辑权威源

- 配置唯一内容源：`Design/Config/GameConfig.xlsx`。
- `config/一笔镇妖_游戏配置表模板.xlsx`只是字节一致的交付镜像，不能单独编辑。
- 不手改以下生成物：
  - `Assets/_Game/Config/Generated/gameplay_config.json`
  - `Assets/_Game/Config/Generated/gameplay_config.hash`
  - `Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs`
- 工作簿审核通过后，通过`Tools/CI/verify-config.sh --update`统一生成。

### 2.3 不手工编辑 Unity YAML

不要用文本编辑器修改`.prefab`、`.anim`、`.controller`、`.spriteatlasv2`、`.asset`、`.unity`或`.meta`。
这些资产必须通过 Unity Editor API、项目菜单或 Unity MCP 创建和修改。

## 3. 开始前要准备什么

### 3.1 所有美术资源

1. 在`docs/ASSET_SOURCES.md`登记来源、作者或生成工具、日期、原始 SHA-256、授权范围和审核状态。
2. Runtime只接收PNG，不把PSD/PSB放进`Assets/`。
3. 文件名使用小写 ASCII 和下划线：
   - 主角：`moyan_*`
   - 敌人：`fire_fish`、`skeleton_ghost`等稳定小写蛇形名
   - 画笔/VFX：`vfx_*`
4. 透明角色和VFX检查透明通道；背景可以不透明。
5. 保留原始源文件，不把下载目录或临时导出物当正式项目输入。

### 3.2 动画额外输入

每个动画至少提供：

- 一张 PNG sprite sheet；
- 一份与PNG配套的 FramePacker/TexturePacker 风格帧 JSON；
- 要替换的现有`assetKey`，或明确声明“只导入、不接运行时”；
- 动画状态名、是否循环、FPS、目标 Prefab 名。

支持的最小帧 JSON：

```json
{
  "frames": {
    "001.png": {
      "frame": { "x": 0, "y": 0, "w": 256, "h": 256 },
      "rotated": false,
      "trimmed": false
    }
  },
  "meta": {
    "size": { "w": 768, "h": 768 }
  }
}
```

约束：

- JSON坐标原点是左上角，Unity Sprite Rect原点是左下角；
- 转换公式为`unityY = textureHeight - jsonY - frameHeight`；
- 帧名按自然数字顺序排列：`2`必须早于`10`；
- `rotated=true`、`trimmed=true`、越界、重叠、空帧或JSON尺寸与PNG不一致都必须停止导入；
- 不要因为当前素材恰好是规则网格，就丢弃源JSON并猜测切片。

### 3.3 默认导入值

| 类型 | Sprite Mode | PPU | Pivot | Sorting Layer | 纹理 |
|---|---|---:|---|---|---|
| 静态主角/敌人 | Single | 100 | 脚底`(0.5,0.08)` | Actors | sRGB、透明、无MipMap、Clamp、Bilinear |
| 动画主角/敌人 | Multiple | 100 | 脚底`(0.5,0.08)` | Actors | 同上，进入Atlas前建议无损 |
| VFX动画 | Multiple | 100 | 中心`(0.5,0.5)` | VFX | 同上，进入Atlas前建议无损 |
| 画笔Prefab | 不依赖动画切片 | — | — | 运行时由配置设置 | LineRenderer拓扑 |

飞行敌人可以显式使用不同Pivot，但必须在导入计划中记录，不能靠场景中随意偏移掩盖问题。

## 4. 通用配置与 Registry 流程

### 4.1 什么情况下必须改工作簿

出现以下任一情况就要改`Design/Config/GameConfig.xlsx`：

- 新增`assetKey`；
- `AssetManifest.assetType`在`Sprite`和`Prefab`之间变化；
- `AssetManifest.addressOrPath`变化；
- 新增玩法敌人、文案、出生配置或策略引用；
- 新增/调整`VfxCues`、`FeedbackCues`或`StrokeTrailStyles`内容值；
- 改变画笔样式与`Stances`的绑定。

如果只是把 Registry 中某个键从旧 Prefab 换成同类型新 Prefab，稳定键、类型和配置路径都不变，通常不需要改工作簿。

### 4.2 配置导出

Windows没有全局.NET SDK时，直接使用Unity随附的.NET 8。工作簿审核通过后，从仓库根目录执行：

```powershell
$dotnet = "C:/Program Files/Unity/Hub/Editor/6000.5.1f1/Editor/Data/DotNetSdk/dotnet.exe"

& $dotnet run --project Tools/ConfigExporter -- generate `
  --input Design/Config/GameConfig.xlsx `
  --output Assets/_Game/Config/Generated/gameplay_config.json `
  --hash-output Assets/_Game/Config/Generated/gameplay_config.hash `
  --ids-output Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs `
  --schema config/schema/gameplay.schema.json `
  --strict
```

导出后检查工作簿、字节一致镜像、JSON、hash和`ConfigIds.g.cs`的差异，再运行只读验证和导出器测试：

```powershell
& $dotnet run --project Tools/ConfigExporter -- verify `
  --input Design/Config/GameConfig.xlsx `
  --output Assets/_Game/Config/Generated/gameplay_config.json `
  --hash-output Assets/_Game/Config/Generated/gameplay_config.hash `
  --ids-output Assets/_Game/Scripts/Config/Generated/ConfigIds.g.cs `
  --schema config/schema/gameplay.schema.json `
  --strict

& $dotnet test Tools/ConfigExporter/Tests/ConfigExporter.Tests.csproj --nologo
```

然后在Unity Test Runner或MCP中运行EditMode和PlayMode的`ConfigPipeline`分类。

在macOS、Linux、CI或`.sh`已保持LF的Git Bash工作树中，可以使用一键完整门：

```bash
Tools/CI/verify-config.sh --update
Tools/CI/verify-config.sh
```

当前仓库未在`.gitattributes`固定`.sh`的LF；如果Windows执行时出现`$'\r'`或`pipefail\r`，说明检出文件被转换为CRLF。不要把失败误判为配置错误，也不要在内容任务中顺手批量改脚本；改用上面的Windows命令和Unity Test Runner/MCP，脚本换行问题见`BUG-0009`。

### 4.3 Registry 的正确理解

- 稳定身份来自`assetKey`，例如`char_moyan_idle`、`enemy_fire_fish`、`vfx_enemy_death`、`vfx_slash`。
- Runtime从 Registry取得实际 Unity 对象，不从工作簿路径或GUID动态加载。
- `AssetManifest.addressOrPath`用于配置期预期路径、作者工具校验和产物审查，不代替 Registry 对象引用。
- 先让生成配置期待正确的类型和路径，再绑定 Registry；不要反过来。

Unity菜单：

```text
One Stroke Demon/Config/Create or Repair Asset Registry
One Stroke Demon/Config/Validate Asset Registry
```

`Create or Repair`只会保留同类型合法引用，并给缺失/错型的新键回填占位；它不会猜测你想绑定哪张新图。
创建或修复后，仍要在 Registry 中明确检查目标键是否指向正确对象，再运行`Validate`。

## 5. 画笔特效

### 5.1 当前生产画笔是什么

当前稳定资源键为`vfx_slash`，Prefab路径：

```text
Assets/_Game/Art/VFX/vfx_slash.prefab
```

生产链路：

```text
Stances.strokeTrailStyleId
→ StrokeTrailStyles
→ VfxCues.vfx_slash
→ AssetManifest.vfx_slash
→ AssetRegistry.vfx_slash
→ StrokeTrailView
```

Prefab只保存稳定组件拓扑；颜色、宽度、排序、寿命和电弧形态由运行时配置覆盖。

### 5.2 只做一个新的外观样式

适用于仍使用“三层主线+稀疏电弧”的画笔。

1. 在权威工作簿的`StrokeTrailStyles`新增一行，给出新的稳定`styleId`。
2. 填写：

| 字段 | 作用 |
|---|---|
| `outerColorHex` | 外层辉光色，格式`#RRGGBBAA` |
| `bodyColorHex` | 主体色 |
| `coreColorHex` | 核心色 |
| `outerWidthMultiplier` | 外层相对基础笔宽 |
| `bodyWidthMultiplier` | 主体相对基础笔宽 |
| `coreWidthMultiplier` | 核心相对基础笔宽 |
| `branchColorHex` | 电弧色 |
| `branchSpacingRefPx` | 电弧平均间距，参考像素 |
| `branchLengthRefPx` | 电弧长度，参考像素 |
| `branchJitterRefPx` | 电弧折线抖动，参考像素 |
| `branchWidthMultiplier` | 电弧相对基础笔宽 |
| `branchSegmentCount` | 每条电弧分段数 |

3. 在目标`Stances`行把`strokeTrailStyleId`改为新`styleId`。
4. 不要复制`Stances.strokeWidthRefPx`到样式表；它仍是架势基础笔宽。
5. 运行配置`--update`和只读门。
6. 不需要重建 Prefab；进入真实 Battle 分别检查刀/符架势。

技术边界：

- 颜色必须为`#RRGGBBAA`；
- 三层宽度倍率范围为`0.01–4`；
- 电弧间距范围为`1–1920`参考像素，长度为`1–500`，抖动为`0–200`；
- 电弧宽度倍率范围为`0.01–1`，分段数为`2–8`；
- 当前最多显示12条电弧；
- 分支只负责显示，不参与命中、伤害、能量或手势识别。

### 5.3 创建或修复当前方案C Prefab

在 Unity 中运行：

```text
One Stroke Demon/T698/Create Lightning Stroke Trail Prefab
```

该工具会生成：

```text
vfx_slash
├─ LineRenderer（外层，位于根）
├─ VfxPoolItem
├─ StrokeTrailView
├─ Body
│  └─ LineRenderer
├─ Core
│  └─ LineRenderer
├─ Compatibility Sprite
│  └─ SpriteRenderer（默认关闭，仅兼容通用资源合同）
└─ Branches
   ├─ Branch 01
   │  └─ LineRenderer
   ├─ ...
   └─ Branch 12
      └─ LineRenderer
```

运行成功后，工具会保存 Prefab、验证 Registry并选中产物。

如果要手动搭建同样结构：

1. 只能在 Unity Editor 中创建，不编辑 YAML。
2. 根节点必须同时有外层`LineRenderer`、`VfxPoolItem`和`StrokeTrailView`。
3. Body/Core及12条Branch必须是子节点`LineRenderer`。
4. 所有Renderer默认关闭、`positionCount=0`、`useWorldSpace=true`、`loop=false`。
5. 使用`StrokeTrailView.ConfigureRenderersForAuthoring`写入引用，不能留下空数组。
6. 不要在材质、颜色或宽度字段中复制工作簿数值。

实际维护优先运行作者菜单；手动搭建只用于理解结构或开发新的幂等作者工具。

### 5.4 新增完全不同的画笔技术方案

粒子、Shader拖尾、贴图印章或网格笔刷不属于“加一行配置”：

1. 先明确是否继续复用`vfx_slash`稳定键。
2. 新建幂等 Editor 作者入口，通过Unity序列化API生成组件和Prefab。
3. 若资源键、类型或路径变化，先改`AssetManifest`和引用它的提示表。
4. 运行时仍应读取配置，不把颜色、尺寸、生命期写死在Prefab。
5. 添加纯规则EditMode、池化PlayMode和真实相机截图。
6. 不得让新特效改变T340笔迹点、命中或伤害链。

## 6. 主角制作与导入

### 6.1 当前主角合同

- 稳定键：`char_moyan_idle`
- Prefab：`Assets/_Game/Prefabs/Actors/PlayerMoyan.prefab`
- 待机图集：`Assets/_Game/Art/Characters/Animated/Moyan/moyan_idle_sheet.png`
- 攻击图集：`Assets/_Game/Art/Characters/Animated/Moyan/moyan_attack_sheet.png`
- Controller：`Assets/_Game/Art/Characters/Animated/Moyan/PlayerMoyan.controller`
- Characters Atlas：`Assets/_Game/Art/SpriteAtlases/Characters.spriteatlasv2`
- 默认状态：`Idle`
- 攻击参数：Trigger `Attack`
- 当前样例：待机9帧循环、攻击12帧非循环、12 FPS、100 PPU、脚底Pivot。

普通有效笔势只触发攻击动画表现。Animator帧、Clip长度和Animation Event不能决定命中、伤害、能量或教程进度。

如果只想把当前主角换成一张静态图，最小改法是保持`char_moyan_idle`仍为Prefab类型和原路径，在Unity中生成只含`SpriteRenderer`的`PlayerMoyan.prefab`并改绑同一稳定键；不要创建空动画状态机冒充动画。当前生产入口只支持这一个玩家身份，新增第二名可选玩家不是单纯资源导入，需要独立玩法和入口任务。

### 6.2 推荐：计划驱动的动画导入

适合帧数、尺寸、来源路径或状态结构发生变化的正式替换。

1. 把待机/攻击PNG和帧JSON放在一个批次目录。
2. 让每个动画条目填写同一个现有`assetKey=char_moyan_idle`；只有待机身份负责`bindRegistry=true`。
3. 先对整个批次做预检，输出规范化 plan。
4. 若`AssetManifest`从Sprite升级为Prefab，先改工作簿并导出。
5. 运行一个读取规范化 plan 的幂等 Editor 作者工具。
6. 作者工具一次完成：
   - 设置Multiple Sprite导入；
   - 按JSON转换坐标并按Sprite名保留既有GUID；
   - 生成或更新Idle/Attack Clip；
   - 生成含`Attack` Trigger的Controller；
   - 生成`SpriteRenderer + Animator` Prefab；
   - 重建Characters Atlas；
   - 绑定`char_moyan_idle`；
   - 最后验证Registry。

使用 Codex 执行时，可以直接说明“使用`unity-import-sprite-animations`流程导入这批主角动画”，并提供PNG、帧JSON、目标键和状态说明。Codex会先生成批次清单与预检计划，再通过 Unity MCP或Editor作者入口生成Unity资产。

### 6.3 当前 T694 菜单的用途和限制

```text
One Stroke Demon/Art/Create or Repair T694 Moyan Animations
```

它严格服务于当前已审批的“9帧待机+12帧攻击”批次，并读取：

```text
artifacts/evals/T694/animation-import-plan.json
```

该计划保存了历史机器的源素材绝对路径。换电脑或导入新素材时，不能把它当通用一键导入器直接运行；应先重新生成规范化计划，并同步更新或新增项目内作者工具。目标Unity路径、稳定键和状态合同仍可复用。

### 6.4 纯手动 Unity 创建

只在没有现成作者工具、且改动很小的情况下使用：

1. 将PNG和JSON放到`Assets/_Game/Art/Characters/Animated/<角色名>/`。
2. 在Texture Import Settings设置Multiple Sprite、100 PPU、透明、无MipMap、Clamp、Bilinear。
3. 在Sprite Editor按JSON坐标切片，按`角色_状态_001`自然编号。
4. 给脚底型角色设置Pivot`(0.5,0.08)`。
5. 创建Idle循环Clip；循环动画在`frameCount / fps`追加首帧，保证最后一帧显示完整。
6. 创建Attack非循环Clip；末尾重复最后一帧一个采样间隔。
7. 创建Controller：
   - Idle为默认状态；
   - 参数`Attack`类型为Trigger；
   - Any State到Attack，无退出时间、0过渡；
   - Attack到Idle，有退出时间、0过渡。
8. 创建`PlayerMoyan` Prefab，只放`SpriteRenderer`和`Animator`，`applyRootMotion=false`，Sorting Layer为Actors。
9. 更新Characters Atlas。
10. 在Registry中把`char_moyan_idle`绑定到Prefab并验证。

手动流程仍不能手改`.meta`、`.anim`、`.controller`或`.prefab`文本。

## 7. 敌人制作与导入

### 7.1 先区分“换皮”和“新增敌人”

#### 替换现有敌人美术

保留现有`enemyId`和`assetKey`，通常不改变HP、移动、攻击、弱点、波次或文案。

- 同类型Sprite换Sprite：导入新图，改绑同一个Registry键。
- 同类型Prefab换Prefab：生成新Prefab，改绑同一个Registry键。
- Sprite升级为动画Prefab：工作簿的`AssetManifest.assetType/addressOrPath`必须先改为Prefab和目标路径。

#### 新增玩法敌人

至少检查和补齐：

- `Texts`：显示名；
- `Enemies`：`enemyId`、`assetKey`、HP及策略引用；
- `MovePatterns`；
- `EnemyAttacks`及其`attackSetId`分组；
- `DefenseRules`；
- `WeakpointRules`；
- `AssetManifest`；
- 需要出场时的`Spawns`及对应关卡/波次。

不要在Prefab中直接挂一份HP、速度或伤害作为第二数值库。当前通用敌人池会在运行时根据`Enemies`和策略表补齐逻辑组件。

### 7.2 静态敌人

推荐目录：

```text
Assets/_Game/Art/Enemies/<enemy_name>.png
```

导入规则：

- Sprite Mode=Single；
- 100 PPU；
- 脚底Pivot默认`(0.5,0.08)`；
- Sorting Layer=Actors；
- 进入`Enemies.spriteatlasv2`。

如果配置的`AssetManifest.assetType=Sprite`，Registry可以直接绑定Sprite。
如果配置期待Prefab，则在Unity中创建只含表现组件的Actor Prefab，并绑定Prefab。

通用原型菜单：

```text
One Stroke Demon/Art/Create or Repair T630 Prototype Assets
```

注意：T630不是任意新敌人的万能Prefab生成器。它会：

- 配置非`/Animated/`PNG导入；
- 重建五类Atlas；
- 为当前受管的魂偶、Boss和通用VFX生成既定Prefab；
- 按当前AssetManifest路径重新绑定Registry。

新敌人没有对应代码映射时，需要新增幂等作者入口或在Unity中手动创建Prefab。

### 7.3 动画敌人

推荐目录：

```text
Assets/_Game/Art/Enemies/Animated/<EnemyName>/
Assets/_Game/Prefabs/Actors/<EnemyPrefab>.prefab
```

使用第3.2节的PNG+帧JSON输入和第6.2节的计划驱动流程。默认12 FPS、100 PPU、Actors层；是否循环、Pivot和状态结构由每个条目明确声明。

当前火鱼示例：

- 稳定键：`enemy_fire_fish`
- 输入：`Assets/_Game/Art/Enemies/Animated/FireFish/fire_fish_idle_sheet.png`
- 帧JSON：同目录`fire_fish_idle_sheet.frames.json`
- 产物：`Assets/_Game/Prefabs/Actors/EnemyFireFish.prefab`
- 菜单：

```text
One Stroke Demon/Art/Create or Repair T690 Fire Fish Animation
```

T690是当前可直接复跑的固定9帧、3×3、256×256、12 FPS火鱼工具。新敌人的帧数或切片不同，不要通过伪造3×3素材套用T690；应生成新计划并复用其代码模式。

### 7.4 怪物死亡动画

当前合同：

- 稳定键：`vfx_enemy_death`
- 11帧、12 FPS、非循环；
- Prefab：`Assets/_Game/Art/VFX/Animated/EnemyDeath/vfx_enemy_death.prefab`
- Sorting Layer=VFX；
- Prefab含`SpriteRenderer + Animator + VfxPoolItem`；
- 寿命、预热、是否跟随、排序、染色、显示尺寸、震屏和震动来自工作簿。

当前菜单：

```text
One Stroke Demon/Art/Create or Repair T695 Enemy Death VFX
```

它读取`artifacts/evals/T695/animation-import-plan.json`并校验源文件hash。该计划同样包含历史机器绝对路径；在新机器或换新素材时先重新预检和生成计划，不要删除hash校验来强行运行。

池化检查：

1. 每次租用从默认状态首帧开始；
2. 播放完不从上一次末帧继续；
3. `followTarget=false`时固定在死亡瞬间位置；
4. 敌人本体回收后VFX仍能播放；
5. VFX不参与死亡判定、伤害、掉落或计分。

## 8. Unity菜单速查

| 菜单 | 用途 | 备注 |
|---|---|---|
| `One Stroke Demon/Art/Create or Repair T630 Prototype Assets` | 通用静态资源、Atlas和既定Prefab | 必须先跑，会覆盖非动画通用VFX |
| `One Stroke Demon/Art/Create or Repair T690 Fire Fish Animation` | 当前火鱼九帧循环动画 | 固定合同，可直接复跑 |
| `One Stroke Demon/Art/Create or Repair T694 Moyan Animations` | 当前主角待机/攻击动画 | 先处理历史绝对源路径 |
| `One Stroke Demon/Art/Create or Repair T695 Enemy Death VFX` | 当前怪物死亡动画 | 先处理历史绝对源路径和hash |
| `One Stroke Demon/T698/Create Lightning Stroke Trail Prefab` | 当前方案C画笔Prefab | 专用工具中最后运行 |
| `One Stroke Demon/Art/Create or Repair T692 Global Light Coverage` | 修复Actors层2D全局光覆盖 | 角色显示全黑时使用 |
| `One Stroke Demon/Config/Create or Repair Asset Registry` | 补齐Registry键和占位 | 不会猜测新资源绑定 |
| `One Stroke Demon/Config/Validate Asset Registry` | 检查键、类型、持久化和Scene | 每批最后运行 |

## 9. 手动操作与 Unity MCP 是什么关系

两种方式生成的是同一套Unity资产：

- **用户手动：** 在Unity里点击上述菜单，或通过Inspector/Sprite Editor完成明确的小改动。
- **Codex + Unity MCP：** Codex先做素材和配置预检，再让Unity执行同一个项目作者方法、刷新编译、运行测试并截取真实相机画面。

Unity MCP是执行Unity主线程操作和收集证据的通道，不是新的资源格式，也不替代工作簿、作者工具或Registry。MCP不可用时，不得退回到手改Unity YAML；如果当前验收明确需要Unity，就应等待Editor/MCP恢复或由用户手动执行同一菜单。

## 10. 验证流程

### 10.1 Unity Editor内

1. 等待右下角导入和编译完成。
2. Console确认没有新的Error、Exception、Assert或Warning。
3. 运行`One Stroke Demon/Config/Validate Asset Registry`。
4. 打开目标Prefab检查Sprite、Controller、Sorting Layer和默认状态。
5. 从Bootstrap进入MainMenu，再进入真实Battle；不要只看Prefab预览。
6. 角色在Actors层必须被Global Light 2D照亮。
7. 检查1920×1080参考空间下的尺寸、层级、Pivot、动画首尾和池化复用。

### 10.2 专项测试

日常手动检查可在Unity中打开`Window > General > Test Runner`，按分类分别运行：

```text
EditMode：T690, T694, T695, T698, AssetImport
PlayMode：T690, T694, T695, T698
```

使用Codex时，可让Unity MCP运行相同分类并保存结构化结果。
需要批处理XML证据时，先关闭图形界面的Unity实例；在`.sh`保持LF的Bash工作树中执行：

```bash
Tools/CI/run-unity-tests.sh \
  --mode EditMode \
  --category T690,T694,T695,T698,AssetImport

Tools/CI/run-unity-tests.sh \
  --mode PlayMode \
  --category T690,T694,T695,T698
```

Windows CRLF工作树使用Unity Test Runner/MCP，或直接以Unity命令行参数`-runTests -testPlatform -testCategory -testResults`执行，不调用已被换行转换的shell脚本。只改某一类资源时可只跑对应分类；任务收尾仍应按影响范围运行全量EditMode/PlayMode。正式证据必须产生非空NUnit XML或等价的MCP结构化结果，不能只看Unity进程退出码。

### 10.3 目视验收

至少保存：

- 主角Idle和Attack代表帧；
- 每类新敌人的默认状态和关键动作；
- 怪物死亡VFX的首、中、末代表帧及回收后表现；
- 刀/符架势下画笔的短划、长划和淡出；
- 真实Battle相机画面，不用Scene视图或孤立Prefab截图代替。

## 11. 常见问题

| 现象 | 常见原因 | 处理 |
|---|---|---|
| 闪电画笔变回一张弧形贴图 | T698后又运行了T630 | 最后重跑T698，再验证Registry |
| 运行T694/T695提示源文件不存在 | 归档plan仍是历史机器绝对路径 | 重新生成规范化plan，不要删除校验 |
| Sprite帧上下颠倒 | 直接用了JSON左上坐标 | 按`textureHeight-y-h`转换到Unity左下坐标 |
| 第10帧跑到第2帧前 | 用普通字符串排序 | 使用自然数字排序和三位帧名 |
| 动画最后一帧一闪而过 | Clip末尾没有重复首帧/末帧 | 在`frameCount/fps`补一个采样键 |
| 角色在Battle里全黑 | Actors层没有Global Light 2D覆盖 | 运行T692并检查真实场景，不改成Default/Unlit掩盖 |
| Registry验证报缺键 | 工作簿新增AssetManifest后Registry没补齐 | 先Create or Repair，再明确绑定目标对象 |
| Registry验证通过但仍显示占位 | 键存在但仍指向同类型占位 | 打开Registry检查目标键的实际对象 |
| 新Prefab在运行时没有HP或攻击组件 | 视觉Prefab本来只保存表现 | 检查`Enemies`配置和运行时原型池，不把数值塞回Prefab |
| 换图后Atlas仍显示旧内容 | Atlas没有在整批末尾重建 | 重建对应Atlas并等待导入完成 |
| 动画对象池从旧末帧继续 | Animator没有在租用时Rebind/采样默认状态 | 复用`VfxPoolItem`/现有池化合同并跑PlayMode |
| 手动改Prefab后Git出现难读YAML漂移 | 用Inspector复制了数值或误改引用 | 回到幂等作者工具；逐项审查，不手改文本 |
| 配置JSON看起来对但启动失败 | 手改生成物或hash/ID漂移 | 恢复只改工作簿，再运行`verify-config.sh --update` |

## 12. 每批内容的交付检查表

### 输入

- [ ] 来源、授权和hash已登记。
- [ ] PNG可解码，透明通道正确。
- [ ] 动画PNG与帧JSON成对，尺寸和帧矩形预检通过。
- [ ] 已确认是替换现有稳定键，还是新增内容。

### 配置

- [ ] 只修改了`Design/Config/GameConfig.xlsx`。
- [ ] 需要时同步了工作簿镜像、FieldDictionary和Schema。
- [ ] `AssetManifest`类型/路径与目标Unity产物一致。
- [ ] 没有把玩法或表现数值复制到Prefab/Inspector。
- [ ] 生成物由导出器统一更新，只读配置门通过。

### Unity资产

- [ ] Unity Editor/MCP完成导入，没有手改YAML或`.meta`。
- [ ] Sprite切片顺序、PPU、Pivot、透明、压缩正确。
- [ ] Clip的FPS、循环、首尾采样正确。
- [ ] Controller状态、Trigger和返回状态正确。
- [ ] Prefab只包含必要表现组件和资源引用。
- [ ] 对应Atlas只重建一次且包含所有目标纹理。
- [ ] Registry稳定键指向正确类型和正确对象。
- [ ] 若运行过T630，专用动画和T698已按顺序重跑。

### 验收

- [ ] Console没有新增警告或错误。
- [ ] Registry验证通过。
- [ ] 专项EditMode/PlayMode通过且结果非空。
- [ ] 受影响范围的全量回归通过。
- [ ] 真实Battle相机和真实光照下目视通过。
- [ ] 池化复用、淡出、死亡位置和动画重播没有残留。
- [ ] 最终Git差异只包含本批白名单路径。

## 13. 进一步参考

- [资源输入、命名和Importer合同](ART_PIPELINE.md)
- [当前Registry与资源接入状态](ASSET_INTEGRATION.md)
- [配置唯一真相源和导出命令](CONFIG_PIPELINE.md)
- [配置表、外键和T694/T695/T698语义](CONFIG_SCHEMA.md)
- [资源来源登记](ASSET_SOURCES.md)
- [测试与证据流程](WORKFLOW.md)
- 火鱼作者工具范例：`Assets/_Game/Scripts/Editor/Art/T690FireFishAnimationAuthoring.cs`
- 主角作者工具范例：`Assets/_Game/Scripts/Editor/Art/T694MoyanAnimationAuthoring.cs`
- 死亡VFX作者工具范例：`Assets/_Game/Scripts/Editor/Art/T695EnemyDeathVfxAuthoring.cs`
- 画笔作者工具：`Assets/_Game/Scripts/Editor/T698StrokeTrailVfxAuthoring.cs`
- Registry作者工具：`Assets/_Game/Scripts/Editor/AssetRegistry/`
