# T610 Verification

## 追溯

- 日期：2026-07-14（Asia/Shanghai）
- 任务与范围：T610；固定可再分发中文字体来源，生成最小重命名子集、字符清单、静态Latin主字体/中文fallback、TMP Settings与移动SDF资源，接入T600 HUD并验证中文、动态数字和裁切。
- 明确不做：不修改工作簿、schema/FieldDictionary/DTO/导出器、受管JSON/hash/ConfigIds、场景、Prefab、Registry、Input Actions、Packages、ProjectSettings、微信SDK或Builds；不提前实现T620反馈、T630正式美术、T640完整适配或T650教程遮罩，不恢复T120/T130平台工作。
- 分支/提交：`main`；提交信息`T610: add Chinese TMP fallback coverage`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：Unity `6000.5.1f1`。主工程Editor由交互实例占用，使用`artifacts/tmp/T600-unity-project`隔离副本执行同版本Unity BatchMode导入、Editor API字体生成与测试；截图专项去掉`-nographics`并使用Metal实际渲染。未使用MCP，不伪造MCP结果。
- 配置Schema/内容版本/hash：保持schema `4` / content `0.6.0-sample` / `54885fb2ce8373bad21af796d96a7a4cbc4ce6d8f41def3f909686b14ec87a1d`，配置与工作簿无差异。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：新增OFL `One Stroke Demon UI`子集、294码点清单、来源/许可证、512×512 Latin静态主Atlas、1024×1024中文静态fallback、精简TMP Essential Resources和可重复作者工具；TMP Settings及HUD显式资源路径统一使用fallback链；调整4处受Noto行高影响的HUD文本容器；新增3个EditMode和1个PlayMode测试、1920×1080截图，并同步任务/进度/技术/配置/测试/决策文档与索引。
- 用户已有改动保护：开始时`main@bbd12724ac398ccd8dc2c7056ab44a332256d5e0`工作树干净；所有差异由T610产生，未覆盖用户已有改动。
- `git diff --check`：PASS。
- 暂存白名单审查：提交前逐项对照`change-whitelist.md`，无禁止路径；最终暂存差异已审查。

## 自动验证

- 静态/导出校验：`Tools/CI/verify-config.sh --skip-unity` PASS；三生成物无漂移，ConfigExporter构建0 warning/0 error，.NET 56/56。FontTools复核交付TTF的294个cmap全部存在，源/子集/字符清单/Atlas/hash及体积见`font-inventory.md`；最终Unity作者日志为`font-authoring-unity.log`。
- EditMode XML：专项3 / 通过3 / 失败0 / `editmode-results.xml`；全量183 / 通过183 / 失败0 / `editmode-full-results.xml`。
- PlayMode XML：专项1 / 通过1 / 失败0 / `playmode-results.xml`；带Metal截图专项1/1 / `playmode-screenshot-results.xml`；全量44 / 通过44 / 失败0 / `playmode-full-results.xml`。
- Console新增Error/Warning：最终专项与全量日志无编译错误、缺字/替换字形warning、运行异常、Native Crash或测试失败。图形截图启动日志在并行主Editor存在时记录一次Unity License IPC初始握手失败行，随后成功取得许可并完成1/1；该行发生在测试前，不是游戏Console或字体运行时错误，已保留原日志。

## 玩家与平台证据

- 真实玩家路径和可断言值：从Bootstrap真实配置进入MainMenu后创建ZhCN BattleHUD；实际渲染`幽菌古道`、生命`87 / 100`、能量`100 / 100`、连斩12、评分5210、架势`斩妖刀`、终极`天地封妖令/可释放`及动态`-12345 暴击`。每个非空活动TMP文本逐字符验证所用字体资产和原Unicode一致，主/fallback链覆盖且无replacement glyph；Playing和Victory/2星/解锁关卡+100积分结算两态均无overflow或truncate。
- 标准Web：NOT RUN（T610不要求构建；历史G1不外推到新字体包体/内存）。
- 微信转换：NOT RUN（用户已要求绕过T120/微信打包任务）。
- DevTools：BLOCKED（未安装微信开发者工具，且本任务明确不恢复平台门）。
- 真机：BLOCKED（缺DevTools/设备路径，本任务不伪造真机结论）。
- 截图/日志/产物：`chinese-hud-1920x1080.png`为1920×1080 RGB PNG，57,894字节，SHA-256 `90a4d8ac...2f447`，已目检中文、动态数字、边界和裁切；完整hash/体积见`font-inventory.md`，专项/全量XML与Unity日志均在本目录。

## 结论

- 已知问题：两个静态TMP `.asset`合计约2.78MB；Editor Metal截图证明字形与基础1920×1080布局，但尚未测量Web/微信压缩后包体、低端机字体内存、DevTools或真机显示。T640仍需完整比例/刘海/左右手截图矩阵。
- 结论：PASS。
