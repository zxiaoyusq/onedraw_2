# T690 验证记录

- Unity 作者工具：`T690_FIRE_FISH_AUTHORING_PASS frames=9 fps=12 prefab=Assets/_Game/Prefabs/Actors/EnemyFireFish.prefab`
- 资源结构：3×3 图集切为 9 个 256×256 Sprite；`FireFishIdle` 12 FPS 循环；Prefab 含 `SpriteRenderer` 与 `Animator`。
- 配置绑定：`enemy_fire_fish` 保持稳定键，`AssetManifest` 类型改为 `Prefab`，Registry GUID 与 `EnemyFireFish.prefab` 一致。
- 配置生成物只读校验：PASS；schema 5、content 0.6.3-sample、hash `0cf75f9d11b2db5311d2910a35b38cbc0500709833723ad8086fb19f34f75d81`、29 表、745 记录。
- ConfigExporter：58/58 PASS；生成器在 Windows/Linux 均固定 LF。
- T690 EditMode：1/1 PASS。
- T690 PlayMode：1/1 PASS；真实对象池实例换帧并可正常回收。
- 全量 EditMode：199/199 PASS。
- 全量 PlayMode：51/51 PASS。
- 最终 Unity 重编译：Tundra build success，0 error，0 warning。
- 工作簿：权威源、镜像和交付副本 SHA-256 均为 `12BD99F7602B01365C9E60230AED69D7D4269B2609F1A7B862BBBDBC90EC375D`。
- 用户既有改动：`ProjectSettings/ProjectSettings.asset` 与 `ProjectSettings/UnityConnectSettings.asset` 保留且不纳入 T690 提交。
