# T695 验证记录

- 素材预检：批次1/1 PASS。源图1024×768、RGBA，共11个256×256有效帧；按4×3网格识别并忽略最后一个空格，未旋转、未裁边。
- 来源保真：项目内PNG SHA-256为`07ad8828fb806e6b3948e0ac3e061788cee7f7be00e371993de2ed011985452a`，逐帧JSON SHA-256为`a647b539e31d2ff4a755127b12895e7f10cccb8320d7b44d632aabf47efaeae2`；原始外部目录记录在批次清单。
- Unity作者工具：幂等执行输出`T695_ENEMY_DEATH_VFX_AUTHORING_PASS frames=11 fps=12 loop=False`，完成切片、Clip、Controller、Prefab、VFX图集重建和Registry绑定。
- 资源结构：Sprite为Multiple、中心枢轴、12 FPS；`EnemyDeath.anim`不循环，Animator默认状态为`Play`；Prefab位于VFX排序层并由配置控制显示顺序和生命周期。
- 运行时语义：生产击杀路径在实体释放前快照死亡位置并发布`EnemyDeath`反馈；`followTarget=false`，不依赖释放后的Transform；VFX池预热6个，复用时通过Animator Rebind从首帧重新播放。
- 配置绑定：新增`vfx_enemy_death`和`feedback_enemy_death`稳定键；Registry共77项，其中43 Prefab、16 Sprite、17 AudioClip、1 Scene。
- 配置门：schema 5、content `0.6.5-sample`、hash `9cc48fcb5f3b45cff68dd0bfc09cf533d808b26cc956553bc5b060cfa5113abb`、29表、748记录；ConfigExporter 58/58、ConfigPipeline EditMode 19/19、PlayMode 3/3、生成物漂移0。
- 工作簿：权威源与镜像SHA-256均为`64c78fda5c3d4bb6b5c2131c818b6a7b85153a33243410b1dd34978ed1fbc8c3`，相关工作表已渲染目检。
- T695 EditMode：1/1 PASS，覆盖切片、导入设置、Clip、Controller、Prefab、配置、图集与Registry合同。
- T695 PlayMode：2/2 PASS，覆盖6个预热对象复用及首帧复位，以及真实致死笔势在敌人释放前发布死亡VFX。
- 受影响专项：T620/T695 EditMode 5/5 PASS；AssetImport 5/5 PASS。
- 全量回归：EditMode 206/206 PASS；PlayMode 55/55 PASS。最终Unity控制台0 Error、0 Warning。
- 视觉证据：Metal真实Camera与测试专用Global Light 2D的1920×1080截图`enemy-death-vfx-gallery.png`，展示死亡动画由浓烟扩散至环形消散的六个时间点；SHA-256为`5194a40b37a98c408e7ed49e4790357feb476a65306575fd5db03ef3e023a3a4`。
- 测试框架插曲：一次PlayMode初始化在Unity Test Framework内部因外部ProjectSettings同步触发空引用，未启动产品测试，已明确作废；清理残留测试任务并恢复Bootstrap后，专项和全量均干净通过。
- 用户既有改动：`Design/Config/~$GameConfig.xlsx`删除状态完整保留且不纳入T695提交。
