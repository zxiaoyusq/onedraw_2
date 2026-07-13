# T350 Verification

## 追溯

- 日期：2026-07-13
- 任务与范围：T350；消费T320/T340同一处理点集的配置半径分段胶囊命中、路径排序、同笔去重、弱点区分和固定NonAlloc缓存。
- 明确不做：不执行伤害、方向奖励、连斩、评分或能量；不实现投射物规则、敌人状态机、场景/Prefab或微信平台工作。
- 分支/提交：`main` / `T350: implement nonalloc stroke hit resolution`。
- 任务开始Git基线：见`baseline-commit.txt`与`baseline-status.txt`。
- Unity精确版本与MCP实例：6000.5.1f1 / `onedraw_2@272e911286835fad`。
- 配置Schema/内容版本/hash：schema 1 / content 0.1.1-sample / `16b64a6f3795cfe0f16dd5f2f092a021b7ef4c07b0b15119296c9da0e22b4b1c`，三生成物无漂移。

## 改动审查

- 预计白名单：见`change-whitelist.md`。
- 实际改动：Combat命中合同/设置/解析器/Physics2D查询、T350 EditMode与PlayMode测试、任务索引/进度和本证据目录。
- 用户已有改动保护：开始工作树干净；Unity Test Runner的`EditorSettings.asset`临时差异每轮恢复；无用户文件被覆盖。
- `git diff --check`：PASS。
- 暂存白名单审查：PASS；全部改动属于预先记录白名单，禁止目录0项，未暂存差异0项，`git diff --cached --check`通过。

## 自动验证

- 静态/导出校验：九个新增C#逐文件标准诊断0 warning/0 error；三生成物diff PASS、ConfigExporter 0 warning/0 error、.NET 54/54；asmdef、`.meta`、唯一READY、依赖和禁止模式检查PASS。
- EditMode：专项6 / 6 / 0，job `2fbbc3cc958a41489fa0f5f76108200c`；状态同步后最终全量78 / 78 / 0，job见`unity-test-jobs.md`。
- PlayMode：专项2 / 2 / 0，job `8f7c8a8acbcf4133b54f2f9348aae0bb`；状态同步后最终全量22 / 22 / 0，job见`unity-test-jobs.md`。
- Console新增Error/Warning：脚本导入编译隔离检查0 / 0；最终全量仅有既有CFGRT003负例和Test Runner固定Exception/Warning，T350新增产品消息0项，详见`regression-notes.md`。

## 玩家与平台证据

- 真实玩家路径和可断言值：Bootstrap加载28表645条配置后，真实Mouse横划经输入、采样、几何、分类和轨迹显示；轨迹与命中共享同一Points引用，依路径命中target 101弱点和target 202主体，跳过偏离路径的303，同体主体/弱点只输出一条记录。
- 标准Web：NOT RUN（T350不要求，沿用T100证据）。
- 微信转换：NOT RUN（按用户要求延期T120/T130）。
- DevTools：BLOCKED（既有T120门，非T350新增阻碍）。
- 真机：BLOCKED（既有T120门，非T350新增阻碍）。
- 截图/日志/产物：见`hit-contract.md`、`runtime-smoke.md`、`unity-test-jobs.md`、`static-audit.md`和`regression-notes.md`。

## 结论

- 已知问题：无T350新增产品问题；真实敌人/投射物挂载分别归T420/T370，微信触摸沿用既有平台门。
- 结论：PASS。
