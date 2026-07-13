# ASSET_INTEGRATION：PSD素材接入计划

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

## Unity导入规则

- 不把PSD原文件放入Runtime Assets，只导出经确认的透明PNG。
- 统一命名：`spr_chr_*`、`spr_enemy_*`、`spr_bg_*`、`ui_*`、`vfx_*`。
- PPU、Pivot、Max Size、Compression和Filter Mode由资源类型预设控制。
- 角色敌人、UI、背景、VFX分别进入SpriteAtlas。
- 原型可用单帧位移、缩放、闪白表达动作；正式动画另开拆件任务。
- 资源通过 `AssetRegistrySO` 的 `assetKey` 引用，配置表不写路径或GUID。

## T240 Registry基线

- Canonical资源：`Assets/_Game/Config/Registry/AssetRegistry.asset`。
- 当前覆盖：76个AssetManifest键，其中Prefab 40、Sprite 18、AudioClip 17、Scene 1；Editor菜单和构建前门会拒绝空、重复、缺失、额外、错型或非持久化引用。
- Registry条目只序列化`assetKey`和Unity对象；Scene使用`AssetSceneReference`保存明确场景引用。HP、CD、伤害、冷却、关卡和文案等仍只来自配置表。
- Runtime和Editor绑定不消费AssetManifest的`addressOrPath`，Prefab/Sprite/Audio不通过路径或GUID查找。资源文件移动或替换不要求修改配置ID。

## 占位与替换流程

当前尚未导入正式美术、音频和逐对象Prefab，因此所有Sprite键复用一个洋红占位Sprite、所有AudioClip键复用一个静音占位AudioClip、所有Prefab键复用一个含占位SpriteRenderer的Prefab；`scene_battle`直接引用Build Settings中的Battle场景。这些资源只用于建立完整类型和覆盖合同，不代表正式表现完成。

替换单项资源时：

1. 按本文件的命名、导入预设和授权要求导入正式Unity资产。
2. 在Canonical Registry中把对应稳定`assetKey`的对象引用替换为同类型资产；不要修改Excel中的配置ID，也不要新增路径/GUID绑定。
3. 运行 `One Stroke Demon/Config/Validate Asset Registry`；缺失或错型引用会立即失败，正式构建也会执行同一门禁。
4. 可再次运行 `One Stroke Demon/Config/Create or Repair Asset Registry` 补齐新键；工具会保留已有合法类型引用，只为缺失或错型项回填占位。
