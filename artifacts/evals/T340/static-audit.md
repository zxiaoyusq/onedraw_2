# T340 Static Audit

- `project-index.yaml`可解析；任务总览和详情仅T350一个READY，T340为DONE。
- Runtime依赖保持Presentation→Core/Config/Combat/Actors/Levels、Combat→Core/Config/Input，依赖图无环；PlayMode测试程序集只新增Presentation直接引用。
- 5个新增Runtime C#、1个新增PlayMode测试C#和T340目录均有Unity生成`.meta`配对；asmdef均为合法JSON。
- 新增Runtime代码中的LINQ、闭包、`Task.Run`、托管线程、运行时反射、`Screen.dpi`、微信SDK、Physics2D、Collider2D、IHittable和HitRecord均为0；唯一`new GameObject`循环位于一次性池初始化。
- LineRenderer只使用`sharedMaterial`；运行时代码无`new Material`、逐段GameObject、`SetPositions`临时数组或热路径List。
- 配置只读检查：三生成物逐字节diff PASS，ConfigExporter构建0 warning/0 error，.NET 54/54；见`config-verify.log`。
- Unity API核验：6000.5.1f1反射确认LineRenderer位置/宽度/颜色API与Renderer.sharedMaterial可用；项目内无正式轨迹Shader/Material资产，测试使用Unity内置`Sprites/Default`，正式资源注入保留给T630。
- Unity Editor资源检查发现原工程只有Default Sorting Layer；经白名单扩展后由Editor API新增配置引用的`VFX`，复核为`Default:0,VFX:1424202891`。
- `git diff --check`通过；Excel、Schema、导出器、生成JSON/hash/ConfigIds、Input Actions、Packages、场景、Prefab及微信SDK差异为0。
