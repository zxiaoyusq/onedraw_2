# ART_PIPELINE：T630 原型位图接入合同

## 1. 输入与来源门

- 原始PSD/PSB、生成平台工程文件和未确认授权的素材不得进入`Assets/`。
- 每批输入必须先在`docs/ASSET_SOURCES.md`登记来源、作者/生成工具、取得日期、原始文件SHA-256、授权范围和审核状态。
- `source_status`不是`APPROVED_PROTOTYPE`或`APPROVED_RELEASE`的图层不得导出到Runtime；无法追溯的生成图不得仅凭视觉相似性视为已授权。
- Runtime只接收RGBA透明PNG。背景可以完全不透明，但文件仍须使用PNG且不得依赖PSD运行时解析。
- 单张完整角色只作为单帧Sprite；不得伪造身体拆件、骨骼或不存在的动画帧。

## 2. 命名与目录

| 类别 | 目录 | 文件名合同 | Atlas |
|---|---|---|---|
| 背景 | `Assets/_Game/Art/Backgrounds/` | `bg_*.png` | `Backgrounds.spriteatlasv2` |
| 主角 | `Assets/_Game/Art/Characters/Moyan/` | `moyan_*.png` | `Characters.spriteatlasv2` |
| 敌人/Boss | `Assets/_Game/Art/Enemies/` | 与AssetManifest稳定键对应的`fire_fish.png`等小写蛇形名 | `Enemies.spriteatlasv2` |
| HUD/UI | `Assets/_Game/Art/UI/` | `button_*`、`hud_*`、`icon_*` | `UI.spriteatlasv2` |
| 技能图标/投射物 | `Assets/_Game/Art/Sprites/` | `icon_*`、`proj_*` | 图标进`UI.spriteatlasv2`，投射物进`VFX.spriteatlasv2` |
| VFX源Sprite | `Assets/_Game/Art/VFX/Sprites/` | `vfx_*.png` | `VFX.spriteatlasv2` |

- 文件名使用小写ASCII和下划线；Registry继续使用配置中的稳定`assetKey`，文件重命名不得改变玩法ID。
- VFX Prefab保留配置`AssetManifest`约定的`vfx_*.prefab`名；角色Prefab使用既有PascalCase目标路径。Atlas按明确文件集合打包，不把整个`Art/UI`目录连同字体资源作为packable。
- 若最终文件路径与`AssetManifest.addressOrPath`不同，必须在同一任务同步正式工作簿及完整配置闭环，不能留下陈旧路径记录。

## 3. Unity导入预设

所有PNG统一：`Texture Type=Sprite (2D and UI)`、`Sprite Mode=Single`、sRGB开启、Alpha Source取输入、`Alpha Is Transparency=true`、MipMap关闭、Read/Write关闭、NPOT不缩放、Wrap=Clamp、Filter=Bilinear。

| 类别 | PPU | Pivot | Mesh | Max Size | 默认压缩 | Sorting Layer |
|---|---:|---|---|---:|---|---|
| 背景 | 100 | Center `(0.5,0.5)` | Full Rect | 4096 | Compressed HQ | Background |
| 主角/敌人/Boss | 100 | Feet `(0.5,0.08)`，可按透明边界微调并记录 | Tight | 2048 | Compressed HQ | Actors |
| UI | 100 | Center `(0.5,0.5)` | Full Rect | 2048 | Compressed HQ | UI/Canvas |
| 投射物 | 100 | Center `(0.5,0.5)` | Tight | 1024 | Compressed HQ | Projectiles |
| VFX | 100 | Center `(0.5,0.5)` | Tight | 1024 | Compressed HQ | VFX |

- 本任务只冻结默认导入值；平台纹理格式、内存与包体收敛属于T730，不能在没有设备证据时宣称最终优化。
- 角色脚底Pivot的例外必须按资源路径记录，不能用Inspector中的第二套玩法坐标补偿。

## 4. Atlas与Prefab

- 角色、敌人、UI、背景、VFX分别进入独立SpriteAtlas；禁用旋转，Padding至少4像素，Atlas不得包含字体Atlas或跨类别重复Sprite。
- Actor Prefab只保存Sprite、渲染层级、必要的视觉子节点与按透明轮廓审核的Collider结构，不保存HP、速度、伤害、CD或其他表内数值。
- 单帧原型动作只允许由运行时状态驱动位移、缩放、翻转和T620闪白；不创建空动画状态机冒充正式动画。
- VFX Prefab必须含可渲染组件，能被T440/T620池完整恢复颜色、缩放、Sorting Layer和Transform。
- Canonical Registry只改绑同类型Unity对象；音频保持T240静音占位直到独立音频来源任务，不纳入T630位图验收。

## 5. 验收门

1. 来源记录全部达到本任务所需的`APPROVED_PROTOTYPE`，输入/输出文件hash可追溯；`APPROVED_RELEASE`仍是发布候选的独立门。
2. Runtime `Assets/`中不存在PSD/PSB；所有目标PNG可解码，角色/UI/VFX含透明像素，背景尺寸和横屏构图有效。
3. Importer、Pivot、PPU、压缩、Max Size、Sorting Layer与本合同一致。
4. 五个Atlas覆盖全部接入Sprite且无跨Atlas重复；Registry不再把目标位图/VFX/角色Prefab指向T240共享占位。
5. 主角、六怪、Boss、两张背景、UI图标、投射物和T620反馈VFX在确定性1920×1080资产画廊中可辨识，无裁切、洋红占位或错误层级；Editor离屏GPU捕获若出现纹理/颜色异常必须标为无效，不得替代可复核的最终RGBA画廊。
6. `AssetImportValidationTests`、Registry门、T450/T600/T620受影响路径和全量EditMode/PlayMode通过；标准Web、微信转换、DevTools与真机因当前用户要求绕过平台门明确记为NOT RUN，不能写PASS。
