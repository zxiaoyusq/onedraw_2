# T030 Verification

- 日期：2026-07-13。
- Git基线：`fe1c3d92 T020: establish Unity subsystem baseline`，分支`main`；开始时工作树干净。
- 范围：建立目标目录、Runtime/Editor/EditMode/PlayMode程序集边界、三场景骨架和最小场景流；未实现战斗、UI或配置业务，也未开始T040。
- Unity/MCP：恢复并锁定active instance `onedraw_2@272e911286835fad`；项目根和Unity `6000.5.1f1`均与文档一致。
- 目录：Art、Config、Prefabs、Scenes和Scripts目标子目录均由Unity识别并生成meta。
- 程序集：创建Core、Config、Input、Combat、Actors、Skills、Levels、Presentation、Platform、Bootstrap十个Runtime asmdef，以及Editor asmdef；依赖严格匹配`TECH_SPEC.md`且全部`autoReferenced=false`。
- 场景流：`ISceneFlowService`定义主菜单/战斗加载契约，`SceneFlowService`验证Build Settings后异步切换场景，`BootstrapController`启动时进入MainMenu。
- 场景：Bootstrap、MainMenu、Battle均通过Unity MCP创建和保存，没有手改Unity YAML；三个场景均包含正交Main Camera与Global Light 2D，MainMenu/Battle各含可识别灰盒根对象。
- 构建列表：Bootstrap、MainMenu、Battle分别为build index 0、1、2，均启用；原SampleScene资产未删除，只从Build Settings移出。
- 专项EditMode：PASS，1/1；`AssemblyDependencyTests`验证程序集集合、精确直接依赖、autoReferenced和无环。
- 专项PlayMode：PASS，1/1；`SceneFlowSmokePlayModeTests`从Bootstrap自动进入MainMenu，再经接口进入Battle，并检查灰盒、相机和灯光。
- 全量回归：EditMode 4/4 PASS；PlayMode 2/2 PASS，覆盖全部T020与T030测试。
- 真实启动路径：Editor加载Bootstrap并进入Play Mode，实际active scene变为MainMenu，层级含MainMenuGraybox、Main Camera和Global Light 2D。
- 场景校验：Bootstrap、MainMenu、Battle的missing script、broken prefab和总问题数均为0；最终Console Error为0。
- 白名单审查：Unity测试临时修改的`ProjectSettings/EditorSettings.asset`已恢复；最终变更仅在T030白名单内。`git diff --check`通过。
- 证据：`change-whitelist.md`、`editmode-summary.log`、`playmode-summary.log`、`scene-validation-summary.log`。
- 已知边界：微信SDK、Web构建、开发者工具和真机门仍未执行；本任务不声称相关PASS。
- 结论：PASS。T030完成，T040置为READY；未开始T040。
