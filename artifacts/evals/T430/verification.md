# T430 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T430；实现配置驱动的敌人移动、攻击、防御、支援策略注册表、攻击预警及护盾减伤闭环。
- 明确不做：T120/T130微信工具与打包、T440对象池、T450敌人装配、T460 Boss阶段、场景/Prefab/Packages/ProjectSettings/微信SDK变更。
- 分支/提交：`main` / `T430: implement configured enemy strategies`（本任务收尾提交）。
- 任务开始Git基线：`6d074e3f4ca640220e984f97aad428fb061a5b4b`；开始时工作区为空，详见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1` / `onedraw_2@272e911286835fad`，WebGL目标。
- 配置Schema/内容版本/hash：schema `4` / content `0.4.0-sample` / `61ed49c024a655a0d97fea7d95d03b973a636177d9e09df9305b1ddfd77351f2`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增移动/攻击显式注册表、策略运行时、防御映射和Telegraph；扩展敌人Buff减伤；配置新增`DamageReduction`、护盾Buff/效果/文本并升级schema v4；同步工作簿、Schema、受管JSON/hash/IDs、冻结断言、合同文档和专项测试。
- 用户已有改动保护：任务开始无已有改动；Unity测试产生的`ProjectSettings/EditorSettings.asset`临时变化已恢复，未纳入任务diff。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；最终暂存清单仅包含`change-whitelist.md`允许的路径，不含场景、Prefab、Packages、ProjectSettings、微信SDK或T440实现。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity`的静态部分PASS并按设计输出`CONFIG_PIPELINE_PARTIAL_PASS`（.NET 56/56、28表/653条、三生成物与二次生成字节一致）；Unity测试由同版本已打开Editor的MCP独立完成。双工作簿SHA-256均为`d3b281c5d1c3131509a6ffd5416a99d22bd92f58ed9a1c3024941a85353b52e8`，JSON 170,196字节，27组/308个ID常量。
- EditMode XML：专项5/5/0，`editmode-results.xml`；全量122/122/0，`full-editmode-results.xml`。
- PlayMode XML：专项1/1/0，`playmode-results.xml`；全量30/30/0，`full-playmode-results.xml`。
- Console新增Error/Warning：最终强制Refresh/编译后0 Error、0 Warning。

## 玩家与平台证据

- 真实玩家路径和可断言值：PlayMode生成配置`enemy_soul_puppet`与盟友`enemy_skeleton_wisp`，支援攻击在0.8秒Windup期间先打开Telegraph，在`Windup -> Attack`边界只执行一次`fx_puppet_shield`；盟友10点来伤变5，配置3秒到期后10点来伤仍为10，Telegraph关闭。
- 标准Web：NOT RUN；T430不要求构建，既有T100基线未外推为本任务结果。
- 微信转换：NOT RUN；按用户要求继续延期T120/T130。
- DevTools：NOT RUN；按用户要求继续延期，现有环境仍缺工具。
- 真机：NOT RUN；按用户要求继续延期，未伪造设备证据。
- 截图/日志/产物：4份NUnit XML；`workbook-tools/previews/updated/`包含README、Global、Buffs、SkillEffects、Texts和Enums渲染复核；`editmode-unity.log`保留未完成的batch尝试，最终测试均由同版本已打开Editor的Unity MCP执行。

## 结论

- 已知问题：无T430新增已知问题；微信DevTools/真机缺口沿用T120延期状态，不影响本原子任务结论。
- 结论：PASS。
