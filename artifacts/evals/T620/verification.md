# T620 Verification

## 追溯

- 日期：2026-07-14
- 任务与范围：T620；实现配置驱动的命中、弱点、破甲、弹反和玩家受击反馈，包括停顿/慢动作、闪白、震屏、池化VFX/伤害数字、预载音效与可关闭震动。
- 明确不做：不改变T360/T420/T370伤害与结算真相；不实现T630正式资源；不改Scene/Prefab/Registry/Input/Packages/ProjectSettings/微信SDK/Builds；不恢复T120/T130或提前开始T630/T640/T650。
- 分支/提交：`main`；提交信息`T620: implement combat feedback`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1；主Editor解锁后自动Refresh并于09:30生成当前Config/Presentation/EditMode/PlayMode程序集。其MCP插件显示orphaned session且服务端实例数为0，因此最终测试使用同步当前Assets/Packages/ProjectSettings/UserSettings/Design/config/Tools的隔离Unity工程执行，不修改主工程Scene或ProjectSettings。
- 配置Schema/内容版本/hash：schema 5 / `0.6.1-sample` / `152b9faa81ba66e29469d7a4a48227f8fb7ef0f969f1cb13679d6fe0ce0786f8`。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：双工作簿/Schema/样例/受管生成物及配置Runtime同步；新增`FeedbackCues`、只读反馈编排与Unity输出、池项渲染/完整重置、参考像素到字体/VFX实际Bounds换算、宿主Layer继承、T620 EditMode/PlayMode测试及相关文档。
- 用户已有改动保护：任务开始工作树干净。审查发现表格工具误改历史`outputs/T600/GameConfig.xlsx`后，已从HEAD归档精确恢复；该路径不再有差异。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；只暂存`change-whitelist.md`列出的T620路径，无Scene、Prefab、Packages、ProjectSettings、Builds、`outputs/**`或其他非白名单路径。

## 自动验证

- 静态/导出校验：`config-pipeline.log`严格生成/verify/diff通过，ConfigExporter构建0 warning/0 error，.NET 58/58。`roslyn-static-compile.log`使用Unity 6000.5.1f1自带编译器覆盖Config、Presentation、T620 EditMode/PlayMode，0 error；Config中2条任务前既存CS0114 warning未扩大。
- T620 EditMode XML：4 / 4 / 0 / `editmode-results.xml`。
- T620 PlayMode XML：1 / 1 / 0 / `playmode-results.xml`；Metal感知重跑1 / 1 / 0 / `playmode-perceptual-results.xml`。
- ConfigPipeline Unity XML：EditMode 19 / 19 / 0 / `config-editmode-results.xml`；PlayMode 3 / 3 / 0 / `config-playmode-results.xml`。
- 全量回归XML：EditMode 187 / 187 / 0 / `full-editmode-results.xml`；PlayMode 45 / 45 / 0 / `full-playmode-results.xml`。
- Console新增Error/Warning：测试结果无未处理日志、编译错误、异常或失败。触发重编译的日志只含T610既有、且本任务未修改对应代码行的4处CS0618测试API弃用warning；Unity许可证首次握手错误均随后成功连接并未影响测试。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap真实配置/Registry创建5个目标，依次发布普通命中、弱点、破甲、弹反和玩家受击；断言目标即时闪白、VFX 5、伤害数字4、音效5、震动5、配置时间缩放0.15、相机位移、活动池9。关闭震动后视觉继续且平台请求不增加；推进4秒后VFX/数字/租约归零、颜色与相机复原，Restart仍无泄漏。
- 标准Web：NOT RUN（T620不要求构建；当前遵循用户要求绕过打包）。
- 微信转换：NOT RUN（T620不要求；当前绕过T120/T130）。
- DevTools：NOT RUN（用户明确暂缓）。
- 真机：NOT RUN（用户明确暂缓）。
- 截图/日志/产物：工作簿关键Sheet、30 Sheet总览和公式检查见`workbook-after/`与`workbook-reimport-check/`；配置证据见`config-pipeline.log`；静态编译见`roslyn-static-compile.log`；Metal截图`combat-feedback-1920x1080.png`为1920×1080、44,179字节、SHA-256 `8c687c19e993035461d01c1eeb3d63a7c43febb180dd949a176c1da08195797d`。画廊显示普通命中白字、弱点黄字、破甲橙字、弹反无伤害数字和玩家受击红字；标签直接取配置`feedbackId`。

## 结论

- 已知问题：T630前沿用Registry黑色占位VFX/静音AudioClip，因此本任务只验cue路由、强度层次、预载和生命周期，不宣称正式资源品质。微信震动平台适配留给T130的`IPlatformService`，T620已验证可关闭端口。
- 结论：PASS。
