# T694 验证记录

- 素材预检：批次2/2 PASS。待机768×768、9个256×256帧；攻击1024×768、12个256×256帧；均为RGBA、未旋转、未裁边，按源JSON自然顺序导入。
- 来源保真：外部用户文件先复制到项目内再由Unity读取，四个Runtime副本SHA-256与用户源逐一一致；原始外部目录保存在批次清单的`sourceProvenance`。
- Unity作者工具：最终幂等执行输出`T694_MOYAN_AUTHORING_PASS idleFrames=9 attackFrames=12 fps=12 prefab=Assets/_Game/Prefabs/Actors/PlayerMoyan.prefab`，该次执行无源图压缩警告、编译错误或异常。
- 资源结构：两张图均为Multiple Sprite、100 PPU、脚底枢轴`(0.5, 0.08)`、无Mip、无损源纹理、最大1024；待机12 FPS循环，攻击12 FPS单次播放后返回待机；Prefab使用`Actors`层。
- 配置绑定：`char_moyan_idle`保持稳定键，AssetManifest类型改为`Prefab`并指向`PlayerMoyan.prefab`；Registry为76项，其中42 Prefab、16 Sprite、17 AudioClip、1 Scene。
- 配置门：schema 5、content `0.6.4-sample`、hash `e348bab0eaf2bce5fa21c0588eb53c3b755791d8a537c4d7f29c93110ee6522c`、29表、745记录；ConfigExporter 58/58、ConfigPipeline EditMode 19/19、PlayMode 3/3、生成物漂移0。
- 工作簿：权威源与镜像SHA-256均为`a5fc5a21e29ea4a56e56857233216f5fbdc26ad27cc33d7a868a46c1a7daa286`，公式错误0，README/Global/AssetManifest预览已目检。
- T694 EditMode：1/1 PASS，覆盖切片、枢轴、导入设置、Clip、Controller、Prefab、配置、Registry与旧单帧移除。
- T694 PlayMode：1/1 PASS，从Bootstrap进入真实Battle，待机会换帧，普通有效笔势进入攻击并自动回待机。
- 全量回归：EditMode 205/205 PASS；PlayMode 53/53 PASS。最终日志无`warning CS`、`error CS`或源图压缩警告。
- 视觉证据：Metal/Apple M4图形专项1/1 PASS；真实Battle相机1920×1080输出`moyan-attack-battle.png`（SHA-256 `3d2a22870fccfa5bbfb0a494704972751ea9169d4d00a86bf4403dbfe608a85e`），确认主角在配置背景、2D全局光和实际笔迹下正确显示攻击帧。
- 用户既有改动：`Design/Config/~$GameConfig.xlsx`删除状态完整保留且不纳入T694提交。
