# T450 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T450；只使用现有配置、T430策略、T440对象池和T240 Registry组合5种普通怪与1种精英怪，建立只读原型目录、通用资源装配与池/策略生命周期。
- 明确不做：不为每怪创建业务子类；不修改配置工作簿/Schema/导出器/受管生成物；不修改场景、Prefab、AssetRegistry、Packages、ProjectSettings或微信SDK；不实现T460/T500/T510/T630。
- 分支/提交：`main` / `T450: assemble configured enemy archetypes`（本任务收尾提交）。
- 任务开始Git基线：`06a68d05a313031419ffdb1afc0f66645b315246`；开始时工作树干净，详见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1` / `onedraw_2@272e911286835fad`，WebGL目标。
- 配置Schema/内容版本/hash：schema `4` / content `0.5.0-sample` / `d524ffcda4693c9cb65e5e21d5ab753472a14b2233b2ae670ecc4b81f1251ee8`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：配置Runtime新增只读敌人枚举；新增`EnemyArchetypeDefinition/Catalog/Actor/Pool`，从表内聚合六怪、检查独立教学特征与前摇、按Sprite/Prefab类型绑定Registry、注册每怪池并在租约内管理策略订阅；新增T450 EditMode/PlayMode测试与合同文档。
- 用户已有改动保护：基线无用户改动；Unity测试临时改动的`ProjectSettings/EditorSettings.asset`已精确恢复，未纳入diff。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；最终暂存只包含白名单路径，不含配置生成物、场景、Prefab、Registry、Packages、ProjectSettings或微信SDK。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity`静态层PASS；ConfigExporter build 0 warning/0 error，.NET 56/56，28表/660条，三生成物漂移为0。详见`config-verification.md`。
- EditMode XML：专项3/3/0，`editmode-results.xml`；全量130/130/0，`full-editmode-results.xml`。四份XML均由Unity原生Test Runner产生并通过仓库检查器，job见`unity-mcp-jobs.md`。
- PlayMode XML：专项1/1/0，`playmode-results.xml`；全量32/32/0，`full-playmode-results.xml`。
- Console新增Error/Warning：清理预期负例/测试日志后最终强制Refresh/编译，0 Error、0 Warning。

## 玩家与平台证据

- 真实玩家路径和可断言值：PlayMode从Bootstrap真实加载正式配置与AssetRegistry，为六怪预热29个实例并同时生成5普通+1精英。每怪按表内起点/速度移动，打开包含配置打断笔势和执行时刻的Telegraph，在`Windup -> Attack`边界总计执行2个投射物、1个冲撞、2个近战、1个支援；六个精确租约归还后活动泄漏0、Transform/状态全重置。
- 配置可调证据：测试只修改重算hash的内存JSON，将符火鱼妖HP 30→47、速度110→137、攻击及弹体伤害8→13，重新加载后原型与行为直接反映新值，产品C#无分支变更。
- 标准Web：NOT RUN；T450不要求构建，不把T100基线外推为本任务结果。
- 微信转换：NOT RUN；按用户要求继续延期T120/T130。
- DevTools：NOT RUN；当前环境仍缺微信开发者工具，未伪造结论。
- 真机：NOT RUN；当前无可用设备证据，未伪造结论。
- 截图/日志/产物：4份NUnit XML、`unity-mcp-jobs.md`、`config-verification.md`、本验证文档。T450使用类型正确占位资源，没有将占位画面截图伪报为正式视觉。

## 结论

- 已知问题：无T450新增缺陷。正式动画/美术和身体碰撞形状仍属T630/后续集成范围；微信DevTools/真机缺口沿用T120延期状态，都未伪报。
- 结论：PASS。
