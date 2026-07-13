# PROGRESS

- 日期：2026-07-13
- 当前成熟度：T110 官方微信转换SDK来源、固定版本与Unity导入兼容基线已完成（含embedded最小补丁）
- 当前任务：T120
- 状态：READY
- Unity精确版本：6000.5.1f1（已由ProjectVersion.txt与本机安装核验）
- 微信SDK来源或版本：官方 `minigame-tuanjie-transform-sdk` v0.1.33 / commit `ed4ad28f433c6b52b5fd3f22a6fa155a0c98c228` / embedded最小补丁
- Active Scene：Assets/_Game/Scenes/Bootstrap.unity
- 配置版本：schema 1 / content 0.1.0-sample

## 已完成

- T110：重新确认官方分发源为 `wechat-miniprogram/minigame-tuanjie-transform-sdk`；固定v0.1.33 commit `ed4ad28f...`、MIT许可证、上游tree和SHA-256，旧禁用仓库未作为安装源。
- T110原始导入：Unity 6000.5.1f1因上游`Object.GetInstanceID()`触发CS0619，且产生`WxEditor`缺失级联错误；原始条目已归档。
- T110兼容补丁：Package Manager embedded完整SDK，只在`UNITY_6000_5_OR_NEWER`切换到`GetEntityId/EntityId.ToULong`；补丁后Console Error/Exception 0，专项2/2、全量EditMode 10/10、PlayMode 2/2通过。
- T110只证明官方来源、依赖可复现和导入编译；G2转换、G3 DevTools、G4真机仍为NOT RUN。SDK的6条非阻断warning登记为BUG-0004。
- T100：Unity 6000.5.1f1标准WebGL构建PASS，耗时9分9秒，总输出12,433,772字节；Brotli产物、SHA-256和HTTP headers已归档。
- T100浏览器：MainMenu canvas实际运行；Input System点击、AudioSource播放启动、中文UTF-8 interop均PASS；PlayerPrefs重载计数1→2；Console Error 0。
- T100回归：专项EditMode 2/2、最终全量EditMode 8/8、PlayMode 2/2通过；初次7/8失败及修复已记录。
- T100只证明G1；G2微信转换、G3 DevTools、G4真机仍为NOT RUN。Web warning与MCP桥接问题登记为BUG-0001至BUG-0003。
- T040：EditMode/PlayMode批处理命令可独立生成NUnit XML，并由结果检查器把失败、零测试和损坏XML转换为非零退出码。
- T040：标准WebGL构建入口已编译并通过参数合同测试；实际Web构建明确留给T100，未生成Builds/WebGL。
- T040：verification/白名单模板、Git基线记录、防覆盖证据初始化、日志卫生和一任务一提交流程已文档化。
- T040专项2/2、批处理全量EditMode 6/6、批处理全量PlayMode 2/2通过；真实Bootstrap→MainMenu路径与最终Console Error 0均已复核。
- T030：目标目录、十个Runtime asmdef、Editor asmdef和现有EditMode/PlayMode测试程序集边界已建立，依赖图无环。
- T030：Bootstrap、MainMenu、Battle由Unity MCP创建并保存；Build Settings固定为三场景，Bootstrap可自动进入MainMenu并通过场景流接口进入Battle。
- T030专项与回归：AssemblyDependencyTests 1/1、SceneFlowSmokePlayModeTests 1/1、全量EditMode 4/4、全量PlayMode 2/2均通过；场景校验和最终Console Error为0。
- T000：玩法、MVP范围、技术边界、配置唯一真相源和完成定义已统一。
- T010：建立根 `.gitignore`，现有Unity工程与开发合同已纳入唯一Git根。
- T020：Graphics与Low/High质量档统一使用URP 2D；Unity MCP依赖固定到commit。
- T020 EditMode 3/3、PlayMode 1/1通过；Mouse与Touchscreen均可驱动同一个Pointer Action。
- TMP与Unity Test Framework程序集已加载，测试程序集可发现并独立运行。
- Unity 6000.5.1f1成功加载SampleScene并进入Play Mode 2秒；探针记录Console Error/Exception/Assert为0。
- T010曾确认SampleScene包含Main Camera与Global Light 2D；T030保留该资产，并由新三场景替换其Build Settings入口。
- 接受现有Unity `6000.5.1f1` 与“仓库根目录即Unity工程根”的基线决策。
- 已确认当前仓库内只有一个Git根；Android、WebGL、macOS和Windows构建模块已安装。
- 玩法、MVP、技术、配置、平台、测试和原子任务计划已建立。
- 已生成Excel配置模板、示例JSON和schema。
- 已纳入工程复盘中的唯一真相源、平台前置Spike、配置闭环、证据分层和一任务一提交方法。

## 当前风险

1. 官方SDK对Unity 6的公开说明仍是测试版本；本工程需要embedded单点补丁才能在6000.5.1f1编译，且仍有6条上游warning（BUG-0004）。
2. G2微信转换、G3 DevTools、G4真机尚未执行；T110结果不能外推为平台可运行。
3. 标准Web存在URP EASU不支持与PlayerPrefs手动同步弃用warning，见BUG-0001/BUG-0002；G1未覆盖TMP中文、后台音频恢复或真机触摸。
4. 长Web构建后Unity MCP实例桥接未自动恢复，见BUG-0003；Unity batch测试与Web运行未受影响。
5. PSD主角和怪物大多为单张Sprite；中文字体包体、Web内存和真机触摸延迟仍需后续验证。

## 下一步

只执行T120：基于固定embedded SDK完成G2转换，并在工具/设备可用时分别记录G3 DevTools和至少一台G4真机；任何未执行层不得写PASS。
