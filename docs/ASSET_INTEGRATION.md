# ASSET_INTEGRATION：PSD原型素材接入记录

> T630的精确导入、Atlas、来源和验收门以`docs/ART_PIPELINE.md`与`docs/ASSET_SOURCES.md`为准。本文件保留对概念PSD内容的历史识别摘要。

## 已识别内容

- 当前红色幽冥洞穴背景，以及隐藏高精度红洞穴和幽暗森林备选。
- 黄衣灵猫主角，另有隐藏服装版本和动作草图。
- 符火鱼妖、轮车僵妖、石甲龟妖、骷髅幽魂、飞行符蝠；鱼妖有受击拆分状态。
- 生命条、终极、切换、设置按钮；斩击轨迹、烟尘和伤害数字。

## 重要限制

- 主角和大多数怪物是单张完整Sprite，不是可直接用于Spine或DragonBones的身体拆件。
- UI英文和装饰大多已栅格化；动态血条需要重新拆成框、底槽、填充、头像和文本。
- 背景虽有多层加工，但没有干净的远景、中景、前景视差层。
- 轮车僵妖源图层名含生成图标记，正式商用前必须核对来源和授权。
- 用户已在2026-07-14提供目标PSD；原始文件保持在仓库外，只把经来源登记和hash固定的派生PNG接入Runtime。PSD内仍未提供魂偶和镇墓玄甲王的独立角色图，因此二者由用户允许的ImageGen单独补齐，并明确标记为新生成原型图。

## Unity导入规则

- 不把PSD原文件放入Runtime Assets，只导出经确认的透明PNG。
- 命名与配置稳定键对齐：背景`bg_*`、主角`moyan_*`、敌人小写蛇形名、UI `button_*`/`hud_*`/`icon_*`、投射物`proj_*`、VFX `vfx_*`；精确目录与Atlas归属见`docs/ART_PIPELINE.md`。
- PPU、Pivot、Max Size、Compression和Filter Mode由资源类型预设控制。
- 角色敌人、UI、背景、VFX分别进入SpriteAtlas。
- 原型可用单帧位移、缩放、闪白表达动作；正式动画另开拆件任务。
- 资源通过 `AssetRegistrySO` 的 `assetKey` 引用，配置表不写路径或GUID。

## T240 Registry基线

- Canonical资源：`Assets/_Game/Config/Registry/AssetRegistry.asset`。
- 当前覆盖：78个AssetManifest键，其中Prefab 44、Sprite 16、AudioClip 17、Scene 1；Editor菜单和构建前门会拒绝空、重复、缺失、额外、错型或非持久化引用。
- Registry条目只序列化`assetKey`和Unity对象；Scene使用`AssetSceneReference`保存明确场景引用。HP、CD、伤害、冷却、关卡和文案等仍只来自配置表。
- Runtime和Editor绑定不消费AssetManifest的`addressOrPath`，Prefab/Sprite/Audio不通过路径或GUID查找。资源文件移动或替换不要求修改配置ID。

## 占位与替换流程

T630把当时18个Sprite键和40个Prefab键改绑为实际原型资产；T694随后把主角键从单帧Sprite升级为动画Prefab，T695再新增池化怪物死亡动画Prefab。当前40个VFX键均使用独立Prefab，其中`vfx_enemy_death`还包含非循环Animator。T698保持`vfx_slash`稳定键，将其升级为外层/主体/核心加12条预建分支的`LineRenderer`拓扑；T699F新增`vfx_stroke_charge`，由4条环线、8条径向电弧、3组ParticleSystem、默认禁用兼容Sprite和`VfxPoolItem`组成。画笔轨迹与蓄力Prefab分别由轨迹池预热，互不借用Renderer；T630通用作者工具明确保护该专用蓄力Prefab，不会批量覆盖。17个AudioClip键继续复用T240静音占位，`scene_battle`继续引用Build Settings中的Battle场景。当前视觉资产只代表原型品质，生成角色与PSD原画的细节密度仍有差异，也不包含正式骨骼动画或逐对象身体碰撞制作。

替换单项资源时：

1. 按本文件的命名、导入预设和授权要求导入正式Unity资产。
2. 在Canonical Registry中把对应稳定`assetKey`的对象引用替换为同类型资产；不要修改Excel中的配置ID，也不要新增路径/GUID绑定。
3. 运行 `One Stroke Demon/Config/Validate Asset Registry`；缺失或错型引用会立即失败，正式构建也会执行同一门禁。
4. 可再次运行 `One Stroke Demon/Config/Create or Repair Asset Registry` 补齐新键；工具会保留已有合法类型引用，只为缺失或错型项回填占位。
