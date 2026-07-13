# T440 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T440；建立敌人、投射物、VFX和伤害数字通用对象池、配置化预热/容量/耗尽、显式租约、完整重置与泄漏检测。
- 明确不做：T120/T130微信工具与打包、T450敌人内容装配、T460 Boss阶段、T500关卡流程、T620反馈编排、场景/Prefab/Packages/ProjectSettings/微信SDK变更。
- 分支/提交：`main` / `T440: implement configured object pools`（本任务收尾提交）。
- 任务开始Git基线：`be5d4f6d0257ac65d559a9151e11ff3a85613b32`；开始时工作区为空，详见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1` / `onedraw_2@272e911286835fad`，WebGL目标。
- 配置Schema/内容版本/hash：schema `4` / content `0.5.0-sample` / `d524ffcda4693c9cb65e5e21d5ab753472a14b2233b2ae670ecc4b81f1251ee8`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增Core池合同/服务、四类配置映射、VFX/伤害数字池项，敌人/投射物接入完整回收；配置新增投射物预热及四类耗尽策略并同步双工作簿、生成物、冻结断言与合同文档；新增T440专项测试。
- 用户已有改动保护：任务开始无已有改动；Unity测试产生的`ProjectSettings/EditorSettings.asset`临时变化已恢复，未纳入任务diff。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；最终暂存清单只包含`change-whitelist.md`允许路径，不含场景、Prefab、Packages、ProjectSettings、微信SDK或T450实现。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity`静态部分PASS（.NET 56/56、28表/660条、三生成物漂移为零）；双工作簿字节一致，详见`config-verification.md`。
- EditMode XML：专项5/5/0，`editmode-results.xml`；全量127/127/0，`full-editmode-results.xml`。job ID与精确结果见`unity-mcp-jobs.md`。
- PlayMode XML：专项1/1/0，`playmode-results.xml`；全量31/31/0，`full-playmode-results.xml`。job ID与精确结果见`unity-mcp-jobs.md`。
- Console新增Error/Warning：最终强制Refresh/编译后0 Error、0 Warning。

## 玩家与平台证据

- 真实玩家路径和可断言值：PlayMode从Bootstrap加载真实配置，预热敌人5、投射物8、VFX 1、伤害数字30，共44个对象；用`enemy_skeleton_ghost`、`proj_ghost_fire`、`vfx_ultimate_prepare`和伤害数字连续3轮生成/污染/击杀/清场/重开。每轮四类对象均复用同一实例，旧敌人事件监听未跨回收触发，HP/护甲/Buff/计数/规则/归属/目标/计时/Collider/Transform/可见态全部归零，活动泄漏为0，generation从1推进到4。
- 标准Web：NOT RUN；T440不要求构建，既有T100基线未外推为本任务结果。
- 微信转换：NOT RUN；按用户要求继续延期T120/T130。
- DevTools：NOT RUN；按用户要求继续延期，现有环境仍缺工具。
- 真机：NOT RUN；按用户要求继续延期，未伪造设备证据。
- 截图/日志/产物：4份原生NUnit XML、Unity MCP四个job记录、配置验证摘要、工作簿修改脚本/审计及README/Global/Enums渲染预览。

## 结论

- 已知问题：无T440新增已知问题；微信DevTools/真机缺口沿用T120延期状态，不影响本原子任务结论。
- 结论：PASS。
