# T460 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T460；实现配置驱动Boss阶段、连续HP阈值、阶段攻击/速度/护甲/弱点覆盖、进入效果和一次性切换，并完成镇墓玄甲王三阶段运行路径。
- 明确不做：不实现T500关卡时间轴、T510战斗流程、T540完整Boss关胜败回路、T630正式美术或T120/T130微信平台工作；不修改场景、Prefab、Registry、Packages、ProjectSettings或微信SDK。
- 分支/提交：`main` / `T460: implement configured boss phases`（本任务收尾提交）。
- 任务开始Git基线：`2bf7fe1fce829839542dfdb5075d67fea4983c4c`；开始时工作树干净，详见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1` / `onedraw_2@272e911286835fad`，WebGL目标。
- 配置：schema `4` / content `0.5.1-sample` / hash `95c42832e54163b63d14f5fc8510453b4b5551e500909eaa9fdb1069f3f4be4b`。

## 改动审查

- 预计白名单：见`change-whitelist.md`；实现中发现受影响的T230/T240/T250冻结断言和`EnemyStrategyRuntime`后，先补充白名单再收尾。
- 实际改动：双工作簿新增Boss二/三阶段移动模板并更新阶段引用；重生成JSON/hash/ConfigIds/样例；新增纯阶段目录/状态机和运行时控制器；通用敌人定义、伤害、状态、移动及策略工厂支持阶段档案；新增T460测试和合同文档。
- 用户已有改动保护：基线无用户改动；Unity测试临时改动的`ProjectSettings/EditorSettings.asset`已精确恢复，未纳入diff。
- 静态规则：产品Boss阶段源码不存在样例阈值、阶段数值、动画时长猜测、自建线程或微信SDK静态调用，详见`static-audit.md`。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；最终暂存只包含白名单路径，不含场景、Prefab、Registry、Packages、ProjectSettings、Builds或微信SDK。

## 自动验证

- 配置/导出：`Tools/CI/verify-config.sh --skip-unity`静态层PASS；ConfigExporter build 0 warning/0 error，.NET 56/56，28表/662条，三生成物漂移为0。详见`config-verification.md`。
- 工作簿：正式源与镜像SHA-256同为`6c931323...16b1`且字节一致；29个Sheet全部渲染审查，公式错误0，详见`workbook-validation.md`。
- EditMode XML：专项4/4/0，`editmode-results.xml`；全量134/134/0，`full-editmode-results.xml`。
- PlayMode XML：专项1/1/0，`playmode-results.xml`；全量33/33/0，`full-playmode-results.xml`。四份Unity原生XML均通过仓库检查器，job见`unity-mcp-jobs.md`。
- Console：最终清空后强制Refresh全部资产并编译，新增Error=0、Warning=0。

## 玩家与平台证据

- 玩家路径：Boss在HP 1200/804/408三个档位依次应用护甲120/60/0、速度20/32/48、三套攻击及弱点变化；进入效果和落石/封印波/冲撞各执行一次，三类阶段事件均恰好3次。详见`player-path.md`。
- 配置可调：只修改重算hash的内存JSON即可把边界改为0.6、phase2速度改为42、护甲和弱点改为none，产品C#无变化。
- 标准Web：NOT RUN；T460不要求构建，不把T100基线外推为本任务结果。
- 微信转换/DevTools/真机：NOT RUN；按用户要求延期T120/T130，当前缺DevTools/设备的缺口未伪造成PASS。
- 证据：4份NUnit XML、Unity job审计、配置/工作簿/静态/玩家路径记录、两张工作簿渲染图和本验证文档。

## 结论

- 已知问题：T460尚未接入T500波次、T510胜败状态机和T540完整Boss关，正式动画/美术仍属后续范围；平台缺口沿用延期状态。
- 结论：PASS。
