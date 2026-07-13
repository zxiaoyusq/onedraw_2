# T350 Static Audit

- Unity 6000.5.1f1反射确认`Physics2D.CircleCast`的`ContactFilter2D + RaycastHit2D[] + distance`数组重载可用且未弃用；其扫圆路径形成分段胶囊并提供命中fraction。
- Runtime依赖保持Combat→Core/Config/Input，未新增Actors反向依赖或程序集环；测试沿用既有Combat/Config/Input/Presentation引用。
- 7个新增Runtime C#、2个新增测试C#和T350目录均有Unity生成`.meta`配对；asmdef保持合法JSON。
- 新增Runtime代码无LINQ、闭包、`Task.Run`、托管线程、`new GameObject`、动态Collider或逐段集合；查询/目标/排序缓冲均在构造时固定分配。
- 配置只读检查：三生成物逐字节diff PASS，ConfigExporter构建0 warning/0 error，.NET 54/54；Unity配置测试由最终全量回归覆盖。
- 配置内容、Excel、FieldDictionary、Schema、导出器、JSON/hash/ConfigIds、DTO、Input Actions、Packages、ProjectSettings、场景、Prefab和微信SDK差异均为0。
- `git diff --check`和暂存白名单审查通过；任务总览与详情仅T360一个READY，其依赖T350/T230均DONE。
