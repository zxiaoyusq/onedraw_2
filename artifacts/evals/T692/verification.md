# T692 验证记录

- 根因：`EnemyFireFish.prefab`使用`Actors` Sorting Layer与URP 2D `Sprite-Lit-Default`材质；Bootstrap、MainMenu、Battle的Global Light 2D此前只覆盖`Default`，未被照亮的Lit Sprite因此显示为全黑剪影。
- 修复：通过Unity Editor API将三个场景的Global Light 2D统一覆盖Background、Default、Actors、Projectiles、VFX全部5个项目Sorting Layer；火鱼贴图、动画、Prefab、Registry、配置及玩法代码保持不变。
- 场景差异：三个场景均只修改`m_ApplyToSortingLayers`一项。
- T692 EditMode：4/4 PASS，覆盖三个场景的灯光层列表，以及火鱼保持`Actors`层和Lit材质。
- T692 PlayMode：1/1 PASS，Battle运行时全局光包含`Actors`层。
- 全量 EditMode：203/203 PASS。
- 全量 PlayMode：52/52 PASS（干净域重载后单独执行）。
- 全量 PlayMode首轮连续接在专项PlayMode之后运行时为43/52，9条既有测试都由Unity Input System编辑器状态断言`Map index out of range in ProcessControlStateChange`造成；无T692失败。干净域重载后相同全量集合52/52通过，判定为测试运行环境串扰而非产品回归。
- 目视验证：Unity相机直接渲染`Actors`层火鱼，输出`fire-fish-lighting-verification.png`，鱼身、衣物与火焰恢复源图颜色，不再是黑色剪影。
- 最终重编译：Tundra PASS；清空Console后无产品Error/Exception/Assert，仅有本机Visual Studio集成UDP端口占用warning。
- 用户既有改动：`ProjectSettings/ProjectSettings.asset`、`ProjectSettings/UnityConnectSettings.asset`与状态型`ProjectSettings/QualitySettings.asset`均未纳入；未跟踪`Assets/Resources.meta`继续保留且未提交。
