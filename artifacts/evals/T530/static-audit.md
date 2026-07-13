# T530 Static Audit

- 产品运行时代码未新增或修改；`lv_002_cave`、8波/23行/45怪、210秒、星级阈值、敌人组合、出生时间、`maxAlive`和精英波次只存在于正式工作簿及生成物。`Assets/_Game/Scripts`中的相关ID只出现在受管生成的`ConfigIds.g.cs`，没有手写关卡/敌人分支。
- 新增EditMode测试只依赖配置、Actors、Levels和测试夹具，不依赖`MonoBehaviour`；PlayMode测试只负责真实Unity Bootstrap、Actor池与生命周期接线。
- 产品与测试没有使用`Task.Run`或自建托管线程；Web运行时仍只读取生成JSON，不解析xlsx。
- Schema、FieldDictionary、DTO、导出/校验规则、产品C#、场景、Prefab、AssetRegistry、Input Actions、Packages、ProjectSettings和微信SDK均无任务差异。
- 双工作簿字节一致；runtime JSON与sample JSON字节一致；三受管生成物与正式工作簿只读重导结果一致。
- `git diff --check`通过；Unity最终Refresh/编译完成，Console Error 0。
